using NodeHub.Services;
using NodeHub.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Node.Web.Services.Interfaces
{
    public interface IOverlayService
    {
        IOverlayService InjectionNode(INodeService node);
        ValueTask<bool> AddNewNodes(IEnumerable<NodeState> nodes);
        ValueTask<bool> RebuildStructure();
    }
    public enum Region
    {
        Inner,
        Outer
    }
}
