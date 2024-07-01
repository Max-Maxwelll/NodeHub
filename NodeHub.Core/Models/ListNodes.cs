using System;
using System.Collections.Generic;
using System.Numerics;

namespace NodeHub.Core.Models
{
    public class ListNodes
    {
        public static Guid Key = new Guid("ee091551-238d-458e-9865-e22250341f0a"); 
        public Dictionary<BigInteger, bool> Keys { get; set; }
    }
}
