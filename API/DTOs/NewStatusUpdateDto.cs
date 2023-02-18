using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class NewStatusUpdateDto
    {
        [Required] public int IssueStatusId { get; set; }
        public string NewStatus { get; set; }
        public string ResponsibleUnit { get; set; }
        public string NewUnit { get; set; }
        public DateTime Date { get; set; }
        public string ApprovalItems { get; set; }
        public IEnumerable<string> Approvals { get; set; }
        public string ReasonDetails { get; set; }
        public string StatusUpdateDetails { get; set; }
        public string WorkType { get; set; }
    }
}