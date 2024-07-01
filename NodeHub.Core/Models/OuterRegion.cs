using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NodeHub.Core.Models
{
    public struct OuterRegion : IRegion
    {
        [JsonIgnore]
        public static Guid Key { get; set; } = new Guid("ab84a5f3-b515-4528-887e-58e00eaf04bf");
        [JsonIgnore]
        public BigInteger this[int index] => ((BigInteger[])Region)[index];
        [JsonIgnore]
        public int Length => ((BigInteger[])Region).Length;
        public IEnumerable<BigInteger> Region { get; set; }
        public OuterRegion(IEnumerable<BigInteger> region) => (this.Region) = (region);

        public BigInteger GetHash()
        {
            var hash = Region.Aggregate(BigInteger.Zero, (h, s) => h ^= s);
            return hash;
        }
    }
}
