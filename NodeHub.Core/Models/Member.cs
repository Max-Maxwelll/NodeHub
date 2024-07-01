using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct Member
    {
        public List<BigInteger> Groups { get; set; }
        public Member(BigInteger group)
        {
            Groups = new List<BigInteger> { group };
        }
        public Member(IEnumerable<BigInteger> groups)
        {
            Groups = groups.ToList();
        }
    }
}
