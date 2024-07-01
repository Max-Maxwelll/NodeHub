using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Core
{
    public interface IRegion
    {
        BigInteger this[int index] { get; }
        IEnumerable<BigInteger> Region { get; set; }
        int Length { get; }
    }
}
