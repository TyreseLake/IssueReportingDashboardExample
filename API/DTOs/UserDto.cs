using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Helpers;

namespace API.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool RequirePasswordReset { get; set; }
        public string[] UserIssueTypes { get; set; } = {};
        public string[] UserDistricts { get; set; } = {};
        public bool AdminPrivileges { get; set; } = false;
        public bool IssueManagementPrivileges { get; set; } = false;
        public bool UserManagementPrivileges { get; set; } = false;
        public bool DataManagementPrivileges { get; set; } = false;
        public bool IssueStatusUpdaterPrivileges { get; set; } = false;
        public UserFilterParams Params { get; set; }
    }
}