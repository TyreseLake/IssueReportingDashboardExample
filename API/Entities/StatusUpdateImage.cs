using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class StatusUpdateImage
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public StatusUpdate StatusUpdate { get; set; }
        public int StatusUpdateId { get; set; }
    }
}