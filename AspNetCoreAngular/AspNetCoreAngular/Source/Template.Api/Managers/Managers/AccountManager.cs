using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Database.AuthDomain;
using DatabaseApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Api.Managers;
using Template.Api.Claims;
using Template.Api.Managers.Interfaces;


namespace Template.Api.Managers.Managers
{
    /// <summary>
    /// This class help us to manage accounts
    /// </summary>
    public class AccountManager : IAccountManager
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppUserRole> _roleManager;


        public AccountManager(
            DataContext context,
            UserManager<AppUser> userManager,
            RoleManager<AppUserRole> roleManager,
            IHttpContextAccessor httpAccessor)
        {
            _context = context;
            _context.CurrentUserId = httpAccessor.HttpContext?.User.FindFirst(OpenIdConnectConstants.Claims.Subject)?.Value?.Trim();
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// This method return User based on passed user id
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns><see cref="AppUser"/></returns>
        public async Task<AppUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        /// <summary>
        /// This method return User based on user nam
        /// </summary>
        /// <param name="userName">User nam</param>
        /// <returns><see cref="AppUser"/></returns>
        public async Task<AppUser> GetUserByUserNameAsync(string userName)
        {
            //AK: Ok, but this name is which property?
            return await _userManager.FindByNameAsync(userName);
        }

        /// <summary>
        /// This method return User based on user email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns><see cref="AppUser"/></returns>
        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        /// <summary>
        /// This method return list of roles that had passed user
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>List of string that represents roles</returns>
        public async Task<IList<string>> GetUserRolesAsync(AppUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        /// <summary>
        /// This method return user and all his roles based on user id
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <returns><see cref="AppUser"/> and array of strings that represents roles</returns>
        public async Task<Tuple<AppUser, string[]>> GetUserAndRolesAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Id == userId)
                .SingleOrDefaultAsync();

            if (user == null)
                return null;

            var userRoleIds = user.Roles.Select(r => r.RoleId).ToList();

            var roles = await _context.Roles
                .Where(r => userRoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToArrayAsync();

            return Tuple.Create(user, roles);
        }

        /// <summary>
        /// This method return users and all their roles based on user id
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns>List of <see cref="AppUser"/> and array of strings that represents roles</returns>
        public async Task<List<Tuple<AppUser, string[]>>> GetUsersAndRolesAsync(int page, int pageSize)
        {
            IQueryable<AppUser> usersQuery = _context.Users
                .Include(u => u.Roles)
                .OrderBy(u => u.UserName);

            if (page != -1)
                usersQuery = usersQuery.Skip((page - 1) * pageSize);

            if (pageSize != -1)
                usersQuery = usersQuery.Take(pageSize);

            var users = await usersQuery.ToListAsync();

            var userRoleIds = users.SelectMany(u => u.Roles.Select(r => r.RoleId)).ToList();

            var roles = await _context.Roles
                .Where(r => userRoleIds.Contains(r.Id))
                .ToArrayAsync();

            return users.Select(u => Tuple.Create(u,
                roles.Where(r => u.Roles.Select(ur => ur.RoleId).Contains(r.Id)).Select(r => r.Name).ToArray()))
                .ToList();
        }

        /// <summary>
        /// This method create user and asign roles for him
        /// </summary>
        /// <param name="user">User to add</param>
        /// <param name="roles">User roles</param>
        /// <param name="password">User password</param>
        /// <returns>Return bool that represents if user was added to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> CreateUserAsync(AppUser user, IEnumerable<string> roles, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            user = await _userManager.FindByNameAsync(user.UserName);

            try
            {
                result = await this._userManager.AddToRolesAsync(user, roles.Distinct());
            }
            catch
            {
                await DeleteUserAsync(user);
                throw;
            }

            if (!result.Succeeded)
            {
                await DeleteUserAsync(user);
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
            }

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method update User
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>Return bool that represents if user was added to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> UpdateUserAsync(AppUser user)
        {
            return await UpdateUserAsync(user, null);
        }

        /// <summary>
        /// This method update user and asign roles for him
        /// </summary>
        /// <param name="user">User to add</param>
        /// <param name="roles">User roles</param>
        /// <returns>Return bool that represents if user was updated to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> UpdateUserAsync(AppUser user, IEnumerable<string> roles)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            if (roles != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var rolesToRemove = userRoles.Except(roles).ToArray();
                var rolesToAdd = roles.Except(userRoles).Distinct().ToArray();

                if (rolesToRemove.Any())
                {
                    result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!result.Succeeded)
                        return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                }

