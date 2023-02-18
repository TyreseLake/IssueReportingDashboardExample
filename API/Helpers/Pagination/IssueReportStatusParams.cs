using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers.Pagination
{
    public class IssueReportStatusParams : PaginationParams
    {
        public string[] IssueTypeAccess { get; set; }
        public string[] DistrictAccess { get; set; }
        public string Districts { get; set; }
        public string IssueTypes { get; set; }
        public string Key { get; set; }
        public DateTime? dateUpper { get; set; }
        public DateTime? dateLower { get; set; }
        public int? maxReportCount { get; set; }
        public int? minReportCount { get; set; }
        public string[] StatusList { get; set; }
        public string Status { get; set; }
        public string SortBy { get; set; } = "Date";
        public string Order {get; set; } = "Descending";
        public Boolean ShowClosed { get; set; } = true;
        // public Boolean OnlyClosed { get; set; } = false;
        public Boolean ShowNew { get; set; } = true;
        public Boolean OnlyNew { get; set; } = false;
        public Boolean PinnedOnTop { get; set; } = true;
        public Boolean ShowHidden { get; set; } = false;
        public Boolean PinnedOnly { get; set; } = false;

        public object Clone() {
            return this.MemberwiseClone();
        }
    }
}