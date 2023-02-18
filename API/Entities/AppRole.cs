using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppRole : IdentityRole<int>
    {
        //Connection between a User and their roles
        public ICollection<AppUserRole> UserRoles { get; set; }
    }
}