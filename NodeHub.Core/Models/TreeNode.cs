using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Core.Models
{
    public struct TreeNode
    {
        public BigInteger Id { get; set; }
        public BigInteger Master { get; set; }
        public List<BigInteger> Children { get; set; }
        public int Level { get; set; }
    }
}
