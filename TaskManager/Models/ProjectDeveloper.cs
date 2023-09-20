using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
