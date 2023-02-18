using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class IssueReport
    {
        public int Id { get; set; }
        public int? MobileIssueId { get; set; }
        public string MobileUserId { get; set; }
        public int? AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public string Subject { get; set; }
        public int? IssueTypeId { get; set; }
        public IssueType IssueType { get; set; }
        public string Description { get; set; }
        public string LocationDescription { get; set; }
        public float LocationLongitude { get; set; }
        public float LocationLatitude { get; set; }
        public ICollection<Image> Images { get; set; }
        public int? DistrictId { get; set; }
        public District District { get; set; }
        public DateTime DateReported { get; set; }
        public string Platform { get; set; }
        public int? IssueStatusId { get; set; }
        public IssueStatus IssueStatus { get; set; }
        public string ReporterEmail { get; set; }
        public string ReporterAddress { get; set; }
        public string ReporterPhoneNumber { get; set; }
        public bool Edited { get; set; } = false;
        public DateTime? DateEdited { get; set; }
        public bool Moved { get; set; } = false;
        public DateTime? DateMoved { get; set; }
        public int? OrignalIssueSourceId { get; set; }
        public IssueStatus OrignalIssueSource { get; set; }
    }
}