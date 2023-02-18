using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UserInfoDto
    {
        [Required] public string UserName { get; set; }
        [Required] [StringLength(255, MinimumLength = 6)] public string Password { get; set; }
        [Required] public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        // [EmailAddress] public string Email { get; set; }
        public string AccountType { get; set; }
        public string[] IssueReportAccessPermissions { get; set; } = {};
        public string[] DistrictAccessPermissions { get; set; } = {};
    }
}