using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class UserDistrict
    {
        public District District { get; set; }
        public int DistrictId { get; set; }
        public AppUser User { get; set; }
        public int UserId { get; set; }
    }
}