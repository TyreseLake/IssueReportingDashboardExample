using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers
{
    public static class RolesHelper
    {
        public const string SystemAdminName = "admin";
        public const string SystemAdminRole = "System Admin";
        public static readonly IList<string> Roles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "System Admin", "Data Manager", "Issue Manager", "Issue Status Updater", "Default"
        });
        public static readonly IList<string> RolesThatCanBeRestricted = new ReadOnlyCollection<string>(new List<string>{
            "Issue Manager", "Issue Status Updater", "Default"
        });
        public static readonly IList<string> AdminRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "System Admin"
        });
        public static readonly IList<string> DataManagementRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "System Admin", "Data Manager"
        });
        public static readonly IList<string> UserManagementRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "System Admin", "Issue Manager"
        });
        public static readonly IList<string> IssueManagementRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "Issue Manager"
        });
        public static readonly IList<string> IssueStatusUpdaterRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "Issue Manager", "Issue Status Updater"
        });
        public static readonly IList<string> AdminAccessOnlyRoles = new ReadOnlyCollection<string>(new List<string>{
            "Admin", "Data Manager", "Issue Manager"
        });

        public static bool RoleIsValid(string role){
            return Roles.Contains(role);
        }

        public static bool RoleAllowsRestriction(string role){
            return RolesThatCanBeRestricted.Contains(role);
        }

        public static bool RoleIsAdminAccessOnly(string role){
            return AdminAccessOnlyRoles.Contains(role);
        }

        public static bool RoleIsAdmin(string role){
            return AdminRoles.Contains(role);
        }
        
        public static bool RoleIsUserManagement(string role){
            return UserManagementRoles.Contains(role);
        }

        public static bool RoleIsIssueManagement(string role){
            return IssueManagementRoles.Contains(role);
        }

        public static bool RoleIsDataManagement(string role){
            return DataManagementRoles.Contains(role);
        }

        public static bool RoleIsIssueStatusUpdater(string role){
            return IssueStatusUpdaterRoles.Contains(role);
        }
    }
}