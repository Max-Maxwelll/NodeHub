using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Models.Data
{
    public class BlockResult
    {
        public IEnumerable<string> Chain { get; set; }
        public int BlockLength { get; set; }
    }
}
