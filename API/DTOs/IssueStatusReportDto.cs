using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueStatusReportDto
    {
        public int Id { get; set; }
        public string IssueType { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string District { get; set; }
        public string LocationDescription { get; set; }
        public float LocationLongitude { get; set; }
        public float LocationLatitude { get; set; }
        public DateTime? DateReported { get; set; }
        public int ImageCount { get; set; }
        public string Platform { get; set; }
        public int? MobileUserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int IssueStatusId { get; set; }
        public string CurrentStatus { get; set; }
        public bool ClosedStatus { get; set; }
    }
}