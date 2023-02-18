using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string ActionGroup { get; set; }
        public string Data { get; set; }
        public string UserName { get; set; }
        public string AccountType { get; set; }
        public string Email { get; set; }
        public string IpAddress { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Status { get; set; } = "Processing";
        public DateTime CompletionDate { get; set; }
        public string Response { get; set; }
    }
}