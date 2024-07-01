using MessagePack;
using Node.Core.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    [MessagePackObject]
    public class Master
    {
        [IgnoreMember]
        public static Guid Key { get; set; } = new Guid("b664601e-11aa-4562-b024-c558c53ea346");
        [Key(0)]
        public BigInteger ID { get; set; }
        [Key(1)]
        public string IP { get; set; }
    }
}
