using NodeHub.Services;
using NodeHub.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Node.Web.Services.Interfaces
{
    public interface IStatesService
    {
        IStatesService InjectionNode(INodeService node);
        ValueTask<bool> SendToMaster();
        ValueTask<bool> SendToChildren();
        ValueTask<bool> TakeInner(INodeService requester, IEnumerable<NodeState> nodes);
        ValueTask<bool> TakeOuter(INodeService requester, IEnumerable<NodeState> nodes);
        void Update();
    }
}
