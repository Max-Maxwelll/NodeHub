using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Models.Monitor
{
    public class NodeModel
    {
        public BigInteger ID { get; set; }
        public string IP { get; set; }
        public bool Online { get; set; }
        public BigInteger Master { get; set; }
    }
}
