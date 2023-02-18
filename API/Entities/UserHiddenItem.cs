using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class UserHiddenItem
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public int IssueStatusId { get; set; }
        public IssueStatus IssueStatus { get; set; }
    }
}