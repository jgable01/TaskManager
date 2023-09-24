using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManager.Areas.Identity.Data;

namespace TaskManager.Models.ViewModels
{
    public class TaskVM
    {
        public Project? Project { get; set; }
        public int SelectedDevId { get; set; }
        public Task? Task { get; set; }

        public Priority Priority { get; set; }

        public List<SelectListItem> SelectItems = new List<SelectListItem>();

        public TaskVM(ICollection<User> devUsers)
        {
            foreach (User d in devUsers)
            {
                SelectItems.Add(new SelectListItem { Text = d.FullName, Value = d.Id.ToString() });

            }

        }

        public TaskVM()
        {


        }


    }

}

