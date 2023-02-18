using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class IssueStatus
    {
        public int Id { get; set; }
        public int CurrentStatusId { get; set; }
        public Status CurrentStatus { get; set; }
        //public string Status { get; set; }
        // public string ReviewType { get; set; }
        // public DateTime ReviewDate { get; set; }
        // public ICollection<Remark> Remarks { get; set; }
        public ICollection<StatusUpdate> StatusUpdates { get; set; }
        public ICollection<IssueReport> IssueReports { get; set; }
        public string Description { get; set; }
        public string LocationDescription { get; set; }
        public int? DistrictId { get; set; }
        public District District { get; set; }
        public ICollection<IssueReport> OriginalIssueReports { get; set; }
    }
}