using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
             IConfiguration config)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIssueTypeRepository, IssueTypeRepository>();
            services.AddScoped<IDistrictRepository, DistrictRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IIssueReportingRepository, IssueReportingRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<IFileService, FileService>();
            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient
            );

            services.AddDbContext<DataContext>(options => 
            {
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // services.AddScoped<DataContext>(x => {
            //     DbContextOptionsBuilder<DataContext> dbBuilder =
            //         new DbContextOptionsBuilder<DataContext>();
            //     dbBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            //     dbBuilder.EnableSensitiveDataLogging();
            //     // return new DataContext(dbBuilder.Options);
            //     var myContext =  new DataContext(dbBuilder.Options);
            //     myContext.Database.SetCommandTimeout(int.MaxValue);
            //     return myContext;
            // });

            return services;
        }
    }
}