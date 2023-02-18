using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers
{
    public class UserFilterParams
    {
        public string[] IssueTypeAccess { get; set; }
        public string[] DistrictAccess { get; set; }
        public string[] StatusList { get; set; }
        public string SortBy { get; set; } = "Date";
        public string Order {get; set; } = "Descending";
        public Boolean ShowClosed { get; set; } = true;
        public Boolean PinnedOnTop { get; set; } = true;
        public Boolean ShowHidden { get; set; } = false;

    }
}