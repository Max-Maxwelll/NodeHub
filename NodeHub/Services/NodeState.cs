using NodeHub.Services.Interfaces;
using System;
using System.Numerics;

namespace NodeHub.Services
{
    public struct NodeState
    {
        public INodeService Node { get; set; }
        public bool IsActive { get; set; }
        public string IP { get; set; }
        public BigInteger Number { get; set; }
        public DateTime Updated { get; set; }
    }
}
