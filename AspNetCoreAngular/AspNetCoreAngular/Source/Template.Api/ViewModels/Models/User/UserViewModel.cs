namespace MProject.Api.ViewModels.Models.User
{
    // TODO: We need to change properties
    public class UserViewModel
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string JobTitle { get; set; }

        public string PhoneNumber { get; set; }

        public string Configuration { get; set; }

        public bool IsEnabled { get; set; }

        public bool IsLockedOut { get; set; }

        public string[] Roles { get; set; }
    }
}
