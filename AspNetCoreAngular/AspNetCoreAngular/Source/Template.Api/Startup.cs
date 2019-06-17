using Common.UOW;
using Database.AuthDomain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Template.Api.Filters;
using KissLog;
using Microsoft.EntityFrameworkCore;
using DatabaseApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Template.Api.Authorization;
using Template.Api.Managers.Interfaces;
using Template.Api.Managers.Managers;

namespace Template.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connection = @"Server=.;Database=TestingTemplate;Trusted_Connection=True;MultipleActiveResultSets=true;";
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(connection);
                options.UseOpenIddict();
            });


            //Add Unit of work DI
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //DI for KissLog
            services.AddScoped<KissLog.ILogger>((context) =>
            {
                return Logger.Factory.Get();
            });

            // Add identity
            services.AddIdentity<AppUser, AppUserRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();

            // Account Manager
            services.AddScoped<IAccountManager, AccountManager>();

            // Auth Handlers
            services.AddSingleton<IAuthorizationHandler, ViewUserAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ManageUserAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ViewRoleAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, AssignRolesAuthorizationHandler>();



            //Add Unit of work filter
            services.AddMvc(options => {
                options.Filters.Add<UnitOfWorkActionFilter>();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
