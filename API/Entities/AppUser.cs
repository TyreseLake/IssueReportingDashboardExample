using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser<int>
    {   
        /*
        public int Id { get; set; }
        public string UserName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        */
        //Connection between a User and their roles
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Boolean RequirePasswordReset { get; set; } = true;
        public ICollection<AppUserRole> UserRoles { get; set; }
        public ICollection<UserIssueType> UserIssueTypes { get; set; }
        public ICollection<UserDistrict> UserDistricts { get; set; }
        public ICollection<IssueReport> IssueReports { get; set; }
        public ICollection<UserHiddenItem> UserHiddenItems { get; set; }
        public ICollection<UserPin> UserPins { get; set; }
        public string UserParams { get; set; }
        // public ICollection<Remark> Remarks { get; set; }
    }
}