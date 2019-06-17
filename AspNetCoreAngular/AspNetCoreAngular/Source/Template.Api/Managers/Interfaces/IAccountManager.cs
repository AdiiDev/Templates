using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Database.AuthDomain;

namespace Template.Api.Managers.Interfaces
{
    public interface IAccountManager
    {
        Task<bool> CheckPasswordAsync(AppUser user, string password);

        Task<Tuple<bool, string[]>> CreateRoleAsync(AppUserRole role, IEnumerable<string> claims);

        Task<Tuple<bool, string[]>> CreateUserAsync(AppUser user, IEnumerable<string> roles, string password);

        Task<Tuple<bool, string[]>> DeleteRoleAsync(AppUserRole role);

        Task<Tuple<bool, string[]>> DeleteRoleAsync(string roleName);

        Task<Tuple<bool, string[]>> DeleteUserAsync(AppUser user);

        Task<Tuple<bool, string[]>> DeleteUserAsync(string userId);

        Task<AppUserRole> GetRoleByIdAsync(string roleId);

        Task<AppUserRole> GetRoleByNameAsync(string roleName);

        Task<AppUserRole> GetRoleLoadRelatedAsync(string roleName);

        Task<List<AppUserRole>> GetRolesLoadRelatedAsync(int page, int pageSize);

        Task<Tuple<AppUser, string[]>> GetUserAndRolesAsync(string userId);

        Task<AppUser> GetUserByEmailAsync(string email);

        Task<AppUser> GetUserByIdAsync(string userId);

        Task<AppUser> GetUserByUserNameAsync(string userName);

        Task<IList<string>> GetUserRolesAsync(AppUser user);

        Task<List<Tuple<AppUser, string[]>>> GetUsersAndRolesAsync(int page, int pageSize);

        Task<Tuple<bool, string[]>> ResetPasswordAsync(AppUser user, string newPassword);

        Task<bool> TestCanDeleteRoleAsync(string roleId);

        Task<bool> TestCanDeleteUserAsync(string userId);

        Task<Tuple<bool, string[]>> UpdatePasswordAsync(AppUser user, string currentPassword, string newPassword);

        Task<Tuple<bool, string[]>> UpdateRoleAsync(AppUserRole role, IEnumerable<string> claims);

        Task<Tuple<bool, string[]>> UpdateUserAsync(AppUser user);

        Task<Tuple<bool, string[]>> UpdateUserAsync(AppUser user, IEnumerable<string> roles);
    }
}
