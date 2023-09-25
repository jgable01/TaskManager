namespace TaskManager.ViewModels
{
    public class AllocateDevelopersViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; }
        public List<DeveloperAllocation> Developers { get; set; }
    }

    public class DeveloperAllocation
    {
        public string DeveloperId { get; set; }
        public string DeveloperName { get; set; }
        public bool IsSelected { get; set; }
    }
}
