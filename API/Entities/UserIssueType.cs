using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class UserIssueType
    {
        public IssueType IssueType { get; set; }
        public int IssueTypeId { get; set; }
        public AppUser User { get; set; }
        public int UserId { get; set; }
    }
}