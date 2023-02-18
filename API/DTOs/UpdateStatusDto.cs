using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UpdateStatusDto
    {
        [Required] public int StatusId { get; set; }
        [Required] public string Status { get; set; }
        public string ReviewType { get; set; }
    }
}