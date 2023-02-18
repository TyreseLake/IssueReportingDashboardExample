using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Database Context to set up database
/// </summary>
namespace API.Data
{
    public class DataContext : IdentityDbContext<AppUser, AppRole, int, 
        IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>, 
        IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        //Models to Create using DbSet
        public DbSet<IssueType> IssueTypes { get; set; }
        public DbSet<UserIssueType> UserIssueTypes { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<UserDistrict> UserDistricts { get; set; }
        public DbSet<IssueReport> IssueReports { get; set; }
        public DbSet<IssueStatus> IssueStatus { get; set; }
        public DbSet<UserActivity> UserActivity { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<StatusUpdate> StatusUpdates { get; set; }
        public DbSet<ApprovalItem> ApprovalItems { get; set; }
        public DbSet<UserPin> UserPins { get; set; }
        public DbSet<UserHiddenItem> UserHiddenItems { get; set; }

        //Configure relationships and calculated feilds
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Configure the many to many relationship between Users and Roles
            builder.Entity<AppUser>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
            builder.Entity<AppRole>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            //Configure the many to many relationship between Users and Issue Types
            builder.Entity<AppUser>()
                .HasMany(ui => ui.UserIssueTypes)
                .WithOne(u => u.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            builder.Entity<IssueType>()
                .HasMany(ui => ui.UserIssueTypes)
                .WithOne(i => i.IssueType)
                .HasForeignKey(ui => ui.IssueTypeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            //Set the concatanated key for the user issue types
            builder.Entity<UserIssueType>()
                .HasKey(k => new {k.UserId, k.IssueTypeId});

            //Set the concatanated key for the user pins
            builder.Entity<UserPin>()
                .HasKey(k => new {k.AppUserId, k.IssueStatusId});
            
            //Set the concatanated key for the user hidden items
            builder.Entity<UserHiddenItem>()
                .HasKey(k => new {k.AppUserId, k.IssueStatusId});

            //Configure the many to many relationship between Users and Districts
            builder.Entity<AppUser>()
                .HasMany(ud => ud.UserDistricts)
                .WithOne(u => u.User)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            builder.Entity<District>()
                .HasMany(ud => ud.UserDistricts)
                .WithOne(d => d.District)
                .HasForeignKey(ud => ud.DistrictId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            //Set the concatanated key for the user districts
            builder.Entity<UserDistrict>()
                .HasKey(k => new {k.UserId, k.DistrictId});

            // builder.Entity<StatusUpdate>()
            //     .HasOne(su => su.Status)
            //     .WithMany(s => s.StatusUpdates)
            //     .HasForeignKey(su => su.StatusId)
            //     .OnDelete(DeleteBehavior.NoAction)
            //     .IsRequired();

            // builder.Entity<StatusUpdate>()
            //     .HasOne(su => su.PreviousStatus)
            //     .WithMany(s => s.PreviousStatusUpdates)
            //     .HasForeignKey(su => su.PreviousStatusId)
            //     .OnDelete(DeleteBehavior.NoAction)
            //     .IsRequired();

            builder.Entity<StatusUpdate>()
                .HasOne(su => su.Status);

            builder.Entity<StatusUpdate>()
                .HasOne(su => su.PreviousStatus);

            builder.Entity<IssueStatus>()
                .HasMany(i => i.OriginalIssueReports)
                .WithOne(ir => ir.OrignalIssueSource)
                .HasForeignKey(ir => ir.OrignalIssueSourceId)
                .OnDelete(DeleteBehavior.SetNull);

            // builder.Entity<ApprovalItem>()
            //     .HasOne(ai => ai.StatusUpdate)
            //     .WithMany(su => su.ApprovalItems)
            //     .HasForeignKey(ai => ai.StatusUpdateId)
            //     .OnDelete(DeleteBehavior.SetNull);
        }
    }
}