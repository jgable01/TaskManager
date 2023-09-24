using TaskManager.Areas.Identity.Data;

namespace TaskManager.Models
{
    public class TaskDeveloper
    {
        public int TaskDeveloperId { get; set; }
        public int TaskId { get; set; }
        public virtual Task Task { get; set; }

        public string DeveloperId { get; set; }
        public virtual User Developer { get; set; }
    }

}
