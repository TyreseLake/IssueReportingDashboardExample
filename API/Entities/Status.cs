using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class Status
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public bool Final { get; set; } = false;
        public bool Default { get; set; } = false;
    }
}