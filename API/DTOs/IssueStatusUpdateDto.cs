using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.DTOs
{
    public class IssueStatusUpdateDto
    {
        public int Id { get; set; }
        public int StatusId { get; set; }
        public string Uploader { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public string PreviousStatus { get; set; }
        // public string Remark { get; set; }
        // public DateTime DateRemarked { get; set; }
        public string ResponsibleUnit { get; set; }
        public string NewUnit { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateReported { get; set; }
        public DateTime DateEdited { get; set; }
        public IEnumerable<string>  ApprovalItems { get; set; }
        public string ReasonDetails { get; set; }
        public string StatusUpdateDetails { get; set; }
        public string WorkType  { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public bool IsCurrentStatus { get; set; } = false;
        public class Image
        {
            public int Id { get; set; }
            public string Path { get; set; }
        }
    }
}