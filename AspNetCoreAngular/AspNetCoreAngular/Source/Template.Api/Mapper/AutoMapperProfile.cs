using AutoMapper;
using Database.AuthDomain;
using Microsoft.AspNetCore.Identity;
using MProject.Api.ViewModels.Models.Auth;
using MProject.Api.ViewModels.Models.User;
using Template.Api.Managers;
using Template.Api.ViewModels.Models.Auth;

namespace Template.Api.Mapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AppUser, UserViewModel>()
                .ForMember(d => d.Roles, map => map.Ignore());
            CreateMap<UserViewModel, AppUser>()
                .ForMember(d => d.Roles, map => map.Ignore());

            CreateMap<AppUser, UserEditViewModel>()
                .ForMember(d => d.Roles, map => map.Ignore());
            CreateMap<UserEditViewModel, AppUser>()
                .ForMember(d => d.Roles, map => map.Ignore());

            CreateMap<AppUser, UserPatchViewModel>()
                .ReverseMap();

            CreateMap<AppUserRole, RoleViewModel>()
                .ForMember(d => d.Permissions, map => map.MapFrom(s => s.Claims))
                .ForMember(d => d.UsersCount, map => map.MapFrom(s => s.Users != null ? s.Users.Count : 0))
                .ReverseMap();
            CreateMap<RoleViewModel, AppUserRole>();

            CreateMap<IdentityRoleClaim<string>, ClaimViewModel>()
                .ForMember(d => d.Type, map => map.MapFrom(s => s.ClaimType))
                .ForMember(d => d.Value, map => map.MapFrom(s => s.ClaimValue))
                .ReverseMap();

            CreateMap<ApplicationPermission, PermissionViewModel>()
                .ReverseMap();

            CreateMap<IdentityRoleClaim<string>, PermissionViewModel>()
                .ConvertUsing(s => AutoMapper.Mapper.Map<PermissionViewModel>(ApplicationPermissions.GetPermissionByValue(s.ClaimValue)));
        }
    }
}
