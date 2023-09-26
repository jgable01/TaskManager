using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManager.Areas.Identity.Data;

namespace TaskManager.Models
{
    public class Project
    {
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(200, ErrorMessage = "Project titles must be between 5-200 characters in length.")]
        [MinLength(5, ErrorMessage = "Project titles must be between 5-200 characters in length.")]
        public string Title { get; set; }

        [Display(Name = "Manager")]
        public string ManagerId { get; set; }
        public virtual User? Manager { get; set; }

        // Navigation properties 
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
        public virtual ICollection<ProjectDeveloper> ProjectDevelopers { get; set; } = new List<ProjectDeveloper>();
    }

}