using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace API.DTOs
{
    public class IssueReportDto
    {
        public string MobileUserId { get; set; }
        public string Subject { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string LocationDescription { get; set; }
        public float? LocationLongitude { get; set; }
        public float? LocationLatitude { get; set; }
        public string District { get; set; } = "";
        public string Platform { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public int? StatusId { get; set; }
        
        // public IFormFileCollection Files { get; set; }
        // public IFormFile Image_1 { get; set; }
        // public IFormFile Image_2 { get; set; }
        // public IFormFile Image_3 { get; set; }
        // public IFormFile Image_4 { get; set; }
        // public IFormFile Image_5 { get; set; }
    }
}