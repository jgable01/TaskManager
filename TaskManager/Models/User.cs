using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using TaskManager.Models;

public class User : IdentityUser
{
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; }

    // Navigation properties
    public virtual ICollection<Project> ManagedProjects { get; set; }
    public virtual ICollection<Task> AssignedTasks { get; set; }
    public virtual ICollection<ProjectDeveloper> ProjectDevelopers { get; set; }

}
