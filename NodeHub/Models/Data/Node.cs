using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeHub.Models.Data
{
    public class Node
    {
        public string ID { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<Group> Groups { get; set; }
    }
}
