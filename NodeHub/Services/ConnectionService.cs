using Microsoft.Extensions.Logging;
using Node.Web.Services.Interfaces;
using NodeHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Services
{
    public class ConnectionService : IConnectionService
    {
        private INodeService node;
        private readonly ILogger logger;
        public ConnectionService(ILogger logger)
        {
            this.logger = logger;
        }
        public IConnectionService InjectionNode(INodeService node)
        {
            if (this.node == null) this.node = node;
            logger.LogInformation($"[{node.ID}] CONNECTION SERVICE STARTED!");
            return this;
        }
        public async ValueTask<bool> Connect(INodeService newNode)
        {
            try
            {
                if (newNode.Online)
                {
                    logger.LogDebug("EXECUTING 'CONNECT' METHOD");
                    var nodes = (await newNode.Connection.GetNodes(node.ID))?.ToArray();
                    if (nodes == null || nodes.Length == 0) return false;
                    var result = await Update(nodes);
                    return result;
                }
                else return false;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"{ex.Message} (Connect)");
                throw ex;
            }
        }

        private ValueTask<bool> Update(IEnumerable<NodeState> nodes)
        {
            try
            {
                logger.LogDebug($"[{node.ID}] EXECUTING 'UPDATE' METHOD");
                logger.LogDebug($"[{node.ID}] Nodes: {nodes?.Count()} (Update)");

                foreach (var n in nodes)
                {
                    node.Storage.Nodes[n.Node.ID] = n;
                }

                var result = node.Overlay.RebuildStructure();

                return result;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public ValueTask<IEnumerable<NodeState>> GetNodes(BigInteger excludeID)
        {
            logger.LogDebug($"[{node.ID}] EXECUTING 'GETNODES' METHOD");
            var nodes = node.Storage.Nodes.Where(x => x.Key != excludeID).ToArray();
            if (nodes.Length > 0)
            {
                logger.LogInformation($"[{node.ID}] IDs : {nodes.Aggregate(string.Empty, (f, s) => f += $"{s.Key} ({s.Value.IsActive}) ")}");
                logger.LogDebug($"[{node.ID}] Nodes: {nodes.Count()}. (GetNodes)");
                return new ValueTask<IEnumerable<NodeState>>(nodes.Select(x => x.Value));
            }
            return new ValueTask<IEnumerable<NodeState>>();
        }
    }
}
