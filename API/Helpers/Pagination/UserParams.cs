using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers.Pagination
{
    public class UserParams : PaginationParams
    {
        public string UserRole { get; set; }
        public string UserName { get; set; }
        public int[] UserDistrictAccessIds { get; set; }
        public int[] UserIssueTypeAccessIds { get; set; }
    }
}