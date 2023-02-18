using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueReportUpdateDto
    {  
        [Required]
        public int IssueReportId { get; set; }
        public string NewAddress { get; set; }
        public string NewDescription { get; set; }
        public string NewEmail { get; set; }
        public string NewLocationDescription { get; set; }
        public string NewPhoneNumber { get; set; }
        public string NewSubject { get; set; }
        public string NewPlatform { get; set; }
        public string NewIssueType { get; set; }
        public string NewDistrict { get; set; }
    }
}