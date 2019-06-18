using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Database.AuthDomain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using OpenIddict.Validation;

using MProject.Api.ViewModels.Models.Auth;
using MProject.Api.ViewModels.Models.User;
using MProject.Api.ViewModels.RequestViewModels.User;
using Template.Api.Authorization;
using Template.Api.Claims;
using Template.Api.Helpers;
using Template.Api.Managers;
using Template.Api.Managers.Interfaces;

namespace PAI.Api.Controllers
{
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly IAccountManager _accountManager;
        private readonly IAuthorizationService _authorizationService;
        private const string GetUserByIdActionName = "GetUserById";
        private const string GetRoleByIdActionName = "GetRoleById";

        public AccountController(IAccountManager accountManager, IAuthorizationService authorizationService)
        {
            _accountManager = accountManager;
            _authorizationService = authorizationService;
        }


        [HttpGet("users/me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            return await GetUserByUserName(this.User.Identity.Name);
        }


        [HttpGet("users/{id}", Name = GetUserByIdActionName)]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Read)).Succeeded)
                return new ChallengeResult();


            UserViewModel userVM = await GetUserViewModelHelper(id);

            if (userVM != null)
                return Ok(userVM);
            else
            {
                var userId = Utilities.GetUserId(this.User);
                return NotFound(id);
            }
        }

        [HttpGet("users/username/{userName}")]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            AppUser appUser = await _accountManager.GetUserByUserNameAsync(userName);

            if (!(await _authorizationService.AuthorizeAsync(this.User, appUser?.Id ?? "", AccountManagementOperations.Read)).Succeeded)
                return new ChallengeResult();

            if (appUser == null)
                return NotFound(userName);

            return await GetUserById(appUser.Id);
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            return await GetUsers(-1, -1);
        }


        [HttpGet("users/{pageNumber:int}/{pageSize:int}")]
        public async Task<IActionResult> GetUsers(int pageNumber, int pageSize)
        {
            var usersAndRoles = await _accountManager.GetUsersAndRolesAsync(pageNumber, pageSize);
            var userId = this.User.FindFirstValue(CustomClaimTypes.Email);
            List<UserViewModel> usersVM = new List<UserViewModel>();

            foreach (var item in usersAndRoles)
            {
                UserViewModel userVM = new UserViewModel();
                item.Item1.Roles = null;

                userVM = Mapper.Map<UserViewModel>(item.Item1);
                userVM.Roles = item.Item2;

                usersVM.Add(userVM);
            }

            return Ok(usersVM);
        }


        [HttpPut("users/me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UserEditViewModel user)
        {
            var model = await UpdateUser(Utilities.GetUserId(this.User), user);
            return model;
        }


        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserEditViewModel user)
        {
            AppUser appUser = await _accountManager.GetUserByIdAsync(id);
            string[] currentRoles = appUser != null ? (await _accountManager.GetUserRolesAsync(appUser)).ToArray() : null;

            var manageUsersPolicy = _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Update);
            var assignRolePolicy = _authorizationService.AuthorizeAsync(this.User, Tuple.Create(user.Roles, currentRoles), Template.Api.Authorization.Policies.AssignAllowedRolesPolicy);


            if ((await Task.WhenAll(manageUsersPolicy, assignRolePolicy)).Any(r => !r.Succeeded))
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");

                if (!string.IsNullOrWhiteSpace(user.Id) && id != user.Id)
                    return BadRequest("Conflicting user id in parameter and model data");

                if (appUser == null)
                    return NotFound(id);


                if (Utilities.GetUserId(this.User) == id && string.IsNullOrWhiteSpace(user.CurrentPassword))
                {
                    if (!string.IsNullOrWhiteSpace(user.NewPassword))
                        return BadRequest("Current password is required when changing your own password");

                    if (appUser.UserName != user.UserName)
                        return BadRequest("Current password is required when changing your own username");
                }


                bool isValid = true;

                if (Utilities.GetUserId(this.User) == id && (appUser.UserName != user.UserName || !string.IsNullOrWhiteSpace(user.NewPassword)))
                {
                    if (!await _accountManager.CheckPasswordAsync(appUser, user.CurrentPassword))
                    {
                        isValid = false;
                        AddErrors(new string[] { "The username/password couple is invalid." });
                    }
                }

                if (isValid)
                {
                    //AppUser appUser = new AppUser();
                    appUser = Mapper.Map<UserViewModel, AppUser>(user, appUser);

                    var result = await _accountManager.UpdateUserAsync(appUser, user.Roles);
                    if (result.Item1)
                    {
                        if (!string.IsNullOrWhiteSpace(user.NewPassword))
                        {
                            if (!string.IsNullOrWhiteSpace(user.CurrentPassword))
                                result = await _accountManager.UpdatePasswordAsync(appUser, user.CurrentPassword, user.NewPassword);
                            else
                                result = await _accountManager.ResetPasswordAsync(appUser, user.NewPassword);
                        }

                        if (result.Item1)
                            return NoContent();
                    }

                    AddErrors(result.Item2);
                }
            }

            return BadRequest(ModelState);
        }


        [HttpPatch("users/me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] JsonPatchDocument<UserPatchViewModel> patch)
        {
            return await UpdateUser(Utilities.GetUserId(this.User), patch);
        }


        [HttpPatch("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] JsonPatchDocument<UserPatchViewModel> patch)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Update)).Succeeded)
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (patch == null)
                    return BadRequest($"{nameof(patch)} cannot be null");


                AppUser appUser = await _accountManager.GetUserByIdAsync(id);

                if (appUser == null)
                    return NotFound(id);


                UserPatchViewModel userPVM = new UserPatchViewModel();
                userPVM = Mapper.Map<UserPatchViewModel>(appUser);
                patch.ApplyTo(userPVM, ModelState);


                if (ModelState.IsValid)
                {
                    Mapper.Map<UserPatchViewModel, AppUser>(userPVM, appUser);

                    var result = await _accountManager.UpdateUserAsync(appUser);
                    if (result.Item1)
                        return NoContent();


                    AddErrors(result.Item2);
                }
            }

            return BadRequest(ModelState);
        }


        [HttpPost("users")]
        [Authorize(Template.Api.Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(201, Type = typeof(UserViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Register([FromBody] UserEditViewModel user)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, Tuple.Create(user.Roles, new string[] { }), Template.Api.Authorization.Policies.AssignAllowedRolesPolicy)).Succeeded)
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");


                AppUser appUser = new AppUser();
                appUser = Mapper.Map<AppUser>(user);
                appUser.EmailConfirmed = true;
                //appUser. = true;
                var result = await _accountManager.CreateUserAsync(appUser, user.Roles, user.NewPassword);
                if (result.Item1)
                {
                    UserViewModel userVM = await GetUserViewModelHelper(appUser.Id);
                    return CreatedAtAction(GetUserByIdActionName, new { id = userVM.Id }, userVM);
                }

                AddErrors(result.Item2);
            }

            return BadRequest(ModelState);
        }

        [HttpGet("users/test")]
        public IActionResult test()
        {
            return Ok();
        }

        [HttpPost("users/register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegistrationRequestModel user)
        {
            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");
               // if (user.Password != user.Password2)
                 //   return BadRequest("Hasla sa inne");

                AppUser appUser = Mapper.Map<AppUser>(user);
                //appUser = (AppUser)CopyItems.CopyProperties(user, appUser);

                var result = await _accountManager.CreateUserAsync(appUser, new[] { "user" }, user.Password);
                if (result.Item1)
                {
                    return Ok(new { Message = "Udało się zarejestrować", Status = 1 });
                }

                AddErrors(result.Item2);
            }

            return BadRequest(ModelState);
        }


        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Delete)).Succeeded)
                return new ChallengeResult();

            if (!await _accountManager.TestCanDeleteUserAsync(id))
                return BadRequest("User cannot be deleted. DeleteAsync all orders associated with this user and try again");


            UserViewModel userVM = null;
            AppUser appUser = await this._accountManager.GetUserByIdAsync(id);

            if (appUser != null)
                userVM = await GetUserViewModelHelper(appUser.Id);


            if (userVM == null)
                return NotFound(id);

            var result = await this._accountManager.DeleteUserAsync(appUser);
            if (!result.Item1)
                throw new Exception("The following errors occurred whilst deleting user: " + string.Join(", ", result.Item2));


            return Ok(userVM);
        }


        [HttpPut("users/unblock/{id}")]
        public async Task<IActionResult> UnblockUser(string id)
        {
            AppUser appUser = await this._accountManager.GetUserByIdAsync(id);

            if (appUser == null)
                return NotFound(id);

            appUser.LockoutEnd = null;
            var result = await _accountManager.UpdateUserAsync(appUser);
            if (!result.Item1)
                throw new Exception("The following errors occurred whilst unblocking user: " + string.Join(", ", result.Item2));


            return NoContent();
        }


        [HttpGet("users/me/preferences")]
        public async Task<IActionResult> UserPreferences()
        {
            var userId = Utilities.GetUserId(this.User);
            AppUser appUser = await this._accountManager.GetUserByIdAsync(userId);

            //if (appUser != null)
            //    return Ok(appUser.Configuration); //?
            //else
            //    return NotFound(userId);
            return Ok();
        }


        [HttpPut("users/me/preferences")]
        public async Task<IActionResult> UserPreferences([FromBody] string data)
        {
            var userId = Utilities.GetUserId(this.User);
            AppUser appUser = await this._accountManager.GetUserByIdAsync(userId);

            if (appUser == null)
                return NotFound(userId);

            //appUser.Configuration = data;
            var result = await _accountManager.UpdateUserAsync(appUser);
            if (!result.Item1)
                throw new Exception("The following errors occurred whilst updating User Configurations: " + string.Join(", ", result.Item2));


            return NoContent();
        }

        [HttpGet("roles/{id}", Name = GetRoleByIdActionName)]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var appRole = await _accountManager.GetRoleByIdAsync(id);

            if (!(await _authorizationService.AuthorizeAsync(this.User, appRole?.Name ?? "", Template.Api.Authorization.Policies.ViewRoleByRoleNamePolicy)).Succeeded)
                return new ChallengeResult();

            if (appRole == null)
                return NotFound(id);

            return await GetRoleByName(appRole.Name);
        }


        [HttpGet("roles/name/{name}")]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, name, Template.Api.Authorization.Policies.ViewRoleByRoleNamePolicy)).Succeeded)
                return new ChallengeResult();


            RoleViewModel roleVM = await GetRoleViewModelHelper(name);

            if (roleVM == null)
                return NotFound(name);

            return Ok(roleVM);
        }


        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            return await GetRoles(-1, -1);
        }


        [HttpGet("roles/{pageNumber:int}/{pageSize:int}")]
        public async Task<IActionResult> GetRoles(int pageNumber, int pageSize)
        {
            var roles = await _accountManager.GetRolesLoadRelatedAsync(pageNumber, pageSize);

            return Ok(Mapper.Map<List<RoleViewModel>>(roles));
        }


        [HttpPut("roles/{id}")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleViewModel role)
        {
            if (ModelState.IsValid)
            {
                if (role == null)
                    return BadRequest($"{nameof(role)} cannot be null");

                if (!string.IsNullOrWhiteSpace(role.Id) && id != role.Id)
                    return BadRequest("Conflicting role id in parameter and model data");



                AppUserRole appRole = await _accountManager.GetRoleByIdAsync(id);

                if (appRole == null)
                    return NotFound(id);


                Mapper.Map<RoleViewModel, AppUserRole>(role, appRole);

                var result = await _accountManager.UpdateRoleAsync(appRole, role.Permissions?.Select(p => p.Value).ToArray());
                if (result.Item1)
                    return NoContent();

                AddErrors(result.Item2);

            }

            return BadRequest(ModelState);
        }


        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] RoleViewModel role)
        {
            if (ModelState.IsValid)
            {
                if (role == null)
                    return BadRequest($"{nameof(role)} cannot be null");

                AppUserRole appRole = Mapper.Map<AppUserRole>(role);

                var result = await _accountManager.CreateRoleAsync(appRole, role.Permissions?.Select(p => p.Value).ToArray());
                if (result.Item1)
                {
                    RoleViewModel roleVM = await GetRoleViewModelHelper(appRole.Name);
                    return CreatedAtAction(GetRoleByIdActionName, new { id = roleVM.Id }, roleVM);
                }

                AddErrors(result.Item2);
            }

            return BadRequest(ModelState);
        }


        [HttpDelete("roles/{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            if (!await _accountManager.TestCanDeleteRoleAsync(id))
                return BadRequest("Role cannot be deleted. Remove all users from this role and try again");


            RoleViewModel roleVM = null;
            AppUserRole appRole = await this._accountManager.GetRoleByIdAsync(id);

            if (appRole != null)
                roleVM = await GetRoleViewModelHelper(appRole.Name);


            if (roleVM == null)
                return NotFound(id);

            var result = await this._accountManager.DeleteRoleAsync(appRole);
            if (!result.Item1)
                throw new Exception("The following errors occurred whilst deleting role: " + string.Join(", ", result.Item2));


            return Ok(roleVM);
        }


        [HttpGet("permissions")]
        public IActionResult GetAllPermissions()
        {
            return Ok(Mapper.Map<List<PermissionViewModel>>(ApplicationPermissions.AllPermissions));
        }

        private async Task<UserViewModel> GetUserViewModelHelper(string userId)
        {
            var userAndRoles = await _accountManager.GetUserAndRolesAsync(userId);
            if (userAndRoles == null)
                return null;

            var userVM = Mapper.Map<UserViewModel>(userAndRoles.Item1);
            userVM.Roles = userAndRoles.Item2;

            return userVM;
        }

        private async Task<RoleViewModel> GetRoleViewModelHelper(string roleName)
        {
            var role = await _accountManager.GetRoleLoadRelatedAsync(roleName);
            if (role != null)
            {
                return Mapper.Map<RoleViewModel>(role);
            }

            return null;
        }

        private void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }
    }
}