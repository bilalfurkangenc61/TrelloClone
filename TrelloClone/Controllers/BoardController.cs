using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.ViewModels;

namespace TrelloClone.Controllers
{
    [Authorize]
    public class BoardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BoardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Panolarım sayfası
        // Board Controller Index Method - Mevcut kodu Sprint'lerle birlikte düzeltelim
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Kullanıcının erişebildiği panolar (üye olduğu takımların panoları)
            var userBoards = await _context.Boards
                .Include(b => b.Team)
                    .ThenInclude(t => t.Members.Where(m => m.IsActive))
                .Include(b => b.CreatedByUser)
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Cards.Where(c => !c.IsArchived))
                .Include(b => b.Sprints) // Sprint'leri Include et - ÖNEMLİ!
                .Where(b => b.Team.Members.Any(m => m.UserId == currentUser.Id && m.IsActive) && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(userBoards);
        }

        // Pano detayları (Ana Trello görünümü)
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var board = await _context.Boards
                .Include(b => b.Team)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
                .Include(b => b.Lists.OrderBy(l => l.Position))
                    .ThenInclude(l => l.Cards.OrderBy(c => c.Position))
                        .ThenInclude(c => c.Assignments)
                            .ThenInclude(a => a.User)
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Cards)
                        .ThenInclude(c => c.Labels)
                            .ThenInclude(cl => cl.Label)
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

            if (board == null)
            {
                return NotFound("Pano bulunamadı.");
            }

            // Kullanıcının bu panoya erişim yetkisi var mı?
            var hasAccess = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == board.TeamId && tm.UserId == currentUser.Id && tm.IsActive);

            if (!hasAccess)
            {
                return Forbid("Bu panoya erişim yetkiniz yok.");
            }

            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.TeamMembers = board.Team.Members.Where(m => m.IsActive).ToList();

            return View(board);
        }

        // Pano oluşturma sayfası
        [HttpGet]
        public async Task<IActionResult> Create(int? teamId = null, string? name = null, string? description = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Kullanıcının üye olduğu takımları getir
            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == currentUser.Id && tm.IsActive && tm.Role != UserRole.Guest)
                .Select(tm => tm.Team)
                .ToListAsync();

            var model = new CreateBoardViewModel
            {
                Name = name ?? string.Empty,
                Description = description ?? string.Empty,
                TeamId = teamId,
                AvailableTeams = userTeams
            };

            return View(model);
        }

        // Pano oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBoardViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Kullanıcının bu takımda üye olduğunu kontrol et
                var membership = await _context.TeamMembers
                    .FirstOrDefaultAsync(tm => tm.TeamId == model.TeamId && tm.UserId == currentUser.Id && tm.IsActive);

                if (membership == null || membership.Role == UserRole.Guest)
                {
                    ModelState.AddModelError("", "Bu takımda pano oluşturma yetkiniz yok.");
                    model.AvailableTeams = await GetUserTeams(currentUser.Id);
                    return View(model);
                }

                // Yeni pano oluştur
                var board = new Board
                {
                    Name = model.Name,
                    Description = model.Description,
                    TeamId = model.TeamId.Value,
                    CreatedByUserId = currentUser.Id,
                    BackgroundColor = model.BackgroundColor ?? "#0079bf",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Boards.Add(board);
                await _context.SaveChangesAsync();

                // Varsayılan listeler oluştur
                var defaultLists = new[]
                {
                    new List { Name = "Yapılacaklar", Position = 1, BoardId = board.Id, CreatedAt = DateTime.UtcNow },
                    new List { Name = "Devam Eden", Position = 2, BoardId = board.Id, CreatedAt = DateTime.UtcNow },
                    new List { Name = "Tamamlanan", Position = 3, BoardId = board.Id, CreatedAt = DateTime.UtcNow }
                };

                _context.Lists.AddRange(defaultLists);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Pano başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Details), new { id = board.Id });
            }

            model.AvailableTeams = await GetUserTeams((await _userManager.GetUserAsync(User)).Id);
            return View(model);
        }

        // Pano düzenleme
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var board = await _context.Boards
                .Include(b => b.Team)
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

            if (board == null)
            {
                return NotFound();
            }

            // Admin yetkisi kontrol et
            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == board.TeamId && tm.UserId == currentUser.Id && tm.IsActive);

            if (membership == null || membership.Role != UserRole.Admin)
            {
                return Forbid("Bu işlem için admin yetkisi gerekli.");
            }

            var model = new EditBoardViewModel
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Description,
                BackgroundColor = board.BackgroundColor,
                TeamName = board.Team.Name
            };

            return View(model);
        }




        // Pano düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditBoardViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var board = await _context.Boards
                    .Include(b => b.Team)
                    .FirstOrDefaultAsync(b => b.Id == model.Id && b.IsActive);

                if (board == null)
                {
                    return NotFound();
                }

                // Admin yetkisi kontrol et
                var membership = await _context.TeamMembers
                    .FirstOrDefaultAsync(tm => tm.TeamId == board.TeamId && tm.UserId == currentUser.Id && tm.IsActive);

                if (membership == null || membership.Role != UserRole.Admin)
                {
                    return Forbid();
                }

                board.Name = model.Name;
                board.Description = model.Description;
                board.BackgroundColor = model.BackgroundColor;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Pano bilgileri güncellendi!";
                return RedirectToAction(nameof(Details), new { id = board.Id });
            }

            return View(model);
        }

        // Yardımcı metod
        private async Task<List<Team>> GetUserTeams(string userId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId && tm.IsActive && tm.Role != UserRole.Guest)
                .Select(tm => tm.Team)
                .ToListAsync();
        }
    }
}