using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class MoveIssueReportDto
    {
        [Required] public int SourceId { get; set; }
        [Required] public int DestinationId { get; set; }
        [Required] public int[] IssueReportIds { get; set; }
    }
}