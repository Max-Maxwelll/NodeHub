using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeHub.Hubs
{
    public class CommonHub : Hub<ICommonHub>
    {
        public async Task NewNode(string id)
        {
            await Clients.All.NewNode(id);
        }
        public async Task NodeStart(string id)
        {
            await Clients.All.NodeStart(id);
        }
        public async Task NodeStop(string id)
        {
            await Clients.All.NodeStop(id);
        }
        public async Task Rebuild(string id)
        {
            await Clients.All.Rebuild(id);
        }
        public async Task AddBlock(string id)
        {
            await Clients.All.AddBlock(id);
        }
    }
}
