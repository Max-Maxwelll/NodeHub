using System.Numerics;

namespace NodeHub.Core.Models
{
    public struct DistanceBlock
    {
        public BigInteger Node { get; set; }
        public BigInteger Block { get; set; }
        public BigInteger Dis { get; set; }
    }
}
