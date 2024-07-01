using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Node.Web.Services.Interfaces;
using NodeHub.Core;
using NodeHub.Core.Models;
using NodeHub.Hubs;
using NodeHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace NodeHub.Services
{
    public class NodeService : INodeService
    {
        public BigInteger ID
        {
            get { return id; }
            set { if (id == BigInteger.Zero) id = value; }
        }
        private BigInteger id = BigInteger.Zero;
        public bool Online { get; set; }
        public string IP { get; set; }
        public IStatesService States { get; }
        public IOverlayService Overlay { get; }
        public IConnectionService Connection { get; }
        public IStorageService Storage { get; }
        public IReplicationService Replication { get; }
        public IHubContext<CommonHub, ICommonHub> CommonHub => commonHub;
        private readonly IHubContext<CommonHub, ICommonHub> commonHub;
        private readonly INetworkingService networking;
        private readonly ILogger logger;
        
        public NodeService() { }
        public NodeService(INetworkingService networking, IStatesService states, IOverlayService overlay, IConnectionService connection, IStorageService storage, IReplicationService replication, IHubContext<CommonHub, ICommonHub> commonHub, ILogger logger)
        {
            this.Storage = storage;
            this.networking = networking;
            this.States = states.InjectionNode(this);
            this.Overlay = overlay.InjectionNode(this);
            this.Connection = connection.InjectionNode(this);
            this.Replication = replication.InjectionNode(this);
            this.logger = logger;
            this.commonHub = commonHub;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"NODE {ID} STARTED");
            var timer = new Timer((obj) => CommonHub.Clients.All.NewNode(ID.ToString()), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Online = true;
            ((IHostedService)this.Overlay).StartAsync(cancellationToken);
            ((IHostedService)this.States).StartAsync(cancellationToken);
            ((IHostedService)this.Replication).StartAsync(cancellationToken);
            CommonHub.Clients.All.NodeStart(ID.ToString());
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Online = false;
            ((IHostedService)this.States).StopAsync(cancellationToken);
            ((IHostedService)this.Overlay).StopAsync(cancellationToken);
            ((IHostedService)this.Replication).StartAsync(cancellationToken);
            CommonHub.Clients.All.NodeStop(ID.ToString());
            return Task.CompletedTask;
        }
    }
}
