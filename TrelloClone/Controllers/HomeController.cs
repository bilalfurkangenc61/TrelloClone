using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrelloClone.Data;
using TrelloClone.Models;

namespace TrelloClone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Kullan�c� giri� yapm�� m� kontrol et
            if (User.Identity?.IsAuthenticated == true)
            {
                // Giri� yapm��sa Dashboard g�ster
                return await Dashboard();
            }
            else
            {
                // Giri� yapmam��sa Landing Page g�ster
                return View("Landing");
            }
        }

        // Dashboard (giri� yapm�� kullan�c�lar i�in)
        private async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kullan�c�n�n �ye oldu�u tak�mlar� getir
            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.Boards.Where(b => b.IsActive))
                        .ThenInclude(b => b.Lists)
                .Where(tm => tm.UserId == currentUser.Id && tm.IsActive)
                .Select(tm => tm.Team)
                .ToListAsync();

            // Dashboard istatistikleri i�in veriler haz�rla
            await PrepareUserStats(currentUser.Id);

            // Dashboard view'�n� d�nd�r
            return View("Dashboard", userTeams);
        }

        // Kullan�c� istatistiklerini haz�rla
        private async Task PrepareUserStats(string userId)
        {
            try
            {
                // Kullan�c�n�n atand��� aktif kartlar
                var activeCards = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == userId &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // Bu ay tamamlanan kartlar (DueDate ge�mi� olan kartlar olarak say�yoruz)
                var completedCards = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == userId &&
                                ca.Card.DueDate.HasValue &&
                                ca.Card.DueDate.Value < DateTime.Now &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // ViewBag ile view'e g�nder
                ViewBag.ActiveTasksCount = activeCards;
                ViewBag.CompletedTasksCount = completedCards;

                _logger.LogInformation($"User {userId} stats: Active={activeCards}, Completed={completedCards}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri y�klenirken hata olu�tu");
                ViewBag.ActiveTasksCount = 0;
                ViewBag.CompletedTasksCount = 0;
            }
        }

        // AJAX ile g�rev istatistiklerini getir
        [HttpGet]
        public async Task<IActionResult> GetTaskStats()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Kullan�c� bulunamad�" });
                }

                // Aktif g�revler (kullan�c�n�n atand���, ar�ivlenmemi� kartlar)
                var activeTasks = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == currentUser.Id &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // Tamamlanan g�revler (son 30 g�n i�inde due date'i ge�mi�)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var completedTasks = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == currentUser.Id &&
                                ca.Card.DueDate.HasValue &&
                                ca.Card.DueDate.Value >= thirtyDaysAgo &&
                                ca.Card.DueDate.Value < DateTime.Now &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // Bug�n vadesi gelen g�revler
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var dueTodayTasks = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == currentUser.Id &&
                                ca.Card.DueDate.HasValue &&
                                ca.Card.DueDate.Value >= today &&
                                ca.Card.DueDate.Value < tomorrow &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    activeTasks = activeTasks,
                    completedTasks = completedTasks,
                    dueTodayTasks = dueTodayTasks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task stats al�n�rken hata olu�tu");
                return Json(new { success = false, message = "�statistikler y�klenemedi" });
            }
        }

        // Son aktiviteleri getir
        [HttpGet]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Kullan�c� bulunamad�" });
                }

                // Son yorumlar
                var recentComments = await _context.Comments
                    .Include(c => c.Card)
                        .ThenInclude(card => card.List)
                            .ThenInclude(l => l.Board)
                    .Include(c => c.User)
                    .Where(c => c.Card.List.Board.Team.Members
                        .Any(m => m.UserId == currentUser.Id && m.IsActive))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .Select(c => new
                    {
                        type = "comment",
                        message = $"{c.User.FirstName} {c.User.LastName} \"{c.Card.Title}\" kart�na yorum ekledi",
                        time = c.CreatedAt,
                        boardName = c.Card.List.Board.Name,
                        boardId = c.Card.List.Board.Id
                    })
                    .ToListAsync();

                // Son kart atamalar�
                var recentAssignments = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Include(ca => ca.User)
                    .Include(ca => ca.AssignedByUser)
                    .Where(ca => (ca.UserId == currentUser.Id || ca.AssignedByUserId == currentUser.Id) &&
                                ca.Card.List.Board.Team.Members
                                    .Any(m => m.UserId == currentUser.Id && m.IsActive))
                    .OrderByDescending(ca => ca.AssignedAt)
                    .Take(5)
                    .Select(ca => new
                    {
                        type = "assignment",
                        message = ca.UserId == currentUser.Id
                            ? $"Size \"{ca.Card.Title}\" kart� atand�"
                            : $"{ca.User.FirstName} {ca.User.LastName} kullan�c�s�na \"{ca.Card.Title}\" kart�n� atad�n�z",
                        time = ca.AssignedAt,
                        boardName = ca.Card.List.Board.Name,
                        boardId = ca.Card.List.Board.Id
                    })
                    .ToListAsync();

                // Aktiviteleri birle�tir ve s�rala
                var allActivities = recentComments
                    .Concat(recentAssignments)
                    .OrderByDescending(a => a.time)
                    .Take(8)
                    .Select(a => new
                    {
                        a.type,
                        a.message,
                        timeAgo = GetTimeAgo(a.time),
                        a.boardName,
                        a.boardId
                    })
                    .ToList();

                return Json(new { success = true, activities = allActivities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recent activities al�n�rken hata olu�tu");
                return Json(new { success = false, message = "Aktiviteler y�klenemedi" });
            }
        }

        // Zaman fark�n� hesapla
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Az �nce";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} dakika �nce";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} saat �nce";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} g�n �nce";

            return dateTime.ToString("dd.MM.yyyy");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}