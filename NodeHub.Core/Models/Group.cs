using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct Group
    {
        public BigInteger ID { get; set; }
        public IEnumerable<BigInteger> Members { get; set; }
        public Dictionary<BigInteger, Block> Blocks { get; set; }
        public BigInteger Hash { get; set; }
        public bool Restored { get; set; }
    }
}
