using NodeHub.Core.Models;
using NodeHub.Services;
using NodeHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Node.Web.Services.Interfaces
{
    public interface IStorageService
    {
        INodeService Master { get; set; }
        Tree ActiveTree { get; set; }
        Tree FullTree { get; set; }
        InnerRegion InnerRegion { get; set; }
        OuterRegion OuterRegion { get; set; }
        Dictionary<BigInteger, NodeState> Nodes { get; }
        Dictionary<BigInteger, List<BigInteger>> NumbersStates { get; }
        Dictionary<BigInteger, Group> Groups { get; }
        Dictionary<BigInteger, Member> Members { get; }
        Dictionary<BigInteger, byte[]> Blocks { get; }
        IEnumerable<NodeState> GetNodes(IEnumerable<BigInteger> keys);
    }
}
