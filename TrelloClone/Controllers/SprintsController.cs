// Controllers/SprintsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrelloClone.ViewModels;

namespace TrelloClone.Controllers
{
    [Authorize]
    public class SprintsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SprintsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Sprints/Index/5 (5 = BoardId)
        public async Task<IActionResult> Index(int id)
        {
            var board = await _context.Boards
                .Include(b => b.User)
                .Include(b => b.Members)
                .Include(b => b.Sprints)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            return View(board);
        }


        // Pano içindeki sprintleri getirmek için partial view döndüren action
        [HttpGet]
        public IActionResult GetSprintsForBoard(int id)
        {
            var sprints = _context.Sprints
                .Where(s => s.BoardId == id)
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.StartDate)
                .ToList();

            return PartialView("_SprintsList", sprints);
        }

        // Sprint durumunu değiştirmek için API endpoint
        [HttpPost]
        public IActionResult ToggleActive(int id, [FromBody] ToggleActiveViewModel model)
        {
            var sprint = _context.Sprints.Find(id);

            if (sprint == null)
            {
                return NotFound();
            }

            // Eğer aktif yapılıyorsa, diğer aktif sprintleri pasif yap
            if (model.IsActive)
            {
                var activeSprints = _context.Sprints
                    .Where(s => s.BoardId == sprint.BoardId && s.IsActive && s.Id != id)
                    .ToList();

                foreach (var activeSprint in activeSprints)
                {
                    activeSprint.IsActive = false;
                }
            }

            sprint.IsActive = model.IsActive;
            _context.SaveChanges();

            return Ok();
        }



        // GET: Sprints/AllSprints
        // GET: Sprints/AllSprints
        public async Task<IActionResult> AllSprints()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Kullanıcının erişimi olan panoları bul
            var userBoards = await _context.Boards
                .Where(b => b.UserId == currentUser.Id || b.Members.Any(m => m.UserId == currentUser.Id))
                .ToListAsync();

            var boardIds = userBoards.Select(b => b.Id).ToList();

            // Bu panolara ait tüm sprintleri ve panoya ait olmayan sprintleri getir
            var sprints = await _context.Sprints
                .Include(s => s.Board)
                .Where(s => s.BoardId == null || boardIds.Contains(s.BoardId.Value))
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.StartDate)
                .ToListAsync();

            return View(sprints);
        }

        // GET: Sprints/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Board)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.BoardList)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Members)
                        .ThenInclude(m => m.User)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Labels)
                        .ThenInclude(l => l.Label)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = sprint.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            return View(sprint);
        }

        // GET: Sprints/Create/5 (5 = BoardId)
        // POST: Sprints/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sprint sprint)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Eğer BoardId varsa, board'u kontrol et
                    if (sprint.BoardId.HasValue)
                    {
                        var board = await _context.Boards
                            .Include(b => b.Members)
                            .FirstOrDefaultAsync(b => b.Id == sprint.BoardId);

                        if (board == null)
                        {
                            return NotFound("Pano bulunamadı");
                        }

                        var currentUser = await _userManager.GetUserAsync(User);
                        if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
                        {
                            return Forbid();
                        }

                        // Eğer bu sprint aktifse, diğer aktif sprintleri pasif yap
                        if (sprint.IsActive)
                        {
                            var activeSprints = await _context.Sprints
                                .Where(s => s.BoardId == sprint.BoardId && s.IsActive)
                                .ToListAsync();

                            foreach (var activeSprint in activeSprints)
                            {
                                activeSprint.IsActive = false;
                                _context.Update(activeSprint);
                            }
                        }
                    }

                    sprint.CreatedAt = DateTime.Now;
                    _context.Add(sprint);
                    await _context.SaveChangesAsync();

                    // Eğer BoardId yoksa, tüm sprintler sayfasına yönlendir
                    if (!sprint.BoardId.HasValue)
                    {
                        return RedirectToAction(nameof(AllSprints));
                    }

                    return RedirectToAction(nameof(Index), new { id = sprint.BoardId });
                }
                else
                {
                    // ModelState hata mesajlarını logla
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Key: {state.Key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata mesajını logla
                System.Diagnostics.Debug.WriteLine($"Sprint oluşturma hatası: {ex.Message}");
                ModelState.AddModelError("", $"Sprint oluşturulurken bir hata oluştu: {ex.Message}");
            }

            return View(sprint);
        }

  

        // GET: Sprints/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = sprint.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            return View(sprint);
        }

        // POST: Sprints/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sprint sprint)
        {
            if (id != sprint.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSprint = await _context.Sprints
                        .Include(s => s.Board)
                            .ThenInclude(b => b.Members)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (existingSprint == null)
                    {
                        return NotFound();
                    }

                    var currentUser = await _userManager.GetUserAsync(User);
                    var board = existingSprint.Board;
                    if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
                    {
                        return Forbid();
                    }

                    // Eğer bu sprint aktifse, diğer aktif sprintleri pasif yap
                    if (sprint.IsActive && !existingSprint.IsActive)
                    {
                        var activeSprints = await _context.Sprints
                            .Where(s => s.BoardId == existingSprint.BoardId && s.IsActive && s.Id != id)
                            .ToListAsync();

                        foreach (var activeSprint in activeSprints)
                        {
                            activeSprint.IsActive = false;
                            _context.Update(activeSprint);
                        }
                    }

                    existingSprint.Name = sprint.Name;
                    existingSprint.Description = sprint.Description;
                    existingSprint.StartDate = sprint.StartDate;
                    existingSprint.EndDate = sprint.EndDate;
                    existingSprint.Goal = sprint.Goal;
                    existingSprint.IsActive = sprint.IsActive;
                    existingSprint.IsCompleted = sprint.IsCompleted;
                    existingSprint.UpdatedAt = DateTime.Now;

                    _context.Update(existingSprint);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = sprint.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SprintExists(sprint.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(sprint);
        }

        // GET: Sprints/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Members)
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = sprint.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            return View(sprint);
        }

        // POST: Sprints/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var board = sprint.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Sprinte bağlı kartların SprintId'sini null yap
            var cards = await _context.Cards.Where(c => c.SprintId == id).ToListAsync();
            foreach (var card in cards)
            {
                card.SprintId = null;
                card.UpdatedAt = DateTime.Now;
                _context.Update(card);
            }

            var boardId = sprint.BoardId;
            _context.Sprints.Remove(sprint);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id = boardId });
        }

        // POST: Sprints/AddCardToSprint
        [HttpPost]
        public async Task<IActionResult> AddCardToSprint(int cardId, int sprintId, int? storyPoints)
        {
            if (cardId <= 0 || sprintId <= 0)
            {
                return BadRequest("Geçersiz ID değerleri.");
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                    .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
            {
                return NotFound("Kart bulunamadı.");
            }

            var sprint = await _context.Sprints
                .Include(s => s.Board)
                    .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return NotFound("Sprint bulunamadı.");
            }

            // Kullanıcı yetki kontrolü
            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Kart ve sprint aynı board'a ait mi kontrolü
            if (card.BoardList.BoardId != sprint.BoardId)
            {
                return BadRequest("Kart ve sprint aynı panoya ait değil.");
            }

            // Kartı sprinte ekle
            card.SprintId = sprintId;
            card.StoryPoints = storyPoints;
            card.UpdatedAt = DateTime.Now;

            _context.Update(card);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // POST: Sprints/RemoveCardFromSprint
        [HttpPost]
        public async Task<IActionResult> RemoveCardFromSprint(int cardId)
        {
            if (cardId <= 0)
            {
                return BadRequest("Geçersiz kart ID değeri.");
            }

            var card = await _context.Cards
                .Include(c => c.BoardList)
                    .ThenInclude(bl => bl.Board)
                    .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
            {
                return NotFound("Kart bulunamadı.");
            }

            // Kullanıcı yetki kontrolü
            var currentUser = await _userManager.GetUserAsync(User);
            var board = card.BoardList.Board;
            if (board.UserId != currentUser.Id && !board.Members.Any(m => m.UserId == currentUser.Id))
            {
                return Forbid();
            }

            // Kartı sprintten çıkar
            card.SprintId = null;
            card.UpdatedAt = DateTime.Now;

            _context.Update(card);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private bool SprintExists(int id)
        {
            return _context.Sprints.Any(e => e.Id == id);
        }
    }
}