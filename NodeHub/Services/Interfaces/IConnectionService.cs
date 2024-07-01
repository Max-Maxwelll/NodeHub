using NodeHub.Services;
using NodeHub.Services.Interfaces;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Node.Web.Services.Interfaces
{
    public interface IConnectionService
    {
        IConnectionService InjectionNode(INodeService node);
        ValueTask<bool> Connect(INodeService newNode);
        ValueTask<IEnumerable<NodeState>> GetNodes(BigInteger excludeID);
    }
}
