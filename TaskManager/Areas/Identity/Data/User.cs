﻿using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using TaskManager.Models;

namespace TaskManager.Areas.Identity.Data
{
    public class User : IdentityUser
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public virtual ICollection<ProjectDeveloper> ProjectDevelopers { get; set; }
        public virtual ICollection<Project> ManagedProjects { get; set; }
        public virtual ICollection<Models.Task> AssignedTasks { get; set; }
    }
}
