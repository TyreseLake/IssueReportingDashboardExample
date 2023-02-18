using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers.Pagination
{
    public class IssueStatusSearchParams
    {
        public string IssueType { get; set; }
        public string District { get; set; }
        public string Description { get; set; }
        public string Subject { get; set; }
        public string LocationDescription { get; set; }
        public string Completed { get; set; } = "show";
        public List<int> IssueTypeIds { get; set; }
        public List<int> DistrictIds { get; set; }
    }
}