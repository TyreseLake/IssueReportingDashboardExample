using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueReportDetailsDto
    {
        public int? Id { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string IssueLocation { get; set; }
        public string District { get; set; }
        public float? LocationLongitude { get; set; }
        public float? LocationLatitude { get; set; }
        //public string IssueStatus { get; set; }
        // public string ReviewType { get; set; }
        // public IEnumerable<RemarkDto> IssueRemarks { get; set; }
        public DateTime? DateReported { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public int? ReportCount { get; set; }
        public IEnumerable<ItemCount> PlatformCounts { get; set; } 
        
        // public class RemarkDto
        // {
        //     public string UserName { get; set; }
        //     public string RemarkType { get; set; }
        //     public string Remark { get; set; }
        //     public DateTime DateRemarked { get; set; }
        // }

        public class ItemCount
        {
            public string Name { get; set; }
            public int? Count { get; set; }
        }

        public class Image
        {
            public int Id { get; set; }
            public string Path { get; set; }
        }
        
        public string CurrentStatus { get; set; }
    }
}