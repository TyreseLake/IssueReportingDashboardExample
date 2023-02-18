using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class AddStatusRemarkDto
    {
        [Required] public int StatusId { get; set; }
        [Required] public string RemarkType { get; set; }
        [Required] public string RemarkData { get; set; }
    }
}