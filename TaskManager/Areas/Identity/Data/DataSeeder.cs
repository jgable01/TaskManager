using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Areas.Identity.Data;
using TaskManager.Models;
using Task = TaskManager.Models.Task;

namespace TaskManager.Areas.Identity.Data
{
    public static class DataSeeder
    {
        public static async System.Threading.Tasks.Task SeedInitialData(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TaskManagerContext>();

                //context.Database.EnsureDeleted(); // Enable these to generate seed data
                //context.Database.Migrate();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!roleManager.RoleExistsAsync("Administrator").Result)
                {
                    await roleManager.CreateAsync(new IdentityRole("Administrator"));
                }
                if (!roleManager.RoleExistsAsync("ProjectManager").Result)
                {
                    await roleManager.CreateAsync(new IdentityRole("ProjectManager"));
                }
                if (!roleManager.RoleExistsAsync("Developer").Result)
                {
                    await roleManager.CreateAsync(new IdentityRole("Developer"));
                }
                
                Console.WriteLine("Checking if users exist...");

                // Check if any users exist
                if (!context.Users.Any())
                {
                    // Seed Users
                    var admin = new User { UserName = "admin@example.com", FullName = "Admin User", Email = "admin@example.com" };
                    await userManager.CreateAsync(admin, "Admin@123");
                    await userManager.AddToRoleAsync(admin, "Administrator");

                    var projectManager1 = new User { UserName = "pm1@example.com", FullName = "Project Manager 1", Email = "pm1@example.com" };
                    await userManager.CreateAsync(projectManager1, "Pm1@123");
                    await userManager.AddToRoleAsync(projectManager1, "ProjectManager");

                    var projectManager2 = new User { UserName = "pm2@example.com", FullName = "Project Manager 2", Email = "pm2@example.com" };
                    await userManager.CreateAsync(projectManager2, "Pm2@123");
                    await userManager.AddToRoleAsync(projectManager2, "ProjectManager");

                    var developer1 = new User { UserName = "dev1@example.com", FullName = "Developer 1", Email = "dev1@example.com" };
                    await userManager.CreateAsync(developer1, "Dev1@123");
                    await userManager.AddToRoleAsync(developer1, "Developer");

                    var developer2 = new User { UserName = "dev2@example.com", FullName = "Developer 2", Email = "dev2@example.com" };
                    await userManager.CreateAsync(developer2, "Dev2@123");
                    await userManager.AddToRoleAsync(developer2, "Developer");

                    var developer3 = new User { UserName = "dev3@example.com", FullName = "Developer 3", Email = "dev3@example.com" };
                    await userManager.CreateAsync(developer3, "Dev3@123");
                    await userManager.AddToRoleAsync(developer3, "Developer");

                    // Seed Projects
                    var project1 = new Project { Title = "Company Website", Manager = projectManager1 };
                    context.Projects.Add(project1);

                    var project2 = new Project { Title = "Mobile App", Manager = projectManager2 };
                    context.Projects.Add(project2);

                    // Seed Tasks for Project 1
                    context.Tasks.Add(new Task { Title = "Fix Main Page Header", Priority = Priority.High, RequiredHours = 10, Project = project1, Developer = developer1 });
                    context.Tasks.Add(new Task { Title = "Correct About Us Display", Priority = Priority.Medium, RequiredHours = 5, Project = project1, Developer = developer2 });
                    context.Tasks.Add(new Task { Title = "Update Contact Form", Priority = Priority.Low, RequiredHours = 3, Project = project1, Developer = developer3 });

                    // Seed Tasks for Project 2
                    context.Tasks.Add(new Task { Title = "Implement Login Feature", Priority = Priority.High, RequiredHours = 20, Project = project2, Developer = developer1 });
                    context.Tasks.Add(new Task { Title = "Design Home Screen", Priority = Priority.Medium, RequiredHours = 8, Project = project2, Developer = developer2 });
                    context.Tasks.Add(new Task { Title = "Fix App Crashes", Priority = Priority.Low, RequiredHours = 15, Project = project2, Developer = developer3 });

                    // Seed ProjectDeveloper assignments
                    context.ProjectDevelopers.Add(new ProjectDeveloper { Project = project1, User = developer1 });
                    context.ProjectDevelopers.Add(new ProjectDeveloper { Project = project1, User = developer2 });
                    context.ProjectDevelopers.Add(new ProjectDeveloper { Project = project1, User = developer3 });

                    context.ProjectDevelopers.Add(new ProjectDeveloper { Project = project2, User = developer1 });
                    context.ProjectDevelopers.Add(new ProjectDeveloper { Project = project2, User = developer2 });

                    var addedEntities = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
                    Console.WriteLine($"Total added entities: {addedEntities.Count}");


                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

