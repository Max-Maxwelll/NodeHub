using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Node.Web.Services.Interfaces;
using NodeHub.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Services.Interfaces
{
    public interface INodeService : IHostedService
    {
        BigInteger ID { get; set; }
        bool Online { get; set; }
        string IP { get; set; }
        IStatesService States { get; }
        IOverlayService Overlay { get; }
        IConnectionService Connection { get; }
        IStorageService Storage { get; }
        IReplicationService Replication { get; }
        IHubContext<CommonHub, ICommonHub> CommonHub { get; }
    }
}
