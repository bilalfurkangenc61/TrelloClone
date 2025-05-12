// Controllers/BoardsController.cs
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
    public class BoardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BoardsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Boards/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Boards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBoardViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var board = new Board
                {
                    Title = model.Title,
                    Description = model.Description,
                    BackgroundColor = model.BackgroundColor ?? "#0079BF",
                    BackgroundImage = "", // Boş string verin, null değil
                    IsPublic = model.IsPublic,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Add(board);
                await _context.SaveChangesAsync();

                // Varsayılan listeler ekle
                await CreateDefaultLists(board.Id);

                return RedirectToAction("Details", new { id = board.Id });
            }
            return View(model);
        }

        // GET: Boards/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var board = await _context.Boards
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Cards)
                        .ThenInclude(c => c.Members)
                            .ThenInclude(m => m.User)
                .Include(b => b.User)
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Sadece pano sahibi veya üyeler erişebilir
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id) && !board.IsPublic)
            {
                return Forbid();
            }

            // Listeleri sırala
            board.Lists = board.Lists.OrderBy(l => l.Position).ToList();

            // Her listenin kartlarını sırala
            foreach (var list in board.Lists)
            {
                list.Cards = list.Cards.OrderBy(c => c.Position).ToList();
            }

            return View(board);
        }

        // GET: Boards/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var board = await _context.Boards.FindAsync(id);
            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Sadece pano sahibi düzenleyebilir
            if (board.UserId != currentUser.Id)
            {
                return Forbid();
            }

            var viewModel = new EditBoardViewModel
            {
                Id = board.Id,
                Title = board.Title,
                Description = board.Description,
                BackgroundColor = board.BackgroundColor,
                BackgroundImage = board.BackgroundImage,
                IsPublic = board.IsPublic
            };

            return View(viewModel);
        }

        // POST: Boards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditBoardViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var board = await _context.Boards.FindAsync(id);
                if (board == null)
                {
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);

                // Sadece pano sahibi düzenleyebilir
                if (board.UserId != currentUser.Id)
                {
                    return Forbid();
                }

                board.Title = model.Title;
                board.Description = model.Description;
                board.BackgroundColor = model.BackgroundColor;
                board.BackgroundImage = model.BackgroundImage;
                board.IsPublic = model.IsPublic;
                board.UpdatedAt = DateTime.Now;

                try
                {
                    _context.Update(board);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BoardExists(board.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Details), new { id = board.Id });
            }

            return View(model);
        }

        // POST: Boards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var board = await _context.Boards
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Cards)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Sadece pano sahibi silebilir
            if (board.UserId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Boards/AddMember
        [HttpPost]
        public async Task<IActionResult> AddMember([FromBody] AddBoardMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == model.BoardId);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Sadece pano sahibi üye ekleyebilir
            if (board.UserId != currentUser.Id)
            {
                return Forbid();
            }

            var userToAdd = await _userManager.FindByEmailAsync(model.Email);
            if (userToAdd == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcı zaten üye mi?
            if (board.Members.Any(m => m.UserId == userToAdd.Id))
            {
                return BadRequest("Bu kullanıcı zaten bu panoya üye.");
            }

            // Kullanıcı kendisi pano sahibi mi?
            if (board.UserId == userToAdd.Id)
            {
                return BadRequest("Pano sahibi zaten üyedir.");
            }

            var boardMember = new BoardMember
            {
                BoardId = model.BoardId,
                UserId = userToAdd.Id,
                Role = "Member", // Varsayılan rol
                CreatedAt = DateTime.Now
            };

            _context.BoardMembers.Add(boardMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = boardMember.Id,
                boardId = boardMember.BoardId,
                userId = boardMember.UserId,
                userName = $"{userToAdd.FirstName} {userToAdd.LastName}",
                userEmail = userToAdd.Email,
                userProfilePicture = userToAdd.ProfilePicture
            });
        }

        // POST: Boards/RemoveMember
        [HttpPost]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveBoardMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = await _context.Boards.FindAsync(model.BoardId);
            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Sadece pano sahibi üye çıkarabilir
            if (board.UserId != currentUser.Id)
            {
                return Forbid();
            }

            var boardMember = await _context.BoardMembers
                .FirstOrDefaultAsync(bm => bm.BoardId == model.BoardId && bm.UserId == model.UserId);

            if (boardMember == null)
            {
                return NotFound();
            }

            _context.BoardMembers.Remove(boardMember);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Boards/AddList
        [HttpPost]
        public async Task<IActionResult> AddList([FromBody] AddBoardListViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = await _context.Boards
                .Include(b => b.Lists)
                .FirstOrDefaultAsync(b => b.Id == model.BoardId);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Pano sahibi veya üye olup olmadığını kontrol et
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // En yüksek pozisyonu bul
            var maxPosition = board.Lists.Any() ? board.Lists.Max(l => l.Position) : 0;

            var boardList = new BoardList
            {
                Title = model.Title,
                BoardId = model.BoardId,
                Position = maxPosition + 1,
                CreatedAt = DateTime.Now
            };

            _context.BoardLists.Add(boardList);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = boardList.Id,
                title = boardList.Title,
                position = boardList.Position,
                boardId = boardList.BoardId,
                createdAt = boardList.CreatedAt
            });
        }

        // POST: Boards/UpdateListPosition
        [HttpPost]
        public async Task<IActionResult> UpdateListPosition([FromBody] UpdateListPositionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = await _context.Boards
                .Include(b => b.Lists)
                .FirstOrDefaultAsync(b => b.Id == model.BoardId);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Pano sahibi veya üye olup olmadığını kontrol et
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            var boardList = board.Lists.FirstOrDefault(l => l.Id == model.ListId);
            if (boardList == null)
            {
                return NotFound();
            }

            // Tüm listeleri yeni pozisyonlara göre güncelle
            var lists = board.Lists.OrderBy(l => l.Position).ToList();
            lists.Remove(boardList);
            lists.Insert(model.NewPosition, boardList);

            for (int i = 0; i < lists.Count; i++)
            {
                lists[i].Position = i;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Boards/UpdateList
        [HttpPost]
        public async Task<IActionResult> UpdateList([FromBody] UpdateBoardListViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var boardList = await _context.BoardLists
                .Include(bl => bl.Board)
                .FirstOrDefaultAsync(bl => bl.Id == model.ListId);

            if (boardList == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Pano sahibi veya üye olup olmadığını kontrol et
            if (boardList.Board.UserId != currentUser.Id && !boardList.Board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            boardList.Title = model.Title;
            boardList.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = boardList.Id,
                title = boardList.Title,
                updatedAt = boardList.UpdatedAt
            });
        }

        // POST: Boards/DeleteList
        [HttpPost]
        public async Task<IActionResult> DeleteList([FromBody] DeleteBoardListViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var boardList = await _context.BoardLists
                .Include(bl => bl.Board)
                .Include(bl => bl.Cards)
                .FirstOrDefaultAsync(bl => bl.Id == model.ListId);

            if (boardList == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Pano sahibi veya üye olup olmadığını kontrol et
            if (boardList.Board.UserId != currentUser.Id && !boardList.Board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            _context.BoardLists.Remove(boardList);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Yardımcı metotlar
        private bool BoardExists(int id)
        {
            return _context.Boards.Any(e => e.Id == id);
        }

        private async Task CreateDefaultLists(int boardId)
        {
            // Varsayılan listeler
            var defaultLists = new[] { "Yapılacaklar", "Devam Edenler", "Tamamlananlar" };

            for (int i = 0; i < defaultLists.Length; i++)
            {
                var list = new BoardList
                {
                    Title = defaultLists[i],
                    BoardId = boardId,
                    Position = i,
                    CreatedAt = DateTime.Now
                };

                _context.BoardLists.Add(list);
            }

            await _context.SaveChangesAsync();
        }
    }
}