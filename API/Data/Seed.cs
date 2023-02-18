using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedData(UserManager<AppUser> userManager, 
                                             RoleManager<AppRole> roleManager,
                                             IConfiguration config,
                                             DataContext context)
        {
            //Add roles if roles do not exist
            if (!(await roleManager.Roles.AnyAsync()))
            {
                // var roles = new List<AppRole>
                // {
                //     new AppRole{Name = "Default"},
                //     new AppRole{Name = "Admin"},
                //     new AppRole{Name = "Data Manager"},
                //     new AppRole{Name = "Issue Manager"}
                // };

                // var roles = new List<AppRole>
                // {
                //     new AppRole{Name = "System Admin"}, //IT System Admin
                //     new AppRole{Name = "Admin"}, //MIRS Admin
                //     new AppRole{Name = "Issue Manager"}, //Issue Manager
                //     new AppRole{Name = "Data Manager"}, //Data Manager
                //     new AppRole{Name = "Issue Status Updater"}, //Issue Status Updater
                //     new AppRole{Name = "Default"}, //Issue Status Updater
                // };

                var roles = RolesHelper.Roles.Select(i => new AppRole{Name = i}).ToList();

                foreach (var role in roles)
                {
                    await roleManager.CreateAsync(role);
                }
            }

            //Add admin if admin account does not exist
            if (!(await userManager.Users.Where(x => x.UserName == "admin").AnyAsync()))
            {
                var admin = new AppUser
                {
                    UserName = "admin",
                    FirstName = "Administrator",
                    Email = "admin@mail.com",
                    RequirePasswordReset = false
                };

                await userManager.CreateAsync(admin, "Admin123!");

                // await userManager.AddToRolesAsync(admin, new string[] {"Admin"});
                await userManager.AddToRolesAsync(admin, new string[] {RolesHelper.SystemAdminRole});
            }
            
            //Add the Issue Types to the database if they dont exist
            if (!(await context.IssueTypes.AnyAsync()))
            {
                foreach (var issueType in config.GetSection("IssueTypes").Get<string[]>())
                {
                    var newIssueType = new IssueType 
                    {
                        Name = issueType
                    };
                    var result = await context.IssueTypes.AddAsync(newIssueType);
                }

                await context.SaveChangesAsync();
            }

            //Add the Districts to the database if they dont exist
            if (!(await context.Districts.AnyAsync()))
            {
                foreach (var district in config.GetSection("Districts").Get<string[]>())
                {
                    var newDistrict = new District 
                    {
                        Name = district
                    };
                    var result = await context.Districts.AddAsync(newDistrict);
                }

                await context.SaveChangesAsync();
            }

            if(!(await context.Status.AnyAsync()))
            {
                var statuses = config.GetSection("Statuses").Get<string[]>();
                var finalStatuses = config.GetSection("FinalStatuses").Get<string[]>();
                var defaultStatus = config.GetSection("DefaultStatus").Get<string>();
                foreach (var status in config.GetSection("Statuses").Get<string[]>())
                {
                    var newStatus = new Status
                    {
                        Name = status,
                        Final = finalStatuses.Contains(status),
                        Default = defaultStatus == status
                    };
                    var result = await context.Status.AddAsync(newStatus);
                }
 
                await context.SaveChangesAsync();
            }

            return;
        }
    }
}