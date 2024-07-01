using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct Block
    {
        public Dictionary<BigInteger, Replica> Replicas { get; set; }
        public List<BigInteger> Files { get; set; }
        public DateTime LastUpdate { get; set; }

        public Block(BigInteger file)
        {
            this.Replicas = new Dictionary<BigInteger, Replica>();
            this.Files = new List<BigInteger> { file };
            this.LastUpdate = DateTime.Now;
        }
        public Block(IEnumerable<BigInteger> files)
        {
            this.Replicas = new Dictionary<BigInteger, Replica>();
            this.Files = new List<BigInteger>(files);
            this.LastUpdate = DateTime.Now;
        }
    }
}
