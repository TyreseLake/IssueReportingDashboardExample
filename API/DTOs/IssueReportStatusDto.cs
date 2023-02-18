using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueReportStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string PreviousStatus { get; set; }
        public string ReviewType { get; set; }
        public DateTime? DateReported { get; set; }
        public DateTime? DateUpdated { get; set; }
        public HashSet<string> IssueTypes { get; set; }
        public HashSet<string> Districts { get; set; }
        // public HashSet<string> IssueTypes { get; set; }
        public string IssueType { get; set; } = "";
        public string District { get; set; } = "";
        public int IssueReportCount { get; set; } = 0;
        public string LocationDescription { get; set; } = "";
        public string Description { get; set; } = "";
        public Boolean Pinned { get; set; } = false;
        public Boolean Hidden { get; set; } = false;
        public DateTime? DateLastReported { get; set; }
        public int StatusUpdateCount { get; set; } = 0; 
        public bool ClosedStatus { get; set; } = false;
        // public bool Moved { get; set; }
        // public DateTime DateMoved { get; set; }
        // public int? OrignalIssueSourceId { get; set; }
        //public Dictionary<string, int> PlatformReportCount { get; set; }
    }
}