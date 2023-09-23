using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Areas.Identity.Data;
using TaskManager.ViewModels;


namespace TaskManager.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly TaskManagerContext _context;
        private readonly UserManager<User> _userManager;

        public UsersController(TaskManagerContext context, UserManager<User> usermanager)
        {
            _context = context;
            _userManager = usermanager;
        }


        // GET: UsersController
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModels = new List<UserRolesViewModel>();
            foreach (var user in users)
            {
                var thisViewModel = new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = await _userManager.GetRolesAsync(user)
                };
                userRolesViewModels.Add(thisViewModel);
            }
            return View(userRolesViewModels);
        }

        // GET: UsersController/AssignRole
        [HttpGet]
        public async Task<IActionResult> AssignRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _context.Roles.Select(r => r.Name).ToList();

            var roleSelections = allRoles.Where(roleName =>
                !roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                userRoles.Contains("Administrator"))
            .Select(roleName => new RoleSelection
            {
                RoleName = roleName,
                IsSelected = userRoles.Contains(roleName)
            }).ToList();

            var viewModel = new AssignRoleViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Roles = roleSelections
            };

            return View(viewModel);
        }

        // POST: UsersController/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole(AssignRoleViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            bool isUserAdmin = currentRoles.Contains("Administrator");

            if (isUserAdmin)
            {
                foreach (var key in ModelState.Keys.ToList())
                {
                    ModelState[key].Errors.Clear();
                }

                // Add the specific error message
                ModelState.AddModelError(string.Empty, "Cannot edit the Administrator role.");

                // Repopulate the roles list
                model.Roles = _context.Roles
                    .Where(r => r.Name != "Administrator" || currentRoles.Contains("Administrator"))
                    .Select(r => new RoleSelection
                    {
                        RoleName = r.Name,
                        IsSelected = currentRoles.Contains(r.Name)
                    }).ToList();

                return View(model);
            }

            if (currentRoles.Contains(model.SelectedRoleName))
            {
                ModelState.AddModelError(string.Empty, $"User is already {model.SelectedRoleName}");
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            if (!string.IsNullOrEmpty(model.SelectedRoleName))
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRoleName);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UsersController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UsersController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsersController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsersController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UsersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsersController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
