// Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;

namespace TrelloClone.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Kullanıcının oluşturduğu panoları getir
            var ownedBoards = await _context.Boards
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync();

            // Kullanıcının üye olduğu panoları getir
            var memberBoards = await _context.BoardMembers
                .Where(bm => bm.UserId == user.Id)
                .Include(bm => bm.Board)
                .Select(bm => bm.Board)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync();

            // Panoları birleştir ve çift kayıtları kaldır
            var allBoards = ownedBoards.Union(memberBoards).ToList();

            return View(allBoards);
        }
    }
}