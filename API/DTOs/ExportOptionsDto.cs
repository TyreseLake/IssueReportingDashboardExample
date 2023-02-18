using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class ExportOptionsDto
    {
        [Required]
        public String ExportType { get; set; }
        public int? StatusId { get; set; }
        public string[] IssueTypeList { get; set; } = {};
        public string[] DistrictList { get; set; } = {};
        public string Districts { get; set; }
        public string IssueTypes { get; set; }
        public DateTime? dateUpper { get; set; }
        public DateTime? dateLower { get; set; }
        public string[] StatusList { get; set; } = {};
        public string Statuses { get; set; }
    }
}