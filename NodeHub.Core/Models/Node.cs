using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Node.Core.Models
{
    [MessagePackObject]
    public struct Node
    {
        [Key(0)]
        public BigInteger Id { get; set; }
    }
}
