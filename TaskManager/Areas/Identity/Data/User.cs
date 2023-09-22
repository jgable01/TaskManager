using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using TaskManager.Models;

namespace TaskManager.Areas.Identity.Data
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public virtual ICollection<ProjectDeveloper> ProjectDevelopers { get; set; } = new List<ProjectDeveloper>();
        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Models.Task> AssignedTasks { get; set; } = new List<Models.Task>();
    }
}
