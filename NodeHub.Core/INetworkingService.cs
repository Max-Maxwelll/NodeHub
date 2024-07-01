using NodeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Core
{
    public interface INetworkingService
    {
        Tree GetTree(IEnumerable<BigInteger> nodes);
        Dictionary<BigInteger, Dictionary<BigInteger, Distance>> GetDistances(IEnumerable<BigInteger> nodes);
        IEnumerable<DistanceBlock> GetDistances(BigInteger block, IEnumerable<BigInteger> nodes);
        InnerOuterRegions GetRegions(Tree tree, BigInteger ownId);
        InnerRegion GetInnerRegion(Tree tree, BigInteger baselineID);
        OuterRegion GetOuterRegion(Tree tree, BigInteger baselineID);
        BigInteger GetNodesHash(IEnumerable<Node.Core.Models.Node> nodes);
    }
}
