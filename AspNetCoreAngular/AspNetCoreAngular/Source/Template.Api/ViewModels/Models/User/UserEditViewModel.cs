using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MProject.Api.ViewModels.Models.User
{
    public class UserEditViewModel : UserViewModel
    {
        public string CurrentPassword { get; set; }

        public string NewPassword { get; set; }
        new private bool IsLockedOut { get; } //Hide base member
    }
}
