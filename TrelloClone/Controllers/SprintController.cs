using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.ViewModels;

namespace TrelloClone.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class SprintController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SprintController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Sprint'leri listeleme - Board bazında
        // Sprint'leri listeleme - Board bazında
        public async Task<IActionResult> Index(int boardId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Board erişim kontrolü
            var board = await _context.Boards
                .Include(b => b.Team)
                    .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.IsActive);

            if (board == null)
            {
                return NotFound("Pano bulunamadı.");
            }

            // Kullanıcının bu panoya erişim yetkisi var mı?
            var hasAccess = board.Team.Members
                .Any(m => m.UserId == currentUser.Id && m.IsActive);

            if (!hasAccess)
            {
                return Forbid("Bu panoya erişim yetkiniz yok.");
            }

            // Board'daki tüm sprint'leri getir - CardSprints ile birlikte
            var sprints = await _context.Sprints
                .Include(s => s.CreatedByUser)
                .Include(s => s.CardSprints)  // ÖNEMLİ: CardSprints Include
                    .ThenInclude(cs => cs.Card)  // Card Include
                        .ThenInclude(c => c.List)  // List Include
                .Where(s => s.BoardId == boardId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Board = board;
            return View(sprints);
        }



        // Sprint detayları
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .Include(s => s.CreatedByUser)
                .Include(s => s.CardSprints)  // ÖNEMLİ!
                    .ThenInclude(cs => cs.Card)
                        .ThenInclude(c => c.List)
                .Include(s => s.CardSprints)
                    .ThenInclude(cs => cs.Card)
                        .ThenInclude(c => c.Assignments)
                            .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound("Sprint bulunamadı.");
            }

            // Erişim kontrolü
            var hasAccess = sprint.Board.Team.Members
                .Any(m => m.UserId == currentUser.Id && m.IsActive);

            if (!hasAccess)
            {
                return Forbid("Bu sprint'e erişim yetkiniz yok.");
            }

            return View(sprint);
        }

        // Sprint oluşturma sayfası
        [HttpGet]
        public async Task<IActionResult> Create(int boardId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Board erişim kontrolü
            var board = await _context.Boards
                .Include(b => b.Team)
                    .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.IsActive);

            if (board == null)
            {
                return NotFound("Pano bulunamadı.");
            }

            // Yetki kontrolü (Guest olamaz)
            var membership = board.Team.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

            if (membership == null || membership.Role == UserRole.Guest)
            {
                return Forbid("Sprint oluşturma yetkiniz yok.");
            }

            var model = new CreateSprintViewModel
            {
                BoardId = boardId,
                BoardName = board.Name,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(14) // Varsayılan 2 haftalık sprint
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCards(int boardId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Board erişim kontrolü
                var board = await _context.Boards
                    .Include(b => b.Team)
                        .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(b => b.Id == boardId && b.IsActive);

                if (board == null)
                {
                    return Json(new List<object>());
                }

                var hasAccess = board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive);

                if (!hasAccess)
                {
                    return Json(new List<object>());
                }

                // Henüz aktif sprint'e atanmamış kartları getir
                var availableCards = await _context.Cards
                    .Include(c => c.List)
                    .Include(c => c.CardSprints)
                        .ThenInclude(cs => cs.Sprint)
                    .Where(c => c.List.BoardId == boardId &&
                               !c.IsArchived &&
                               !c.CardSprints.Any(cs => cs.Sprint.Status == SprintStatus.Active))
                    .Select(c => new {
                        id = c.Id,
                        title = c.Title,
                        listName = c.List.Name
                    })
                    .OrderBy(c => c.listName)
                    .ThenBy(c => c.title)
                    .ToListAsync();

                return Json(availableCards);
            }
            catch (Exception ex)
            {
                // Hata loglama
                Console.WriteLine($"GetAvailableCards error: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }



        // Sprint oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSprintViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Board erişim kontrolü
                var board = await _context.Boards
                    .Include(b => b.Team)
                        .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(b => b.Id == model.BoardId && b.IsActive);

                if (board == null)
                {
                    ModelState.AddModelError("", "Pano bulunamadı.");
                    return View(model);
                }

                // Yetki kontrolü
                var membership = board.Team.Members
                    .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

                if (membership == null || membership.Role == UserRole.Guest)
                {
                    ModelState.AddModelError("", "Sprint oluşturma yetkiniz yok.");
                    return View(model);
                }

                // Model validasyonu - Sprint model kurallarına göre
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError("Name", "Sprint adı gereklidir.");
                    model.BoardName = board.Name;
                    return View(model);
                }

                if (model.Name.Length > 200)
                {
                    ModelState.AddModelError("Name", "Sprint adı en fazla 200 karakter olabilir.");
                    model.BoardName = board.Name;
                    return View(model);
                }

                if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 1000)
                {
                    ModelState.AddModelError("Description", "Açıklama en fazla 1000 karakter olabilir.");
                    model.BoardName = board.Name;
                    return View(model);
                }

                // Tarih kontrolü
                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
                    model.BoardName = board.Name;
                    return View(model);
                }

                // Çakışan aktif sprint kontrolü
                var conflictingSprint = await _context.Sprints
                    .AnyAsync(s => s.BoardId == model.BoardId &&
                                  s.Status == SprintStatus.Active &&
                                  ((model.StartDate >= s.StartDate && model.StartDate <= s.EndDate) ||
                                   (model.EndDate >= s.StartDate && model.EndDate <= s.EndDate) ||
                                   (model.StartDate <= s.StartDate && model.EndDate >= s.EndDate)));

                if (conflictingSprint)
                {
                    ModelState.AddModelError("", "Bu tarih aralığında zaten aktif bir sprint bulunmaktadır.");
                    model.BoardName = board.Name;
                    return View(model);
                }

                // Yeni sprint oluştur - Model'deki default değerleri kullan
                var sprint = new Sprint
                {
                    Name = model.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    BoardId = model.BoardId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Status = SprintStatus.Planning, // Model'deki default değer
                    CreatedByUserId = currentUser.Id,
                    CreatedAt = DateTime.UtcNow // Model'deki default değer
                };

                _context.Sprints.Add(sprint);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Sprint başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Details), new { id = sprint.Id });
            }

            // Model validation başarısızsa, board adını tekrar set et
            var boardData = await _context.Boards.FindAsync(model.BoardId);
            model.BoardName = boardData?.Name ?? "Bilinmeyen Pano";

            return View(model);
        }

        // Sprint düzenleme
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound("Sprint bulunamadı.");
            }

            // Yetki kontrolü (Admin veya oluşturan kişi)
            var membership = sprint.Board.Team.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

            if (membership == null ||
                (membership.Role != UserRole.Admin && sprint.CreatedByUserId != currentUser.Id))
            {
                return Forbid("Bu sprint'i düzenleme yetkiniz yok.");
            }

            var model = new EditSprintViewModel
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Description = sprint.Description,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                BoardId = sprint.BoardId,
                BoardName = sprint.Board.Name
            };

            return View(model);
        }

        // Sprint düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditSprintViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var sprint = await _context.Sprints
                    .Include(s => s.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (sprint == null)
                {
                    return NotFound("Sprint bulunamadı.");
                }

                // Yetki kontrolü
                var membership = sprint.Board.Team.Members
                    .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

                if (membership == null ||
                    (membership.Role != UserRole.Admin && sprint.CreatedByUserId != currentUser.Id))
                {
                    return Forbid("Bu sprint'i düzenleme yetkiniz yok.");
                }

                // Model validasyonu - Sprint model kurallarına göre
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError("Name", "Sprint adı gereklidir.");
                    return View(model);
                }

                if (model.Name.Length > 200)
                {
                    ModelState.AddModelError("Name", "Sprint adı en fazla 200 karakter olabilir.");
                    return View(model);
                }

                if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 1000)
                {
                    ModelState.AddModelError("Description", "Açıklama en fazla 1000 karakter olabilir.");
                    return View(model);
                }

                // Tarih kontrolü
                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
                    return View(model);
                }

                // Aktif sprint'in tarihlerini değiştirme kontrolü
                if (sprint.Status == SprintStatus.Active &&
                    (sprint.StartDate != model.StartDate || sprint.EndDate != model.EndDate))
                {
                    ModelState.AddModelError("", "Aktif sprint'in tarihleri değiştirilemez.");
                    return View(model);
                }

                // Sprint'i güncelle
                sprint.Name = model.Name.Trim();
                sprint.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
                sprint.StartDate = model.StartDate;
                sprint.EndDate = model.EndDate;
                sprint.Status = model.Status;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sprint başarıyla güncellendi!";
                return RedirectToAction(nameof(Details), new { id = sprint.Id });
            }

            return View(model);
        }

        // Sprint başlatma
        [HttpPost]
        public async Task<IActionResult> StartSprint(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return Json(new { success = false, message = "Sprint bulunamadı." });
            }

            // Yetki kontrolü
            var membership = sprint.Board.Team.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

            if (membership == null || membership.Role == UserRole.Guest)
            {
                return Json(new { success = false, message = "Sprint başlatma yetkiniz yok." });
            }

            // Sprint durumu kontrolü
            if (sprint.Status != SprintStatus.Planning)
            {
                return Json(new { success = false, message = "Sadece planlama aşamasındaki sprint'ler başlatılabilir." });
            }

            // Çakışan aktif sprint kontrolü
            var hasActiveSprint = await _context.Sprints
                .AnyAsync(s => s.BoardId == sprint.BoardId &&
                              s.Status == SprintStatus.Active &&
                              s.Id != sprint.Id);

            if (hasActiveSprint)
            {
                return Json(new { success = false, message = "Bu panoda zaten aktif bir sprint bulunmaktadır." });
            }

            // Sprint'i başlat
            sprint.Status = SprintStatus.Active;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sprint başlatıldı!" });
        }

        // Sprint bitirme
        [HttpPost]
        public async Task<IActionResult> CompleteSprint(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .Include(s => s.CardSprints)
                    .ThenInclude(cs => cs.Card)
                        .ThenInclude(c => c.List)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return Json(new { success = false, message = "Sprint bulunamadı." });
            }

            // Yetki kontrolü
            var membership = sprint.Board.Team.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

            if (membership == null || membership.Role == UserRole.Guest)
            {
                return Json(new { success = false, message = "Sprint bitirme yetkiniz yok." });
            }

            // Sprint durumu kontrolü
            if (sprint.Status != SprintStatus.Active)
            {
                return Json(new { success = false, message = "Sadece aktif sprint'ler bitirilebilir." });
            }

            // Sprint'i tamamla
            sprint.Status = SprintStatus.Completed;
            await _context.SaveChangesAsync();

            // Sprint raporunu oluştur
            var totalCards = sprint.CardSprints?.Count ?? 0;
            var completedCards = 0;

            if (sprint.CardSprints != null)
            {
                completedCards = sprint.CardSprints.Count(cs =>
                    cs.Card?.List?.Name?.ToLower()?.Contains("tamamla") == true ||
                    cs.Card?.List?.Name?.ToLower()?.Contains("done") == true ||
                    cs.Card?.List?.Name?.ToLower()?.Contains("finish") == true);
            }

            var completionRate = totalCards > 0 ? (completedCards * 100 / totalCards) : 0;

            return Json(new
            {
                success = true,
                message = "Sprint tamamlandı!",
                report = new
                {
                    totalCards = totalCards,
                    completedCards = completedCards,
                    completionRate = completionRate
                }
            });
        }

        // Sprint iptal etme
        // Sprint iptal etme
        [HttpPost]
        public async Task<IActionResult> CancelSprint(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return Json(new { success = false, message = "Sprint bulunamadı." });
            }

            // Yetki kontrolü (Admin veya oluşturan kişi)
            var membership = sprint.Board.Team.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id && m.IsActive);

            if (membership == null ||
                (membership.Role != UserRole.Admin && sprint.CreatedByUserId != currentUser.Id))
            {
                return Json(new { success = false, message = "Sprint iptal etme yetkiniz yok." });
            }

            // Sprint durumu kontrolü
            if (sprint.Status == SprintStatus.Completed || sprint.Status == SprintStatus.Cancelled)
            {
                return Json(new { success = false, message = "Tamamlanmış veya iptal edilmiş sprint'ler tekrar iptal edilemez." });
            }

            // Sprint'i iptal et
            sprint.Status = SprintStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sprint iptal edildi!" });
        }

        // Sprint kart atama
        [HttpPost]
        public async Task<IActionResult> AssignCardToSprint([FromBody] AssignCardToSprintRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Sprint ve kart kontrolü
                var sprint = await _context.Sprints
                    .Include(s => s.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(s => s.Id == request.SprintId);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (sprint == null || card == null)
                {
                    return Json(new { success = false, message = "Sprint veya kart bulunamadı." });
                }

                // Aynı pano kontrolü
                if (sprint.BoardId != card.List.BoardId)
                {
                    return Json(new { success = false, message = "Kart ve sprint aynı panoda olmalıdır." });
                }

                // Yetki kontrolü
                var hasAccess = sprint.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Sprint durumu kontrolü
                if (sprint.Status == SprintStatus.Completed || sprint.Status == SprintStatus.Cancelled)
                {
                    return Json(new { success = false, message = "Tamamlanmış veya iptal edilmiş sprint'lere kart atanamaz." });
                }

                // Zaten atanmış mı kontrolü
                var existingAssignment = await _context.CardSprints
                    .FirstOrDefaultAsync(cs => cs.CardId == request.CardId && cs.SprintId == request.SprintId);

                if (existingAssignment != null)
                {
                    return Json(new { success = false, message = "Kart zaten bu sprint'e atanmış." });
                }

                // Kartın başka aktif sprint'e atanması kontrolü
                var hasActiveSprintAssignment = await _context.CardSprints
                    .Include(cs => cs.Sprint)
                    .AnyAsync(cs => cs.CardId == request.CardId &&
                                   cs.Sprint.Status == SprintStatus.Active);

                if (hasActiveSprintAssignment)
                {
                    return Json(new { success = false, message = "Kart zaten başka bir aktif sprint'e atanmış." });
                }

                // Yeni atama oluştur
                var cardSprint = new CardSprint
                {
                    CardId = request.CardId,
                    SprintId = request.SprintId,
                    AddedAt = DateTime.UtcNow
                };

                _context.CardSprints.Add(cardSprint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart sprint'e atandı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Sprint'ten kart kaldırma
        [HttpPost]
        public async Task<IActionResult> RemoveCardFromSprint([FromBody] AssignCardToSprintRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var cardSprint = await _context.CardSprints
                    .Include(cs => cs.Sprint)
                        .ThenInclude(s => s.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(cs => cs.CardId == request.CardId && cs.SprintId == request.SprintId);

                if (cardSprint == null)
                {
                    return Json(new { success = false, message = "Kart ataması bulunamadı." });
                }

                // Yetki kontrolü
                var hasAccess = cardSprint.Sprint.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Sprint durumu kontrolü
                if (cardSprint.Sprint.Status == SprintStatus.Completed)
                {
                    return Json(new { success = false, message = "Tamamlanmış sprint'lerden kart kaldırılamaz." });
                }

                _context.CardSprints.Remove(cardSprint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart sprint'ten kaldırıldı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Sprint raporu
        public async Task<IActionResult> Report(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Team)
                        .ThenInclude(t => t.Members)
                .Include(s => s.CardSprints)
                    .ThenInclude(cs => cs.Card)
                        .ThenInclude(c => c.List)
                .Include(s => s.CardSprints)
                    .ThenInclude(cs => cs.Card)
                        .ThenInclude(c => c.Assignments)
                            .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound("Sprint bulunamadı.");
            }

            // Erişim kontrolü
            var hasAccess = sprint.Board.Team.Members
                .Any(m => m.UserId == currentUser.Id && m.IsActive);

            if (!hasAccess)
            {
                return Forbid("Bu sprint'e erişim yetkiniz yok.");
            }

            return View(sprint);
        }
    }

    // Request modelleri
    public class AssignCardToSprintRequest
    {
        public int CardId { get; set; }
        public int SprintId { get; set; }
    }
}