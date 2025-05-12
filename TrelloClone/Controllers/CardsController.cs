// Controllers/CardsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrelloClone.Controllers
{
    [Authorize]
    public class CardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CardsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: Cards/GetCardDetails/5
        public async Task<IActionResult> GetCardDetails(int id)
        {
            var card = await _context.Cards
                .Include(c => c.BoardList)
                .ThenInclude(bl => bl.Board)
                .ThenInclude(b => b.Lists)
                .Include(c => c.Labels)
                .ThenInclude(cl => cl.Label)
                .Include(c => c.Checklists)
                .ThenInclude(cl => cl.Items)
                .Include(c => c.Comments)
                .ThenInclude(co => co.User)
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            // Pano etiketlerini getir
            var boardLabels = await _context.Labels
                .Where(l => l.BoardId == card.BoardList.BoardId)
                .ToListAsync();

            // Karta atanmamış kullanıcıları getir
            var currentUser = await _userManager.GetUserAsync(User);
            var boardMembers = await _context.BoardMembers
                .Where(bm => bm.BoardId == card.BoardList.BoardId)
                .Include(bm => bm.User)
                .Select(bm => bm.User)
                .ToListAsync();

            // Pano sahibini ekle
            var boardOwner = await _userManager.FindByIdAsync(card.BoardList.Board.UserId);
            if (boardOwner != null && !boardMembers.Any(u => u.Id == boardOwner.Id))
            {
                boardMembers.Add(boardOwner);
            }

            // Zaten atanmış üyeleri filtrele
            var cardMemberUserIds = card.Members.Select(m => m.UserId).ToList();
            var availableMembers = boardMembers.Where(u => !cardMemberUserIds.Contains(u.Id)).ToList();

            // View model oluştur
            var viewModel = new CardDetailsViewModel
            {
                Card = card,
                BoardLabels = boardLabels,
                AvailableMembers = availableMembers
            };

            return PartialView("_CardDetails", viewModel);
        }


        // GET: Cards/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var card = await _context.Cards
                .Include(c => c.BoardList)
                .ThenInclude(bl => bl.Board)
                .ThenInclude(b => b.Lists)
                .Include(c => c.Labels)
                .ThenInclude(cl => cl.Label)
                .Include(c => c.Checklists)
                .ThenInclude(cl => cl.Items)
                .Include(c => c.Comments)
                .ThenInclude(co => co.User)
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            // Pano etiketlerini getir
            var boardLabels = await _context.Labels
                .Where(l => l.BoardId == card.BoardList.BoardId)
                .ToListAsync();

            // Karta atanmamış kullanıcıları getir
            var currentUser = await _userManager.GetUserAsync(User);
            var boardMembers = await _context.BoardMembers
                .Where(bm => bm.BoardId == card.BoardList.BoardId)
                .Include(bm => bm.User)
                .Select(bm => bm.User)
                .ToListAsync();

            // Pano sahibini ekle
            var boardOwner = await _userManager.FindByIdAsync(card.BoardList.Board.UserId);
            if (boardOwner != null && !boardMembers.Any(u => u.Id == boardOwner.Id))
            {
                boardMembers.Add(boardOwner);
            }

            // Zaten atanmış üyeleri filtrele
            var cardMemberUserIds = card.Members.Select(m => m.UserId).ToList();
            var availableMembers = boardMembers.Where(u => !cardMemberUserIds.Contains(u.Id)).ToList();

            // View model oluştur
            var viewModel = new CardDetailsViewModel
            {
                Card = card,
                BoardLabels = boardLabels,
                AvailableMembers = availableMembers
            };

            return View(viewModel);
        }

        // POST: Cards/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var boardList = await _context.BoardLists
                .Include(bl => bl.Board)
                .Include(bl => bl.Cards)
                .FirstOrDefaultAsync(bl => bl.Id == model.BoardListId);

            if (boardList == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = boardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Bu listede en yüksek pozisyonu bul
            var maxPosition = boardList.Cards.Any() ? boardList.Cards.Max(c => c.Position) : 0;

            var card = new Card
            {
                Title = model.Title,
                Description = model.Description ?? "",  // Null kontrolü ekleyin
                BoardListId = model.BoardListId,
                UserId = currentUser.Id,
                Position = maxPosition + 1,
                CreatedAt = DateTime.Now,
                CoverColor = "",  // Varsayılan değer
                CoverImage = "",  // Varsayılan değer
                IsCompleted = false  // Varsayılan değer
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = card.Id,
                title = card.Title,
                description = card.Description,
                position = card.Position,
                boardListId = card.BoardListId,
                userId = card.UserId,
                createdAt = card.CreatedAt
            });
        }

        // CardsController.cs dosyasında Update metodunu kontrol edin
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            card.Title = model.Title;
            card.Description = model.Description;
            card.CoverColor = model.CoverColor;
            card.DueDate = model.DueDate;
            card.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = card.Id,
                title = card.Title,
                description = card.Description,
                coverColor = card.CoverColor,
                dueDate = card.DueDate,
                updatedAt = card.UpdatedAt
            });
        }

        // POST: Cards/Delete
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeleteCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/MoveCard
        [HttpPost]
        public async Task<IActionResult> MoveCard([FromBody] MoveCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Hedef list bu boardda mı kontrol et
            var targetList = await _context.BoardLists
                .FirstOrDefaultAsync(bl => bl.Id == model.TargetListId && bl.BoardId == board.Id);

            if (targetList == null)
            {
                return NotFound("Hedef liste bulunamadı.");
            }

            // Eski listeden yeni listeye taşınıyor
            if (card.BoardListId != model.TargetListId)
            {
                card.BoardListId = model.TargetListId;
            }

            // Pozisyonu güncelle
            card.Position = model.NewPosition;
            card.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/AssignMember
        [HttpPost]
        public async Task<IActionResult> AssignMember([FromBody] AssignCardMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Atanacak kullanıcı bu boardın üyesi mi kontrol et
            var userToAssign = await _userManager.FindByIdAsync(model.UserId);
            if (userToAssign == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcı zaten atanmış mı
            if (card.Members.Any(m => m.UserId == model.UserId))
            {
                return BadRequest("Bu kullanıcı zaten bu karta atanmış.");
            }

            // Kullanıcı panonun üyesi mi
            bool isBoardMember = board.UserId == model.UserId ||
                                await _context.BoardMembers.AnyAsync(bm =>
                                    bm.BoardId == board.Id && bm.UserId == model.UserId);

            if (!isBoardMember)
            {
                return BadRequest("Bu kullanıcı panonun üyesi değil.");
            }

            var cardMember = new CardMember
            {
                CardId = model.CardId,
                UserId = model.UserId,
                CreatedAt = DateTime.Now
            };

            _context.CardMembers.Add(cardMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = cardMember.Id,
                cardId = cardMember.CardId,
                userId = cardMember.UserId,
                userName = $"{userToAssign.FirstName} {userToAssign.LastName}",
                userEmail = userToAssign.Email,
                userProfilePicture = userToAssign.ProfilePicture
            });
        }

        // POST: Cards/RemoveMember
        [HttpPost]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveCardMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cardMember = await _context.CardMembers
                .Include(cm => cm.Card)
                    .ThenInclude(c => c.BoardList)
                        .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(cm => cm.CardId == model.CardId && cm.UserId == model.UserId);

            if (cardMember == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = cardMember.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.CardMembers.Remove(cardMember);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/AddLabel
        [HttpPost]
        public async Task<IActionResult> AddLabel([FromBody] AddCardLabelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .Include(c => c.Labels)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Etiket bu boardda mı kontrol et
            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == model.LabelId && l.BoardId == board.Id);

            if (label == null)
            {
                return NotFound("Etiket bulunamadı.");
            }

            // Etiket zaten eklenmiş mi
            if (card.Labels.Any(cl => cl.LabelId == model.LabelId))
            {
                return BadRequest("Bu etiket zaten bu karta eklenmiş.");
            }

            var cardLabel = new CardLabel
            {
                CardId = model.CardId,
                LabelId = model.LabelId
            };

            _context.CardLabels.Add(cardLabel);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = cardLabel.Id,
                cardId = cardLabel.CardId,
                labelId = cardLabel.LabelId,
                labelName = label.Name,
                labelColor = label.Color
            });
        }

        // POST: Cards/RemoveLabel
        [HttpPost]
        public async Task<IActionResult> RemoveLabel([FromBody] RemoveCardLabelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cardLabel = await _context.CardLabels
                .Include(cl => cl.Card)
                    .ThenInclude(c => c.BoardList)
                        .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(cl => cl.CardId == model.CardId && cl.LabelId == model.LabelId);

            if (cardLabel == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = cardLabel.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.CardLabels.Remove(cardLabel);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/AddComment
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] AddCardCommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            var comment = new CardComment
            {
                CardId = model.CardId,
                UserId = currentUser.Id,
                Content = model.Content,
                CreatedAt = DateTime.Now
            };

            _context.CardComments.Add(comment);
            await _context.SaveChangesAsync();

            // Yorum için kullanıcı bilgilerini tekrar çek
            var user = await _userManager.FindByIdAsync(currentUser.Id);

            return Ok(new
            {
                id = comment.Id,
                cardId = comment.CardId,
                userId = comment.UserId,
                userName = $"{user.FirstName} {user.LastName}",
                userProfilePicture = user.ProfilePicture,
                content = comment.Content,
                createdAt = comment.CreatedAt
            });
        }

        // POST: Cards/DeleteComment
        [HttpPost]
        public async Task<IActionResult> DeleteComment([FromBody] DeleteCardCommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var comment = await _context.CardComments
                .Include(cc => cc.Card)
                    .ThenInclude(c => c.BoardList)
                        .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(cc => cc.Id == model.CommentId);

            if (comment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = comment.Card.BoardList.Board;

            // Yorum sahibi veya pano sahibi mi kontrol et
            if (comment.UserId != currentUser.Id && board.UserId != currentUser.Id)
            {
                return Forbid();
            }

            _context.CardComments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/AddChecklist
        [HttpPost]
        public async Task<IActionResult> AddChecklist([FromBody] AddCardChecklistViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                .Include(c => c.Checklists)
                .FirstOrDefaultAsync(c => c.Id == model.CardId);

            if (card == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // En yüksek pozisyonu bul
            var maxPosition = card.Checklists.Any() ? card.Checklists.Max(cl => cl.Position) : 0;

            var checklist = new CardChecklist
            {
                CardId = model.CardId,
                Title = model.Title,
                Position = maxPosition + 1
            };

            _context.CardChecklists.Add(checklist);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = checklist.Id,
                cardId = checklist.CardId,
                title = checklist.Title,
                position = checklist.Position
            });
        }

        // POST: Cards/DeleteChecklist
        [HttpPost]
        public async Task<IActionResult> DeleteChecklist([FromBody] DeleteCardChecklistViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checklist = await _context.CardChecklists
                .Include(ccl => ccl.Card)
                    .ThenInclude(c => c.BoardList)
                        .ThenInclude(bl => bl.Board)
                .Include(ccl => ccl.Items)
                .FirstOrDefaultAsync(ccl => ccl.Id == model.ChecklistId);

            if (checklist == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = checklist.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.CardChecklists.Remove(checklist);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Cards/AddChecklistItem
        [HttpPost]
        public async Task<IActionResult> AddChecklistItem([FromBody] AddChecklistItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checklist = await _context.CardChecklists
                .Include(ccl => ccl.Card)
                    .ThenInclude(c => c.BoardList)
                        .ThenInclude(bl => bl.Board)
                .Include(ccl => ccl.Items)
                .FirstOrDefaultAsync(ccl => ccl.Id == model.ChecklistId);

            if (checklist == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = checklist.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // En yüksek pozisyonu bul
            var maxPosition = checklist.Items.Any() ? checklist.Items.Max(ci => ci.Position) : 0;

            var checklistItem = new ChecklistItem
            {
                ChecklistId = model.ChecklistId,
                Content = model.Content,
                IsCompleted = false,
                Position = maxPosition + 1
            };

            _context.ChecklistItems.Add(checklistItem);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = checklistItem.Id,
                checklistId = checklistItem.ChecklistId,
                content = checklistItem.Content,
                isCompleted = checklistItem.IsCompleted,
                position = checklistItem.Position
            });
        }

        // POST: Cards/UpdateChecklistItem
        [HttpPost]
        public async Task<IActionResult> UpdateChecklistItem([FromBody] UpdateChecklistItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checklistItem = await _context.ChecklistItems
                .Include(ci => ci.Checklist)
                    .ThenInclude(ccl => ccl.Card)
                        .ThenInclude(c => c.BoardList)
                            .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(ci => ci.Id == model.ItemId);

            if (checklistItem == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = checklistItem.Checklist.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            checklistItem.Content = model.Content;
            checklistItem.IsCompleted = model.IsCompleted;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = checklistItem.Id,
                checklistId = checklistItem.ChecklistId,
                content = checklistItem.Content,
                isCompleted = checklistItem.IsCompleted,
                position = checklistItem.Position
            });
        }

        // POST: Cards/DeleteChecklistItem
        [HttpPost]
        public async Task<IActionResult> DeleteChecklistItem([FromBody] DeleteChecklistItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checklistItem = await _context.ChecklistItems
                .Include(ci => ci.Checklist)
                    .ThenInclude(ccl => ccl.Card)
                        .ThenInclude(c => c.BoardList)
                            .ThenInclude(bl => bl.Board)
                .FirstOrDefaultAsync(ci => ci.Id == model.ItemId);

            if (checklistItem == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = checklistItem.Checklist.Card.BoardList.Board;

            // Pano sahibi veya üyesi mi kontrol et
            if (board.UserId != currentUser.Id &&
                !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.ChecklistItems.Remove(checklistItem);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Yardımcı metotlar
        private async Task<List<ApplicationUser>> GetAvailableBoardMembers(int boardId, int cardId)
        {
            var board = await _context.Boards
                .Include(b => b.User)
                .Include(b => b.Members)
                    .ThenInclude(bm => bm.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return new List<ApplicationUser>();
            }

            // Panonun üyeleri
            var boardMembers = new List<ApplicationUser> { board.User };
            boardMembers.AddRange(board.Members.Select(bm => bm.User));

            // Karta zaten atanmış üyeler
            var cardMembers = await _context.CardMembers
                .Where(cm => cm.CardId == cardId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            // Karta atanmamış üyeleri filtrele
            var availableMembers = boardMembers
                .Where(bm => !cardMembers.Contains(bm.Id))
                .ToList();

            return availableMembers;
        }
    }
}