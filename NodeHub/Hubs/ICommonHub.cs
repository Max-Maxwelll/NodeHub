using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeHub.Hubs
{
    public interface ICommonHub
    {
        Task NewNode(string id);
        Task NodeStop(string id);
        Task NodeStart(string id);
        Task Rebuild(string id);
        Task AddBlock(string id);
    }
}
