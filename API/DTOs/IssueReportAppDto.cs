using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueReportAppDto
    {
        public int? IssueId { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string LocationDescription { get; set; }
        public float? LocationLatitude { get; set; }
        public float? LocationLongitude { get; set; }
        public string Images { get; set; }
        public string UserId { get; set; }
        public string District { get; set; }
        public string Status { get; set; }
        public string ReviewType { get; set; }
        public string Remarks { get; set; }
        public DateTime? Date { get; set; }
        public string ClassName { get; set; }
        public string DocumentCheckedOutVersionHistoryID { get; set; }

    }
}