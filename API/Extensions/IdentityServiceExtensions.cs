using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, 
            IConfiguration config)
        {
            services.AddIdentityCore<AppUser>(opt => 
            {
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireLowercase = false;
                opt.User.RequireUniqueEmail = false;
            })
                .AddRoles<AppRole>()
                .AddRoleManager<RoleManager<AppRole>>()
                .AddSignInManager<SignInManager<AppUser>>()
                .AddRoleValidator<RoleValidator<AppRole>>()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<DataContext>();
            
            //Token Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => 
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                            .GetBytes(config["TokenKey"])),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                    };
                });

            services.AddAuthorization(opt => 
            {
                // opt.AddPolicy("RequireAdminPrivileges", policy => policy.RequireRole("Admin", "System Admin"));
                // opt.AddPolicy("RequireIssueManagementPrivileges", policy => policy.RequireRole("Admin", "Issue Manager"));
                // opt.AddPolicy("RequireDataManagementPrivileges", policy => policy.RequireRole("Admin", "System Admin", "Data Manager"));
                // opt.AddPolicy("RequireUserManagementPrivileges", policy => policy.RequireRole("Admin", "System Admin", "Issue Manager"));
                // opt.AddPolicy("RequireStatusUpdatePrivileges", policy => policy.RequireRole("Admin", "Issue Manager", "Issue Status Updater"));

                opt.AddPolicy("RequireAdminPrivileges", policy => policy.RequireRole(RolesHelper.AdminRoles.ToArray()));
                opt.AddPolicy("RequireIssueManagementPrivileges", policy => policy.RequireRole(RolesHelper.IssueManagementRoles.ToArray()));
                opt.AddPolicy("RequireDataManagementPrivileges", policy => policy.RequireRole(RolesHelper.DataManagementRoles.ToArray()));
                opt.AddPolicy("RequireUserManagementPrivileges", policy => policy.RequireRole(RolesHelper.UserManagementRoles.ToArray()));
                opt.AddPolicy("RequireStatusUpdatePrivileges", policy => policy.RequireRole(RolesHelper.IssueStatusUpdaterRoles.ToArray()));
            });
            
            return services;
        }
    }
}