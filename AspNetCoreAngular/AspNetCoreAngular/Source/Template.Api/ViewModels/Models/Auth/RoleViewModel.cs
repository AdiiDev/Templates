﻿namespace MProject.Api.ViewModels.Models.Auth
{
    public class RoleViewModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int UsersCount { get; set; }

        public PermissionViewModel[] Permissions { get; set; }
    }
}
