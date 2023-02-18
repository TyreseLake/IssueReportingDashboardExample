using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UpdateStatusLocationDto
    {
        [Required] public int StatusId { get; set; }
        public string LocationDescription { get; set; }
        public string District { get; set; }
    }
}