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
using TaskManager.ViewModels;
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
                var project = await _context.Projects
                    .Include(p => p.ProjectDevelopers)
                    .ThenInclude(pd => pd.User)
                    .FirstOrDefaultAsync(m => m.ProjectId == id.Value);

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

            taskVM.Task.TaskDevelopers = new List<TaskDeveloper>();

            if (taskVM.SelectedDevIds?.Any() == true)
            {
                foreach (var devId in taskVM.SelectedDevIds)
                {
                    taskVM.Task.TaskDevelopers.Add(new TaskDeveloper
                    {
                        Task = taskVM.Task,
                        DeveloperId = devId
                    });
                }

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
            }

            _context.Tasks.Add(taskVM.Task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Projects");
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
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
        [Authorize(Roles = "ProjectManager, Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.TaskDevelopers)
                .Include(p => p.ProjectDevelopers)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project != null)
            {
                // Remove all related TaskDevelopers for tasks related to the project
                foreach (var task in project.Tasks)
                {
                    _context.TaskDevelopers.RemoveRange(task.TaskDevelopers);
                }

                // Remove all related Tasks for the project
                _context.Tasks.RemoveRange(project.Tasks);

                // Remove all related ProjectDevelopers for the project
                _context.ProjectDevelopers.RemoveRange(project.ProjectDevelopers);

                // Remove the project itself
                _context.Projects.Remove(project);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Projects");
        }


        // GET: Projects/DeleteTask/5
        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> DeleteTask(int? id)
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

            return View(task);
        }

        // POST: Tasks/Delete/5
        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        [HttpPost, ActionName("DeleteTask")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaskConfirmed(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskDevelopers)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task != null)
            {
                // Remove all related TaskDevelopers for the task
                _context.TaskDevelopers.RemoveRange(task.TaskDevelopers);

                // Remove the task itself
                _context.Tasks.Remove(task);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Tasks", new { id = task.ProjectId });
        }



        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> Tasks(bool excludeCompleted, bool excludeAssigned, int? id, int page = 1, string sortBy = "Title")
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewData["ProjectId"] = id;
            ViewData["ProjectTitle"] = (await _context.Projects.FindAsync(id)).Title;

            var isDeveloper = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Developer");
            var userId = _userManager.GetUserId(User);

            IQueryable<Task> tasksQuery = _context.Tasks.Include(t => t.TaskDevelopers).ThenInclude(td => td.Developer).Where(t => t.ProjectId == id);

            if (excludeCompleted)
            {
                tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
            }
            if (excludeAssigned)
            {
                tasksQuery = tasksQuery.Where(t => !t.TaskDevelopers.Any());
            }
            if (isDeveloper)
            {
                tasksQuery = tasksQuery.Where(t => t.TaskDevelopers.Any(td => td.DeveloperId == userId));
            }

            switch (sortBy)
            {
                case "RequiredHours":
                    tasksQuery = tasksQuery.OrderByDescending(t => t.RequiredHours);
                    break;
                case "Priority":
                    tasksQuery = tasksQuery.OrderByDescending(t => t.Priority);
                    break;
                default:
                    tasksQuery = tasksQuery.OrderByDescending(t => t.Title);
                    break;
            }

            var tasks = await tasksQuery.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(tasksQuery.Count() / (double)PageSize);
            ViewBag.SortBy = sortBy;
            ViewBag.excludeCompleted = excludeCompleted;
            ViewBag.excludeAssigned = excludeAssigned;

            Console.WriteLine(ViewBag.ExcludeCompleted);  // For debugging purposes
            Console.WriteLine(ViewBag.ExcludeAssigned);   // For debugging purposes

            return View(tasks);
        }

        [Authorize(Roles = "ProjectManager, Administrator, Developer")]
        public async Task<IActionResult> MyTasks(int page = 1, string sortBy = "Title", bool excludeCompleted = false)
        {
            var userId = _userManager.GetUserId(User);

            IQueryable<Task> tasksQuery = _context.Tasks
                                              .Include(t => t.TaskDevelopers)
                                              .Where(t => t.TaskDevelopers.Any(td => td.DeveloperId == userId));

            if (excludeCompleted)
            {
                tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
            }

            switch (sortBy)
            {
                case "RequiredHours":
                    tasksQuery = tasksQuery.OrderByDescending(t => t.RequiredHours);
                    break;
                case "Priority":
                    tasksQuery = tasksQuery.OrderByDescending(t => t.Priority);
                    break;
                default:
                    tasksQuery = tasksQuery.OrderByDescending(t => t.Title);
                    break;
            }

            var tasks = await tasksQuery.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(tasksQuery.Count() / (double)PageSize);
            ViewBag.SortBy = sortBy;
            ViewBag.ExcludeCompleted = excludeCompleted;

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

            // If the current user is a developer
            if (isDeveloper)
            {
                // Developers can only edit RequiredHours and completion status
                originalTask.RequiredHours = taskVM.Task.RequiredHours;
                originalTask.IsCompleted = taskVM.Task.IsCompleted;
            }
            else
            {
                // Handle the complete task editing for other roles
                originalTask.Title = taskVM.Task.Title;
                originalTask.RequiredHours = taskVM.Task.RequiredHours;
                originalTask.Priority = taskVM.Task.Priority;

                // Handle the many-to-many relationship with developers
                var existingTaskDevelopers = _context.TaskDevelopers.Where(td => td.TaskId == id).ToList();
                _context.TaskDevelopers.RemoveRange(existingTaskDevelopers);

                if (taskVM.SelectedDevIds?.Any() == true)
                {
                    foreach (var devId in taskVM.SelectedDevIds)
                    {
                        _context.TaskDevelopers.Add(new TaskDeveloper
                        {
                            TaskId = id,
                            DeveloperId = devId
                        });
                    }
                }
            }

            ModelState.Remove("Task.Project");
            ModelState.Remove("SelectDevs");
            ModelState.Remove("Task.TaskDevelopers");

            // If there are no selected developers, remove this from ModelState to prevent validation issues
            if (taskVM.SelectedDevIds?.Any() == false)
            {
                ModelState.Remove("Task.SelectedDevIds");
            }

            // Check model validity
            if (ModelState.IsValid)
            {
                await _context.SaveChangesAsync();
                return RedirectToAction("Tasks", new { id = originalTask.ProjectId });
            }
            else
            {
                // Handle the error or return to the view with the current model state
                return View(taskVM);
            }
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
            return RedirectToAction("Tasks", new { id = taskToMark.ProjectId });
        }

        // GET: Projects/AllocateDevelopers/5
        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> AllocateDevelopers(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.ProjectDevelopers)
                .ThenInclude(pd => pd.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            var allDevelopers = await _userManager.GetUsersInRoleAsync("Developer");
            var projectDeveloperIds = project.ProjectDevelopers.Select(pd => pd.UserId).ToList();

            var vm = new AllocateDevelopersViewModel
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.Title,
                Developers = allDevelopers.Select(d => new DeveloperAllocation
                {
                    DeveloperId = d.Id,
                    DeveloperName = d.FullName,
                    IsSelected = projectDeveloperIds.Contains(d.Id)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProjectManager, Administrator")]
        public async Task<IActionResult> AllocateDevelopers(AllocateDevelopersViewModel vm)
        {
            if (vm == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.ProjectDevelopers)
                .FirstOrDefaultAsync(p => p.ProjectId == vm.ProjectId);

            if (project == null)
            {
                return NotFound();
            }

            var developersToRemove = project.ProjectDevelopers
                .Where(pd => !vm.Developers.Any(d => d.IsSelected && d.DeveloperId == pd.UserId))
                .ToList();

            foreach (var developer in developersToRemove)
            {
                _context.ProjectDevelopers.Remove(developer);
            }

            var newDeveloperIds = vm.Developers
                .Where(d => d.IsSelected)
                .Select(d => d.DeveloperId)
                .Where(id => !project.ProjectDevelopers.Any(pd => pd.UserId == id))
                .ToList();

            foreach (var developerId in newDeveloperIds)
            {
                project.ProjectDevelopers.Add(new ProjectDeveloper { UserId = developerId, ProjectId = vm.ProjectId });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = vm.ProjectId });
        }




        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }
    }
}
