using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UpdateUserInfoDto
    {
        [Required] public int Id { get; set; }
        [Required] public string AccountType { get; set; }
        public string[] DistrictsAccess { get; set; }
        public string Email { get; set; }
        [Required] public string FirstName { get; set; }
        public string[] IssueTypesAccess { get; set; }
        public string LastName { get; set; }
        [Required] public string UserName { get; set; }
    }
}