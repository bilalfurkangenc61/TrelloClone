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
    public class ListController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ListController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Liste ekleme
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateListRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Board'a erişim kontrolü
                var hasAccess = await _context.Boards
                    .AnyAsync(b => b.Id == request.BoardId &&
                                  b.Team.Members.Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest));

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu panoya erişim yetkiniz yok." });
                }

                // Pozisyon belirle
                var maxPosition = await _context.Lists
                    .Where(l => l.BoardId == request.BoardId)
                    .MaxAsync(l => (int?)l.Position) ?? 0;

                var newList = new List
                {
                    Name = request.Name,
                    BoardId = request.BoardId,
                    Position = maxPosition + 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Lists.Add(newList);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Liste eklendi!",
                    list = new
                    {
                        id = newList.Id,
                        name = newList.Name,
                        position = newList.Position
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Liste düzenleme
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateListRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var list = await _context.Lists
                    .Include(l => l.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(l => l.Id == request.Id);

                if (list == null)
                {
                    return Json(new { success = false, message = "Liste bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = list.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                list.Name = request.Name;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Liste güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Liste silme (arşivleme)
        [HttpPost]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var list = await _context.Lists
                    .Include(l => l.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (list == null)
                {
                    return Json(new { success = false, message = "Liste bulunamadı." });
                }

                // Admin yetkisi kontrolü
                var hasAccess = list.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role == UserRole.Admin);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için admin yetkisi gerekli." });
                }

                list.IsArchived = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Liste arşivlendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Çoklu liste pozisyon güncelleme
        [HttpPost]
        public async Task<IActionResult> UpdatePositions([FromBody] List<UpdateListPositionRequest> updates)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                foreach (var update in updates)
                {
                    var list = await _context.Lists
                        .Include(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                        .FirstOrDefaultAsync(l => l.Id == update.ListId);

                    if (list == null) continue;

                    // Erişim kontrolü
                    var hasAccess = list.Board.Team.Members
                        .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                    if (!hasAccess) continue;

                    list.Position = update.NewPosition;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Liste pozisyonları güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Liste pozisyon değiştirme
        [HttpPost]
        public async Task<IActionResult> UpdatePosition([FromBody] UpdateListPositionRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var list = await _context.Lists
                    .Include(l => l.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(l => l.Id == request.ListId);

                if (list == null)
                {
                    return Json(new { success = false, message = "Liste bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = list.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                list.Position = request.NewPosition;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Liste pozisyonu güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
    }

    // Request Models
    public class CreateListRequest
    {
        public string Name { get; set; } = string.Empty;
        public int BoardId { get; set; }
    }

    public class UpdateListRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateListPositionRequest
    {
        public int ListId { get; set; }
        public int NewPosition { get; set; }
    }
}