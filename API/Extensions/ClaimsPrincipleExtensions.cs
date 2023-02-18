using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Extensions
{
    public static class ClaimsPrincipleExtensions
    {
        public static string GetUserName(this ClaimsPrincipal user){
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static int GetUserId(this ClaimsPrincipal user){
            return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        public static string GetUserRole(this ClaimsPrincipal user){
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }

        // public static List<string> GetUserRoles(this ClaimsPrincipal user){
        //     return user.FindAll(ClaimTypes.Role).Select(i => i.Value).ToList();
        // }
    }
}