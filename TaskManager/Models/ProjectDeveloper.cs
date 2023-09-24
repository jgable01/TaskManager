using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManager.Areas.Identity.Data;

namespace TaskManager.Models
{
    public class ProjectDeveloper
    {
        [Key]
        public int ProjectDeveloperId { get; set; }

        [ForeignKey("ProjectId")]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [ForeignKey("UserId")]
        public string UserId { get; set; }
        public User? User { get; set; }
    }
}
