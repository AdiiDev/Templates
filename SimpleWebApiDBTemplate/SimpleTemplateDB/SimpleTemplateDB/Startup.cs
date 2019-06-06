using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.UOW;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleTemplateDB.Filters;

namespace SimpleTemplateDB
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
            //var connection = @"data source=.\SQLEXPRESS;initial catalog=MangaProject;integrated security=True;MultipleActiveResultSets=True;";
            services.AddDbContext<DataContext>
                (options =>
                {
                    options.UseSqlServer(connection);
                    //options.UseOpenIddict();
                });

            //Add Unit of work DI
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IPersonService, PersonService>();

            //Add Unit of work filter
            services.AddMvc(options => {
                options.Filters.Add<UnitOfWorkActionFilter>();
                }
            ).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
