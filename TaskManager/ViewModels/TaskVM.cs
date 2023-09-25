using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TaskManager.Areas.Identity.Data;
using TaskManager.Models;
using Task = TaskManager.Models.Task;

namespace TaskManager.ViewModels.TaskVM
{
    public class TaskVM
    {

        public Task Task { get; set; }

        [Required(ErrorMessage = "Please select a priority")]
        public Priority Priority { get; set; }

        [Display(Name = "Select Developers")]
        [BindNever]
        public List<SelectListItem> SelectDevs { get; set; }
        [Display(Name = "Select Developers")]
        public List<string>? SelectedDevIds { get; set; } // New property for selected developer IDs
        public List<SelectListItem> PriorityItems { get; set; } = Enum.GetValues(typeof(Priority))
             .Cast<Priority>()
             .Select(p => new SelectListItem
             {
                 Text = p.ToString(),
                 Value = ((int)p).ToString()
             }).ToList();

        public TaskVM(ICollection<User> devUsers)
        {
            SelectDevs = devUsers.Select(d => new SelectListItem { Text = d.FullName, Value = d.Id.ToString() }).ToList();
        }

        public TaskVM() { }
    }

}