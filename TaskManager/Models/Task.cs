using System.ComponentModel.DataAnnotations;
using TaskManager.Models;
using TaskManager.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManager.Models
{
    public class Task
    {
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Range(1, 999)]
        [Display(Name = "Required Hours")]
        [Required]
        public int RequiredHours { get; set; }

        [Required]
        public Priority Priority { get; set; }

        [Display(Name = "Completed?")]
        public bool IsCompleted { get; set; }

        // Navigation properties
        public int ProjectId { get; set; }
        [BindNever]
        public virtual Project Project { get; set; }

        // New relationship for many-to-many with developers
        [BindNever]
        public virtual ICollection<TaskDeveloper> TaskDevelopers { get; set; }
    }


}