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
using TaskManager.ViewModels.TaskVM;
using Task = TaskManager.Models.Task;

namespace TaskManager.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly TaskManagerContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsController(TaskManagerContext context, UserManager<User> usermanager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = usermanager ?? throw new ArgumentNullException(nameof(usermanager));
        }

        private const int PageSize = 10;

        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var projects = await _context.Projects.Include(p => p.Manager).OrderBy(p => p.Title).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(_context.Projects.Count() / (double)PageSize);
            return View(projects);
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
                .Include(p => p.Tasks)
                .Include(p => p.ProjectDevelopers)
                    .ThenInclude(pd => pd.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> Create()
        {
            var isAdmin = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Administrator");

            if (isAdmin)
            {
                var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
                ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName");
            }

            var developers = await _userManager.GetUsersInRoleAsync("Developer");
            ViewData["DeveloperId"] = new SelectList(developers, "Id", "FullName");

            return View();
        }

        [Authorize(Roles = "ProjectManager, Administrator")]
        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Title,ManagerId")] Project project, string[] AllocatedDeveloperIds)
        {
            project.Manager = await _userManager.FindByIdAsync(project.ManagerId);

            var allocatedDevelopers = await _userManager.Users.Where(u => AllocatedDeveloperIds.Contains(u.Id)).ToListAsync();

            foreach (var developer in allocatedDevelopers)
            {
                project.ProjectDevelopers.Add(new ProjectDeveloper { Project = project, User = developer });
            }

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            ViewData["ManagerId"] = new SelectList(projectManagers, "Id", "FullName");

            var developers = await _userManager.GetUsersInRoleAsync("Developer");
            ViewData["DeveloperId"] = new SelectList(developers, "Id", "FullName");

            return View(project);
        }



        // GET: Projects/Edit/5
        [Authorize(Roles = "ProjectManager, Administrator")]
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
        [Authorize(Roles = "ProjectManager, Administrator")]
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

        // GET: Add Task
        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> AddTask(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the project with related data in one call
            var project = await _context.Projects
                .Include(p => p.Manager)
                .Include(pd => pd.ProjectDevelopers)
                .ThenInclude(pd => pd.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            // Extract distinct developers associated with the project
            var devusers = project.ProjectDevelopers
                .Select(pd => pd.User)
                .DistinctBy(user => user.Id)
                .OrderBy(user => user.Id)
                .ToList();

            TaskVM vm = new TaskVM(devusers)
            {
                Task = new Models.Task()
            };

            return View(vm);
        }

        // POST: Add Task to Project
        [Authorize(Roles = "ProjectManager, Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(int? id, TaskVM taskVM)
        {
            if (id == null || taskVM == null)
            {
                return NotFound();
            }

            ModelState.Remove("Task.Project"); // Remove the Project property from the ModelState as it is not needed and will cause an error
            ModelState.Remove("SelectDevs");
            ModelState.Remove("Task.TaskDevelopers");

            // Check ModelState validity right at the start of the action
            if (!ModelState.IsValid)
            {
                // Repopulate the view model's data and return
                var project = await _context.Projects
                    .Include(p => p.ProjectDevelopers)
                    .ThenInclude(pd => pd.User)
                    .FirstOrDefaultAsync(m => m.ProjectId == id.Value);

                // Extract distinct developers associated with the project
                var devusers = project.ProjectDevelopers
                    .Select(pd => pd.User)
                    .DistinctBy(user => user.Id)
                    .OrderBy(user => user.Id)
                    .ToList();

                taskVM.SelectDevs = devusers.Select(d => new SelectListItem { Text = d.FullName, Value = d.Id.ToString() }).ToList();
                return View(taskVM);
            }

            taskVM.Task.Project = await _context.Projects.FindAsync(id);
            taskVM.Task.IsCompleted = false;
            taskVM.Task.ProjectId = id.Value;

            // Handle the many-to-many relationship with developers
            taskVM.Task.TaskDevelopers = new List<TaskDeveloper>();
            foreach (var devId in taskVM.SelectedDevIds)
            {
                taskVM.Task.TaskDevelopers.Add(new TaskDeveloper
                {
                    Task = taskVM.Task,
                    DeveloperId = devId
                });
            }

            // Check if the assigned developers are part of the project's developers
            var projectDevelopersIds = _context.ProjectDevelopers
                .Where(pd => pd.ProjectId == id.Value)
                .Select(pd => pd.UserId)
                .ToList();

            bool isDeveloperMismatch = taskVM.SelectedDevIds.Any(devId => !projectDevelopersIds.Contains(devId));
            if (isDeveloperMismatch)
            {
                ModelState.AddModelError("", "One or more selected developers are not associated with this project.");
                return View(taskVM);
            }

            // Add the task to the context
            _context.Tasks.Add(taskVM.Task);

            // Save changes
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Projects");
        }



        // POST: Projects/Delete/5
        [Authorize(Roles = "ProjectManager, Administrator")]
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

        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        // GET: Projects/Tasks/5
        public async Task<IActionResult> Tasks(int? projectId, int page = 1)
        {
            if (projectId == null)
            {
                return NotFound();
            }
            ViewData["ProjectId"] = projectId;
            ViewData["ProjectTitle"] = _context.Projects.Find(projectId).Title;

            var isDeveloper = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Developer");
            var userId = _userManager.GetUserId(User);

            IQueryable<Task> tasksQuery = _context.Tasks.Include(t => t.TaskDevelopers).ThenInclude(td => td.Developer).Where(t => t.ProjectId == projectId);

            if (isDeveloper)
            {
                tasksQuery = tasksQuery.Where(t => t.TaskDevelopers.Any(td => td.DeveloperId == userId));
            }

            var tasks = await tasksQuery.OrderBy(t => t.Title).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(tasksQuery.Count() / (double)PageSize);

            return View(tasks);
        }


        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> MyTasks(int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            var tasks = await _context.Tasks
                                      .Include(t => t.TaskDevelopers)
                                      .Where(t => t.TaskDevelopers.Any(td => td.DeveloperId == userId))
                                      .OrderBy(t => t.Title)
                                      .Skip((page - 1) * PageSize)
                                      .Take(PageSize)
                                      .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(_context.Tasks.Count(t => t.TaskDevelopers.Any(td => td.DeveloperId == userId)) / (double)PageSize);

            return View(tasks);
        }

        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> MyProjects()
        {
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                                         .Include(p => p.ProjectDevelopers)
                                         .Where(p => p.ProjectDevelopers.Any(pd => pd.UserId == userId))
                                         .ToListAsync();

            return View(projects);
        }

        // GET: Projects/EditTask/5
        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> EditTask(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.TaskDevelopers)
                .ThenInclude(td => td.Developer)
                .FirstOrDefaultAsync(t => t.TaskId == id.Value);

            if (task == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var isDeveloper = await _userManager.IsInRoleAsync(user, "Developer");

            // Security check: Ensure developers can only edit their own tasks
            if (isDeveloper && !task.TaskDevelopers.Any(td => td.DeveloperId == userId))
            {
                return Forbid();
            }

            var taskDevelopers = await _context.TaskDevelopers.Where(td => td.TaskId == id.Value).ToListAsync();
            var developerIds = taskDevelopers.Select(td => td.DeveloperId).ToArray();

            var taskVM = new TaskVM
            {
                Task = task,
                SelectedDevIds = developerIds.ToList()
            };

            var project = await _context.Projects
                .Include(p => p.ProjectDevelopers)
                    .ThenInclude(pd => pd.User)
                .FirstOrDefaultAsync(m => m.ProjectId == task.ProjectId);

            // Extract distinct developers associated with the project
            var devusers = project.ProjectDevelopers
                .Select(pd => pd.User)
                .DistinctBy(user => user.Id)
                .OrderBy(user => user.Id)
                .ToList();

            taskVM.SelectDevs = devusers.Select(d => new SelectListItem { Text = d.FullName, Value = d.Id.ToString() }).ToList();

            return View(taskVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> EditTask(int id, TaskVM taskVM)
        {
            if (id != taskVM.Task.TaskId)
            {
                return NotFound();
            }

            var originalTask = await _context.Tasks.FindAsync(id);
            if (originalTask == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var isDeveloper = await _userManager.IsInRoleAsync(user, "Developer");

            // Distinguish between developers and other roles
            if (isDeveloper)
            {
                // Developers can only edit RequiredHours
                originalTask.RequiredHours = taskVM.Task.RequiredHours;
            }
            else
            {
                // Project managers and admins can edit all fields
                originalTask.Title = taskVM.Task.Title;
                originalTask.RequiredHours = taskVM.Task.RequiredHours;
                originalTask.Priority = taskVM.Task.Priority;
                originalTask.IsCompleted = taskVM.Task.IsCompleted;

                // Handle the many-to-many relationship with developers
                var existingTaskDevelopers = _context.TaskDevelopers.Where(td => td.TaskId == id).ToList();
                _context.TaskDevelopers.RemoveRange(existingTaskDevelopers);

                foreach (var devId in taskVM.SelectedDevIds)
                {
                    _context.TaskDevelopers.Add(new TaskDeveloper
                    {
                        TaskId = id,
                        DeveloperId = devId
                    });
                }
            }

            // No need to explicitly update the task because you're modifying the original tracked entity
            await _context.SaveChangesAsync();

            return RedirectToAction("Tasks", new { projectId = originalTask.ProjectId });
        }


        // POST: Projects/MarkTaskCompleted/5
        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkTaskCompleted(int taskId)
        {
            var taskToMark = await _context.Tasks.FindAsync(taskId);

            if (taskToMark == null)
            {
                return NotFound();
            }

            taskToMark.IsCompleted = true;

            _context.Update(taskToMark);
            await _context.SaveChangesAsync();

            // Redirect to the project's tasks list after marking the task as completed.
            return RedirectToAction("Tasks", new { projectId = taskToMark.ProjectId });
        }

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }
    }
}
