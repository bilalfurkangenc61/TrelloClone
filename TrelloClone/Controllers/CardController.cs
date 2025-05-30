using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;

namespace TrelloClone.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Kart ekleme
        [HttpPost]
        [Route("Card/Create")]
        public async Task<IActionResult> Create([FromBody] CreateCardRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Liste erişim kontrolü
                var list = await _context.Lists
                    .Include(l => l.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(l => l.Id == request.ListId);

                if (list == null)
                {
                    return Json(new { success = false, message = "Liste bulunamadı." });
                }

                var hasAccess = list.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Pozisyon belirle
                var maxPosition = await _context.Cards
                    .Where(c => c.ListId == request.ListId && !c.IsArchived)
                    .MaxAsync(c => (int?)c.Position) ?? 0;

                var newCard = new Card
                {
                    Title = request.Title,
                    ListId = request.ListId,
                    Position = maxPosition + 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Cards.Add(newCard);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Kart eklendi!",
                    card = new
                    {
                        id = newCard.Id,
                        title = newCard.Title,
                        position = newCard.Position
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Kart detaylarını JSON olarak getir
        // CardController.cs - Debug versiyonu (GetCardDetails metodu)

        [HttpGet]
        [Route("Card/GetCardDetails/{cardId:int}")]
        public async Task<IActionResult> GetCardDetails(int cardId)
        {
            try
            {
                Console.WriteLine($"CardId alındı: {cardId}");

                var currentUser = await _userManager.GetUserAsync(User);
                Console.WriteLine($"Mevcut kullanıcı: {currentUser?.Email}");

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                                    .ThenInclude(m => m.User)
                    .Include(c => c.Assignments)
                        .ThenInclude(a => a.User)
                    .Include(c => c.Comments)
                        .ThenInclude(com => com.User)
                    .Include(c => c.Labels)
                        .ThenInclude(cl => cl.Label)
                    .Include(c => c.Checklists)
                        .ThenInclude(cl => cl.Items)
                    .Include(c => c.Attachments)
                    .FirstOrDefaultAsync(c => c.Id == cardId);

                Console.WriteLine($"Kart bulundu: {card != null}");

                if (card == null)
                {
                    Console.WriteLine("Kart veritabanında bulunamadı!");
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                Console.WriteLine($"Kart başlığı: {card.Title}");
                Console.WriteLine($"Liste ID: {card.ListId}");
                Console.WriteLine($"Board ID: {card.List?.BoardId}");

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive);

                Console.WriteLine($"Kullanıcının erişim yetkisi: {hasAccess}");

                if (!hasAccess)
                {
                    Console.WriteLine("Kullanıcının bu karta erişim yetkisi yok!");
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                var cardData = new
                {
                    id = card.Id,
                    title = card.Title,
                    description = card.Description ?? string.Empty,
                    dueDate = card.DueDate?.ToString("yyyy-MM-ddTHH:mm"),
                    listName = card.List.Name,
                    assignments = card.Assignments.Select(a => new
                    {
                        userId = a.UserId,
                        userName = $"{a.User.FirstName} {a.User.LastName}",
                        userInitial = a.User.FirstName.Substring(0, 1).ToUpper(),
                        profilePicture = a.User.ProfilePicture
                    }).ToList(),
                    teamMembers = card.List.Board.Team.Members
                        .Where(m => m.IsActive)
                        .Select(m => new
                        {
                            userId = m.UserId,
                            userName = $"{m.User.FirstName} {m.User.LastName}",
                            userInitial = m.User.FirstName.Substring(0, 1).ToUpper(),
                            profilePicture = m.User.ProfilePicture,
                            role = m.Role.ToString()
                        }).ToList()
                };

                Console.WriteLine($"Kart verisi hazırlandı, ID: {cardData.id}");
                return Json(new { success = true, card = cardData });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Yorumları getir
        [HttpGet]
        [Route("Card/GetComments/{cardId:int}")]
        public async Task<IActionResult> GetComments(int cardId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .Include(c => c.Comments)
                        .ThenInclude(com => com.User)
                    .FirstOrDefaultAsync(c => c.Id == cardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                var comments = card.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        id = c.Id,
                        content = c.Content,
                        createdAt = c.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        userName = $"{c.User.FirstName} {c.User.LastName}",
                        userInitial = c.User.FirstName.Substring(0, 1).ToUpper(),
                        profilePicture = c.User.ProfilePicture
                    })
                    .ToList();

                return Json(new { success = true, comments = comments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Kart açıklaması güncelle
        [HttpPost]
        [Route("Card/UpdateDescription")]
        public async Task<IActionResult> UpdateDescription([FromBody] UpdateDescriptionRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                card.Description = request.Description;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Açıklama güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Yorum ekleme
        [HttpPost]
        [Route("Card/AddComment")]
        public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                var comment = new Comment
                {
                    Content = request.Content,
                    CardId = request.CardId,
                    UserId = currentUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Yorum eklendi!",
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        createdAt = comment.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        user = new
                        {
                            firstName = currentUser.FirstName,
                            lastName = currentUser.LastName,
                            profilePicture = currentUser.ProfilePicture
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Kartı güncelle (başlık)
        [HttpPost]
        [Route("Card/UpdateTitle")]
        public async Task<IActionResult> UpdateTitle([FromBody] UpdateTitleRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                card.Title = request.Title;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart başlığı güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Son tarihi güncelle
        [HttpPost]
        [Route("Card/UpdateDueDate")]
        public async Task<IActionResult> UpdateDueDate([FromBody] UpdateDueDateRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Tarih parse etme
                if (string.IsNullOrEmpty(request.DueDate))
                {
                    card.DueDate = null;
                }
                else
                {
                    if (DateTime.TryParse(request.DueDate, out DateTime parsedDate))
                    {
                        card.DueDate = parsedDate;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Geçersiz tarih formatı." });
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Son tarih güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Karta üye atama
        // CardController.cs - AssignMember metodunu bu debug versiyonuyla değiştirin:

        [HttpPost]
        [Route("Card/AssignMember")]
        public async Task<IActionResult> AssignMember([FromBody] AssignMemberRequest request)
        {
            try
            {
                Console.WriteLine($"=== AssignMember Debug Başladı ===");
                Console.WriteLine($"CardId: {request.CardId}");
                Console.WriteLine($"UserId: {request.UserId}");

                var currentUser = await _userManager.GetUserAsync(User);
                Console.WriteLine($"Current User: {currentUser?.Email}");

                if (request.CardId <= 0)
                {
                    Console.WriteLine("HATA: Geçersiz CardId");
                    return Json(new { success = false, message = "Geçersiz kart ID'si." });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    Console.WriteLine("HATA: Geçersiz UserId");
                    return Json(new { success = false, message = "Geçersiz kullanıcı ID'si." });
                }

                // Önce kullanıcının var olup olmadığını kontrol et
                var targetUser = await _userManager.FindByIdAsync(request.UserId);
                if (targetUser == null)
                {
                    Console.WriteLine("HATA: Hedef kullanıcı bulunamadı");
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                Console.WriteLine($"Hedef kullanıcı bulundu: {targetUser.Email}");

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .Include(c => c.Assignments)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    Console.WriteLine("HATA: Kart bulunamadı");
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                Console.WriteLine($"Kart bulundu: {card.Title}");

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                Console.WriteLine($"Erişim kontrolü: {hasAccess}");

                if (!hasAccess)
                {
                    Console.WriteLine("HATA: Erişim yetkisi yok");
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Kullanıcının takım üyesi olup olmadığını kontrol et
                var isTeamMember = card.List.Board.Team.Members
                    .Any(m => m.UserId == request.UserId && m.IsActive);

                Console.WriteLine($"Hedef kullanıcı takım üyesi mi: {isTeamMember}");

                if (!isTeamMember)
                {
                    Console.WriteLine("HATA: Kullanıcı takım üyesi değil");
                    return Json(new { success = false, message = "Kullanıcı bu takımın üyesi değil." });
                }

                // Zaten atanmış mı kontrol et
                var existingAssignment = await _context.CardAssignments
                    .FirstOrDefaultAsync(ca => ca.CardId == request.CardId && ca.UserId == request.UserId);

                Console.WriteLine($"Mevcut atama var mı: {existingAssignment != null}");

                if (existingAssignment != null)
                {
                    Console.WriteLine("HATA: Kullanıcı zaten atanmış");
                    return Json(new { success = false, message = "Bu kullanıcı zaten karta atanmış." });
                }

                // Yeni atama oluştur
                var assignment = new CardAssignment
                {
                    CardId = request.CardId,
                    UserId = request.UserId,
                    AssignedByUserId = currentUser.Id,
                    AssignedAt = DateTime.UtcNow
                };

                Console.WriteLine("Atama nesnesi oluşturuldu, veritabanına ekleniyor...");

                _context.CardAssignments.Add(assignment);

                Console.WriteLine("SaveChanges çağrılıyor...");
                await _context.SaveChangesAsync();

                Console.WriteLine("✅ Atama başarıyla kaydedildi!");

                return Json(new { success = true, message = "Üye karta atandı!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Database constraint hatası kontrolü
                if (ex.Message.Contains("UNIQUE constraint failed") || ex.Message.Contains("duplicate key"))
                {
                    return Json(new { success = false, message = "Bu kullanıcı zaten karta atanmış." });
                }

                return Json(new { success = false, message = "Atama sırasında hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // Kart atamasını kaldır
        [HttpPost]
        [Route("Card/UnassignMember")]
        public async Task<IActionResult> UnassignMember([FromBody] AssignMemberRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var assignment = await _context.CardAssignments
                    .Include(ca => ca.Card)
                        .ThenInclude(c => c.List)
                            .ThenInclude(l => l.Board)
                                .ThenInclude(b => b.Team)
                                    .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(ca => ca.CardId == request.CardId && ca.UserId == request.UserId);

                if (assignment == null)
                {
                    return Json(new { success = false, message = "Atama bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = assignment.Card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                _context.CardAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Üye ataması kaldırıldı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Kartı arşivleme
        [HttpPost]
        [Route("Card/Archive")]
        public async Task<IActionResult> Archive([FromBody] ArchiveCardRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                card.IsArchived = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart arşivlendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Kartı silme
        [HttpPost]
        [Route("Card/Delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteCardRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Kartı başka listeye taşıma
        [HttpPost]
        [Route("Card/MoveToList")]
        public async Task<IActionResult> MoveToList([FromBody] MoveCardRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var card = await _context.Cards
                    .Include(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Team)
                                .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId);

                if (card == null)
                {
                    return Json(new { success = false, message = "Kart bulunamadı." });
                }

                var targetList = await _context.Lists
                    .Include(l => l.Board)
                        .ThenInclude(b => b.Team)
                            .ThenInclude(t => t.Members)
                    .FirstOrDefaultAsync(l => l.Id == request.ListId);

                if (targetList == null)
                {
                    return Json(new { success = false, message = "Hedef liste bulunamadı." });
                }

                // Erişim kontrolü
                var hasAccess = card.List.Board.Team.Members
                    .Any(m => m.UserId == currentUser.Id && m.IsActive && m.Role != UserRole.Guest);

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                // Aynı board'da olup olmadığını kontrol et
                if (card.List.BoardId != targetList.BoardId)
                {
                    return Json(new { success = false, message = "Kartlar sadece aynı pano içinde taşınabilir." });
                }

                // Yeni pozisyon belirle
                var maxPosition = await _context.Cards
                    .Where(c => c.ListId == request.ListId && !c.IsArchived)
                    .MaxAsync(c => (int?)c.Position) ?? 0;

                card.ListId = request.ListId;
                card.Position = maxPosition + 1;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kart taşındı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }

    // Request Models
    public class CreateCardRequest
    {
        public string Title { get; set; } = string.Empty;
        public int ListId { get; set; }
    }

    public class UpdateDescriptionRequest
    {
        public int CardId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AddCommentRequest
    {
        public int CardId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateTitleRequest
    {
        public int CardId { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class UpdateDueDateRequest
    {
        public int CardId { get; set; }
        public string? DueDate { get; set; }
    }

    public class AssignMemberRequest
    {
        public int CardId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class ArchiveCardRequest
    {
        public int CardId { get; set; }
    }

    public class DeleteCardRequest
    {
        public int CardId { get; set; }
    }

    public class MoveCardRequest
    {
        public int CardId { get; set; }
        public int ListId { get; set; }
    }
}