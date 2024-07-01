using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NodeHub.Core.Models
{
    public struct InnerRegion : IRegion
    {
        [JsonIgnore]
        public BigInteger this[int index] => ((BigInteger[])Region)[index];
        //[IgnoreMember]
        //BigInteger IRegion.this[int index] => ((BigInteger[])Region)[index];
        [JsonIgnore]
        public static Guid Key { get; set; } = new Guid("4941d4c2-7589-42e1-b8d8-85c4b25038fb");

        public IEnumerable<BigInteger> Region { get; set; }
        [JsonIgnore]
        public int Length => Region.Count();

        public InnerRegion(IEnumerable<BigInteger> region) => (this.Region) = (region);

        public BigInteger GetHash()
        {
            var hash = Region.Aggregate(BigInteger.Zero, (h, s) => h ^= s);
            return hash;
        }
    }
}
