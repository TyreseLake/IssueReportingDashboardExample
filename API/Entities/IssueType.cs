using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class IssueType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<UserIssueType> UserIssueTypes { get; set; }
        public ICollection<IssueReport> IssueReport { get; set; }
    }
}