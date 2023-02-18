using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers.Pagination
{
    public class IssueReportParams : PaginationParams
    {
        public int IssueStatusId;
        public string SortBy { get; set; } = "MostRecent";
    }
}