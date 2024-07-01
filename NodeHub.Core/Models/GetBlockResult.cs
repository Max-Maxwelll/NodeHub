using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Core.Models
{
    public struct GetBlockResult
    {
        public IEnumerable<BigInteger> Chain { get; set; }
        public byte[] Block { get; set; }
        public GetBlockResult(IEnumerable<BigInteger> chain, byte[] block) => (Chain, Block) = (chain, block);
    }
}
