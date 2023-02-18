using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class StatusUpdate
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        public int? PreviousStatusId { get; set; }
        public Status PreviousStatus { get; set; }
        // public string Remark { get; set; }
        // public DateTime DateRemarked { get; set; }
        public int IssueStatusId { get; set; }
        public IssueStatus IssueStatus { get; set; }
        public string ResponsibleUnit { get; set; }
        public string NewUnit { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateReported { get; set; }
        public ICollection<ApprovalItem>  ApprovalItems { get; set; }
        public ICollection<StatusUpdateImage> Images { get; set; }
        public string ReasonDetails { get; set; }
        public string StatusUpdateDetails { get; set; }
        public string WorkType  { get; set; }
        public bool Edited { get; set; } = false;
        public DateTime? DateEdited { get; set; }
    }
}