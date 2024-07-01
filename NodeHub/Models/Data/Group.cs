using NodeHub.Core.Models;
using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Models.Data
{
    public class Group
    {
        public string ID { get; set; }
        public IEnumerable<Block> Blocks { get; set; }
    }
}