                if (rolesToAdd.Any())
                {
                    result = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!result.Succeeded)
                        return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                }
            }

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method reset password for passed used and set password to new passed password
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="newPassword">New password for user</param>
        /// <returns>Return bool that represents if user was updated to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> ResetPasswordAsync(AppUser user, string newPassword)
        {
            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method update the password for passed User
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="currentPassword">Current user password</param>
        /// <param name="newPassword">New password for user</param>
        /// <returns>Return bool that represents if user was updated to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> UpdatePasswordAsync(AppUser user, string currentPassword, string newPassword)
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method check if passed password and user password are the same
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="password">Password</param>
        /// <returns>True if passwods match each other</returns>
        public async Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                if (!_userManager.SupportsUserLockout)
                    await _userManager.AccessFailedAsync(user);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Test method. Not implemented for our use
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> TestCanDeleteUserAsync(string userId)
        {
            //AK: We don't need it
            //if (await _context.Orders.Where(o => o.CashierId == userId).AnyAsync())
            //    return false;
           // var n = await _context.Users2.Where(x => x.IsAdmin == true).ToListAsync();
            //canDelete = !await ; //Do other tests...

            return true;
        }

        /// <summary>
        /// This method delete user from db with passed id
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>Return bool that represents if user was deleted from db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
                return await DeleteUserAsync(user);

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method delete passed user from db
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>Return bool that represents if user was deleted from db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> DeleteUserAsync(AppUser user)
        {
            var result = await _userManager.DeleteAsync(user);
            return Tuple.Create(result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
        }

        /// <summary>
        /// This method return role based on role id
        /// </summary>
        /// <param name="roleId">Role id</param>
        /// <returns><see cref="AppUserRole"/></returns>
        public async Task<AppUserRole> GetRoleByIdAsync(string roleId)
        {
            return await _roleManager.FindByIdAsync(roleId);
        }

        /// <summary>
        /// This method return role based on role name
        /// </summary>
        /// <param name="roleName">Role name</param>
        /// <returns><see cref="AppUserRole"/></returns>
        public async Task<AppUserRole> GetRoleByNameAsync(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task<AppUserRole> GetRoleLoadRelatedAsync(string roleName)
        {
            var role = await _context.Roles
                .Include(r => r.Claims)
                .Include(r => r.Users)
                .Where(r => r.Name == roleName)
                .SingleOrDefaultAsync();

            return role;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<AppUserRole>> GetRolesLoadRelatedAsync(int page, int pageSize)
        {
            IQueryable<AppUserRole> rolesQuery = _context.Roles
                .Include(r => r.Claims)
                .Include(r => r.Users)
                .OrderBy(r => r.Name);

            if (page != -1)
                rolesQuery = rolesQuery.Skip((page - 1) * pageSize);

            if (pageSize != -1)
                rolesQuery = rolesQuery.Take(pageSize);

            var roles = await rolesQuery.ToListAsync();

            return roles;
        }

        /// <summary>
        /// This method create role and assign permissions
        /// </summary>
        /// <param name="role">Role</param>
        /// <param name="claims">Permissions</param>
        /// <returns>Return bool that represents if role was added to db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> CreateRoleAsync(AppUserRole role, IEnumerable<string> claims)
        {
            if (claims == null)
                claims = new string[] { };

            string[] invalidClaims = claims.Where(c => ApplicationPermissions.GetPermissionByValue(c) == null).ToArray();
            if (invalidClaims.Any())
                return Tuple.Create(false, new[] { "The following claim types are invalid: " + string.Join(", ", invalidClaims) });


            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            role = await _roleManager.FindByNameAsync(role.Name);

            foreach (string claim in claims.Distinct())
            {
                result = await this._roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, ApplicationPermissions.GetPermissionByValue(claim)));

                if (!result.Succeeded)
                {
                    await DeleteRoleAsync(role);
                    return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                }
            }

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method update role
        /// </summary>
        /// <param name="role">Role</param>
        /// <param name="claims">Permissions</param>
        /// <returns>Return bool that represents if role was updated and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> UpdateRoleAsync(AppUserRole role, IEnumerable<string> claims)
        {
            if (claims != null)
            {
                string[] invalidClaims = claims.Where(c => ApplicationPermissions.GetPermissionByValue(c) == null).ToArray();
                if (invalidClaims.Any())
                    return Tuple.Create(false, new[] { "The following claim types are invalid: " + string.Join(", ", invalidClaims) });
            }


            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            if (claims != null)
            {
                var roleClaims = (await _roleManager.GetClaimsAsync(role)).Where(c => c.Type == CustomClaimTypes.Permission);
                var roleClaimValues = roleClaims.Select(c => c.Value).ToArray();

                var claimsToRemove = roleClaimValues.Except(claims).ToArray();
                var claimsToAdd = claims.Except(roleClaimValues).Distinct().ToArray();

                if (claimsToRemove.Any())
                {
                    foreach (string claim in claimsToRemove)
                    {
                        result = await _roleManager.RemoveClaimAsync(role, roleClaims.Where(c => c.Value == claim).FirstOrDefault());
                        if (!result.Succeeded)
                            return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                    }
                }

                if (claimsToAdd.Any())
                {
                    foreach (string claim in claimsToAdd)
                    {
                        result = await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, ApplicationPermissions.GetPermissionByValue(claim)));
                        if (!result.Succeeded)
                            return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                    }
                }
            }

            return Tuple.Create(true, new string[] { });
        }


        /// <summary>
        /// This method checks if any user has role with passed id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<bool> TestCanDeleteRoleAsync(string roleId)
        {
            return !await _context.UserRoles.Where(r => r.RoleId == roleId).AnyAsync();
        }

        /// <summary>
        /// This method delete role from db based on role name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>Return bool that represents if role was deleted from db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role != null)
                return await DeleteRoleAsync(role);

            return Tuple.Create(true, new string[] { });
        }

        /// <summary>
        /// This method delete passed role from db
        /// </summary>
        /// <param name="role"></param>
        /// <returns>Return bool that represents if user was deleted from db and array of strings that represents all errors</returns>
        public async Task<Tuple<bool, string[]>> DeleteRoleAsync(AppUserRole role)
        {
            var result = await _roleManager.DeleteAsync(role);
            return Tuple.Create(result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
        }
    }
}
