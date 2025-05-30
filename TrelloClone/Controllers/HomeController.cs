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
            // Kullanýcý giriþ yapmýþ mý kontrol et
            if (User.Identity?.IsAuthenticated == true)
            {
                // Giriþ yapmýþsa Dashboard göster
                return await Dashboard();
            }
            else
            {
                // Giriþ yapmamýþsa Landing Page göster
                return View("Landing");
            }
        }

        // Dashboard (giriþ yapmýþ kullanýcýlar için)
        private async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kullanýcýnýn üye olduðu takýmlarý getir
            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.Boards.Where(b => b.IsActive))
                        .ThenInclude(b => b.Lists)
                .Where(tm => tm.UserId == currentUser.Id && tm.IsActive)
                .Select(tm => tm.Team)
                .ToListAsync();

            // Dashboard istatistikleri için veriler hazýrla
            await PrepareUserStats(currentUser.Id);

            // Dashboard view'ýný döndür
            return View("Dashboard", userTeams);
        }

        // Kullanýcý istatistiklerini hazýrla
        private async Task PrepareUserStats(string userId)
        {
            try
            {
                // Kullanýcýnýn atandýðý aktif kartlar
                var activeCards = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == userId &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // Bu ay tamamlanan kartlar (DueDate geçmiþ olan kartlar olarak sayýyoruz)
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

                // ViewBag ile view'e gönder
                ViewBag.ActiveTasksCount = activeCards;
                ViewBag.CompletedTasksCount = completedCards;

                _logger.LogInformation($"User {userId} stats: Active={activeCards}, Completed={completedCards}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri yüklenirken hata oluþtu");
                ViewBag.ActiveTasksCount = 0;
                ViewBag.CompletedTasksCount = 0;
            }
        }

        // AJAX ile görev istatistiklerini getir
        [HttpGet]
        public async Task<IActionResult> GetTaskStats()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Kullanýcý bulunamadý" });
                }

                // Aktif görevler (kullanýcýnýn atandýðý, arþivlenmemiþ kartlar)
                var activeTasks = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                    .Where(ca => ca.UserId == currentUser.Id &&
                                !ca.Card.IsArchived &&
                                ca.Card.List.Board.IsActive)
                    .CountAsync();

                // Tamamlanan görevler (son 30 gün içinde due date'i geçmiþ)
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

                // Bugün vadesi gelen görevler
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
                _logger.LogError(ex, "Task stats alýnýrken hata oluþtu");
                return Json(new { success = false, message = "Ýstatistikler yüklenemedi" });
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
                    return Json(new { success = false, message = "Kullanýcý bulunamadý" });
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
                        message = $"{c.User.FirstName} {c.User.LastName} \"{c.Card.Title}\" kartýna yorum ekledi",
                        time = c.CreatedAt,
                        boardName = c.Card.List.Board.Name,
                        boardId = c.Card.List.Board.Id
                    })
                    .ToListAsync();

                // Son kart atamalarý
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
                            ? $"Size \"{ca.Card.Title}\" kartý atandý"
                            : $"{ca.User.FirstName} {ca.User.LastName} kullanýcýsýna \"{ca.Card.Title}\" kartýný atadýnýz",
                        time = ca.AssignedAt,
                        boardName = ca.Card.List.Board.Name,
                        boardId = ca.Card.List.Board.Id
                    })
                    .ToListAsync();

                // Aktiviteleri birleþtir ve sýrala
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
                _logger.LogError(ex, "Recent activities alýnýrken hata oluþtu");
                return Json(new { success = false, message = "Aktiviteler yüklenemedi" });
            }
        }

        // Zaman farkýný hesapla
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Az önce";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} dakika önce";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} saat önce";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} gün önce";

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