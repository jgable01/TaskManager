namespace TaskManager.ViewModels
{
    public class AssignRoleViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();
        public string SelectedRoleName { get; set; }

    }

    public class RoleSelection
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}
