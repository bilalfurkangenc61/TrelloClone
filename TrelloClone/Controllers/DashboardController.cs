using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;

namespace TrelloClone.Controllers
{
    // Ana dashboard sayfasını yöneten controller
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
            var currentUser = await _userManager.GetUserAsync(User);

            // Kullanıcının üye olduğu takımları getir
            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .ThenInclude(t => t.Boards)
                .Where(tm => tm.UserId == currentUser.Id && tm.IsActive)
                .Select(tm => tm.Team)
                .ToListAsync();

            return View(userTeams);
        }
    }
}