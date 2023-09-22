using System.ComponentModel.DataAnnotations;
using TaskManager.Models;
using TaskManager.Areas.Identity.Data;


namespace TaskManager.Models
{
    public class Task
    {
        public int TaskId { get; set; }

        [Required] 
        [StringLength(200)]
        public string Title { get; set; }

        [Range(1, 999)]
        public int RequiredHours { get; set; }

        public Priority Priority { get; set; }
        public bool IsCompleted { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public string DeveloperId { get; set; }
        public virtual User Developer { get; set; }
    }

}