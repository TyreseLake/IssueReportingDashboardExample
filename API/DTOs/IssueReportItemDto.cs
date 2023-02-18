using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class IssueReportItemDto
    {
        public int Id {get; set; }
        public string UserName { get; set; }
        public string Uploader { get; set; }
        public string Subject { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string LocationDescription { get; set; }
        public float? LocationLongitude { get; set; }
        public float? LocationLatitude { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public string District { get; set; }
        public string Platform { get; set; }
        public DateTime? DateReported { get; set; }
        public string PhoneNumber {get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public bool Moved { get; set; }
        public DateTime? DateMoved { get; set; }
        public int? OrignalIssueSourceId { get; set; }

        public class Image
        {
            public int Id { get; set; }
            public string Path { get; set; }
        }
    }
}