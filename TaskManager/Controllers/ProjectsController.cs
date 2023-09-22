using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManager.Areas.Identity.Data;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Authorize(Roles = "ProjectManager, Administrator")]
    public class ProjectsController : Controller
    {
        private readonly TaskManagerContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsController(TaskManagerContext context, UserManager<User> usermanager)
        {
            _context = context;
            _userManager = usermanager;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var taskManagerContext = _context.Projects.Include(p => p.Manager);
            return View(await taskManagerContext.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName");

            var developers = await _userManager.GetUsersInRoleAsync("Developer");
            ViewData["DeveloperId"] = new SelectList(developers, "Id", "FullName");

            return View();
        }


        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Title,ManagerId")] Project project, string[] AllocatedDeveloperIds)
        {
            // Retrieve the manager and allocated developers
            project.Manager = await _userManager.FindByIdAsync(project.ManagerId);
            var allocatedDevelopers = await _userManager.Users.Where(u => AllocatedDeveloperIds.Contains(u.Id)).ToListAsync();

            // Assign allocated developers to the project
            foreach (var developer in allocatedDevelopers)
            {
                project.ProjectDevelopers.Add(new ProjectDeveloper { Project = project, User = developer });
            }
            if (!ModelState.IsValid)
            {
                var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
                ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName");

                var developers = await _userManager.GetUsersInRoleAsync("Developer");
                ViewData["DeveloperId"] = new SelectList(developers, "Id", "FullName");
            }
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }


            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Get the list of users with the role "ProjectManager"
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");

            // Populate the ViewData with these users
            ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName", project.ManagerId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,Title,ManagerId")] Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // If we reach here, it means the model state was not valid.
            // So, repopulate the dropdown list before returning to the view
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName", project.ManagerId);

            return View(project);
        }


        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'TaskManagerContext.Projects'  is null.");
            }
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Projects");
        }

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }
    }
}
