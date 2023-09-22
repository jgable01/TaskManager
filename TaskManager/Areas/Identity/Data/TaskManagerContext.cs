using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Areas.Identity.Data;
using TaskManager.Models;
using Task = TaskManager.Models.Task;

public class TaskManagerContext : IdentityDbContext<User>
{
    public TaskManagerContext(DbContextOptions<TaskManagerContext> options) : base(options)
    {
    }
     
    public DbSet<Project> Projects { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<ProjectDeveloper> ProjectDevelopers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Relationship between Project and ProjectDeveloper
        builder.Entity<ProjectDeveloper>()
            .HasOne(pd => pd.Project)
            .WithMany(p => p.ProjectDevelopers)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Restrict); // Change here

        // Relationship between Project and its Manager (User)
        builder.Entity<Project>()
            .HasOne(p => p.Manager)
            .WithMany(u => u.ManagedProjects)
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship between Task and its Developer (User)
        builder.Entity<Task>()
            .HasOne(t => t.Developer)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.DeveloperId)
            .OnDelete(DeleteBehavior.Restrict);
    }


}
