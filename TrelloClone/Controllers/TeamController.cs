using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.ViewModels;

namespace TrelloClone.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Takımlarım sayfası
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Kullanıcının üye olduğu takımları getir
            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.Boards)
                .Where(tm => tm.UserId == currentUser.Id && tm.IsActive)
                .OrderByDescending(tm => tm.JoinedAt)
                .ToListAsync();

            return View(userTeams);
        }

        // Takım detayları
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Boards)
                    .ThenInclude(b => b.Lists)
                        .ThenInclude(l => l.Cards)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            if (team == null)
            {
                return NotFound("Takım bulunamadı.");
            }

            // Kullanıcının bu takımda üye olup olmadığını kontrol et
            var userMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == currentUser.Id && tm.IsActive);

            if (userMembership == null)
            {
                return Forbid("Bu takıma erişim yetkiniz yok.");
            }

            ViewBag.UserRole = userMembership.Role;
            ViewBag.CurrentUserId = currentUser.Id;

            return View(team);
        }

        // Takım oluşturma sayfası
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Takım oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTeamViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Yeni takım oluştur
                var team = new Team
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Teams.Add(team);
                await _context.SaveChangesAsync();

                // Oluşturan kişiyi admin olarak ekle
                var teamMember = new TeamMember
                {
                    TeamId = team.Id,
                    UserId = currentUser.Id,
                    Role = UserRole.Admin,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Takım başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Details), new { id = team.Id });
            }

            return View(model);
        }

        // Takım düzenleme sayfası
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var team = await _context.Teams.FindAsync(id);
            if (team == null || !team.IsActive)
            {
                return NotFound();
            }

            // Kullanıcının admin yetkisi var mı kontrol et
            var userMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == currentUser.Id && tm.IsActive);

            if (userMembership == null || userMembership.Role != UserRole.Admin)
            {
                return Forbid("Bu işlem için admin yetkisi gerekli.");
            }

            var viewModel = new EditTeamViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description
            };

            return View(viewModel);
        }

        // Takım düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTeamViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var team = await _context.Teams.FindAsync(model.Id);
                if (team == null || !team.IsActive)
                {
                    return NotFound();
                }

                // Admin yetkisi kontrolü
                var userMembership = await _context.TeamMembers
                    .FirstOrDefaultAsync(tm => tm.TeamId == model.Id && tm.UserId == currentUser.Id && tm.IsActive);

                if (userMembership == null || userMembership.Role != UserRole.Admin)
                {
                    return Forbid();
                }

                team.Name = model.Name;
                team.Description = model.Description;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Takım bilgileri güncellendi!";
                return RedirectToAction(nameof(Details), new { id = team.Id });
            }

            return View(model);
        }

        // Takım üyesi ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int teamId, string email, UserRole role = UserRole.Member)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Admin yetkisi kontrolü
            var adminMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == currentUser.Id && tm.IsActive);

            if (adminMembership == null || adminMembership.Role != UserRole.Admin)
            {
                return Json(new { success = false, message = "Bu işlem için admin yetkisi gerekli." });
            }

            // Eklenecek kullanıcıyı bul
            var userToAdd = await _userManager.FindByEmailAsync(email);
            if (userToAdd == null)
            {
                return Json(new { success = false, message = "Bu e-posta adresine sahip kullanıcı bulunamadı." });
            }

            // Zaten üye mi kontrol et
            var existingMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userToAdd.Id);

            if (existingMembership != null)
            {
                if (existingMembership.IsActive)
                {
                    return Json(new { success = false, message = "Bu kullanıcı zaten takımda." });
                }
                else
                {
                    // Devre dışı üyeliği aktif hale getir
                    existingMembership.IsActive = true;
                    existingMembership.Role = role;
                    existingMembership.JoinedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Yeni üyelik oluştur
                var newMembership = new TeamMember
                {
                    TeamId = teamId,
                    UserId = userToAdd.Id,
                    Role = role,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.TeamMembers.Add(newMembership);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{userToAdd.FirstName} {userToAdd.LastName} takıma eklendi.",
                user = new
                {
                    id = userToAdd.Id,
                    name = $"{userToAdd.FirstName} {userToAdd.LastName}",
                    email = userToAdd.Email,
                    role = role.ToString()
                }
            });
        }

        // Takım üyesi rolü değiştirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberRole(int teamId, string userId, UserRole newRole)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Admin yetkisi kontrolü
            var adminMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == currentUser.Id && tm.IsActive);

            if (adminMembership == null || adminMembership.Role != UserRole.Admin)
            {
                return Json(new { success = false, message = "Bu işlem için admin yetkisi gerekli." });
            }

            var memberToUpdate = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

            if (memberToUpdate == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            memberToUpdate.Role = newRole;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Üye rolü güncellendi." });
        }

        // Takım üyesi çıkarma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int teamId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Admin yetkisi kontrolü veya kendi kendini çıkarma
            var adminMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == currentUser.Id && tm.IsActive);

            if (adminMembership == null || (adminMembership.Role != UserRole.Admin && currentUser.Id != userId))
            {
                return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
            }

            var memberToRemove = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

            if (memberToRemove == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            // Son admin çıkarılamaz
            var adminCount = await _context.TeamMembers
                .CountAsync(tm => tm.TeamId == teamId && tm.Role == UserRole.Admin && tm.IsActive);

            if (memberToRemove.Role == UserRole.Admin && adminCount == 1)
            {
                return Json(new { success = false, message = "Son admin takımdan çıkarılamaz." });
            }

            memberToRemove.IsActive = false;
            await _context.SaveChangesAsync();

            var message = currentUser.Id == userId ? "Takımdan ayrıldınız." : "Üye takımdan çıkarıldı.";
            var redirectNeeded = currentUser.Id == userId;

            return Json(new { success = true, message = message, redirectNeeded = redirectNeeded });
        }
    }
}