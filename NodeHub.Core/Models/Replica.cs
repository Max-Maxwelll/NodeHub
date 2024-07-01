using System;
using System.Collections.Generic;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct Replica
    {
        public ReplicaType Type { get; set; }
        public bool Ready { get; set; }
    }
    public enum ReplicaType
    {
        Primary,
        Secondary
    }
}
