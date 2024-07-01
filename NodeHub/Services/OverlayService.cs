using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodeHub.Core;
using Node.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NodeHub.Core.Models;
using NodeHub.Services.Interfaces;
using NodeHub.Services;

namespace Node.Web.Services
{
    public class OverlayService : IOverlayService, IHostedService
    {
        private INodeService node;
        private readonly INetworkingService networking;
        private readonly ILogger logger;

        private Timer timerDeactivate;
        private const long TMCHECKEXPIREDNODES = 1000;

        public OverlayService(INetworkingService networking, ILogger logger)
        {
            this.networking = networking;
            this.logger = logger;
        }
        public IOverlayService InjectionNode(INodeService node)
        {
            if (this.node == null) this.node = node;
            logger.LogInformation($"[{node.ID}] OVERLAY SERVICE STARTED!");
            return this;
        }
        private async void Deactivate()
        {
            try
            {
                logger.LogDebug($"[{node.ID}] EXECUTING 'DELETEEXPIREDNODES' METHOD");
                var nodes = node.Storage.Nodes.ToList().Select(x => x.Value).Where(x => x.IsActive);
                var treeOwnNode = this.node.Storage.ActiveTree[this.node.ID];
                var levelInTree = treeOwnNode.Level;
                var count = nodes.Count();
                if (count > 0)
                {
                    var expired = new List<BigInteger>();

                    foreach (var state in nodes)
                    {
                        var period = (DateTime.Now - state.Updated);
                        if(period.TotalMilliseconds > 500 * (levelInTree + 1))
                        {
                            var n = state;
                            n.IsActive = false;
                            this.node.Storage.Nodes[state.Node.ID] = n;
                            expired.Add(state.Node.ID);
                        }
                    }

                    if (expired.Count > 0)
                    {
                        logger.LogDebug($"[{this.node.ID}] We have {expired.Count} expired nodes!");
                        await RebuildStructure();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public async ValueTask<bool> AddNewNodes(IEnumerable<NodeState> nodes)
        {
            try
            {
                logger.LogDebug($"[{node.ID}] EXECUTING 'ADDNEWNODES' METHOD");

                var connected = new List<BigInteger>();
                var disconnected = new List<BigInteger>();

                foreach (var n in nodes)
                {
                    if(this.node.Storage.Nodes.TryGetValue(n.Node.ID, out var state))
                    {
                        if(this.node.Storage.NumbersStates.TryGetValue(n.Node.ID, out var numbers))
                        {
                            lock (numbers)
                            {
                                if (!numbers.Contains(n.Number))
                                {
                                    if (state.IsActive == true && n.IsActive == false)
                                        disconnected.Add(n.Node.ID);
                                    else if (state.IsActive == false && n.IsActive == true)
                                        connected.Add(n.Node.ID);

                                    state = n;
                                    state.Updated = DateTime.Now;
                                    node.Storage.Nodes[n.Node.ID] = state;

                                    if (numbers.Count > 9) numbers.Clear();
                                    numbers.Add(state.Number);
                                }
                            }
                        }
                        else
                        {
                            if (state.IsActive == true && n.IsActive == false)
                                disconnected.Add(n.Node.ID);
                            else if (state.IsActive == false && n.IsActive == true)
                                connected.Add(n.Node.ID);

                            state = n;
                            state.Updated = DateTime.Now;
                            node.Storage.Nodes[n.Node.ID] = state;

                            numbers = new List<BigInteger>() { n.Number };
                            this.node.Storage.NumbersStates.Add(n.Node.ID, numbers);
                        }
                    }
                    else
                    {
                        if (n.IsActive)
                            connected.Add(n.Node.ID);
                        else if (!n.IsActive)
                            disconnected.Add(n.Node.ID);

                        state = n;
                        state.Updated = DateTime.Now;
                        node.Storage.Nodes[n.Node.ID] = state;

                        var numbers = new List<BigInteger>() { n.Number };
                        this.node.Storage.NumbersStates.Add(n.Node.ID, numbers);
                    }
                }

                if (disconnected.Count > 0 || connected.Count > 0)
                {
                    await RebuildStructure();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                return false;
            }
        }

        public async ValueTask<bool> RebuildStructure()
        {
            try
            {
                logger.LogDebug($"[{node.ID}] EXECUTING 'REBUILDSTRUCTURE' METHOD");
                logger.LogDebug($"[{node.ID}] Inpute nodes count : {node.Storage.Nodes.Where(x => x.Value.IsActive)?.Count()}");

                var aciveNodes = node.Storage.Nodes.Where(x => x.Value.IsActive).Select(x => x.Value.Node.ID).ToArray();
                var fullNodes = node.Storage.Nodes.Select(x => x.Value.Node.ID).ToArray();
                var activeTree = networking.GetTree(aciveNodes);
                var fullTree = networking.GetTree(fullNodes);
                var masterId = activeTree[node.ID].Master;

                node.Storage.Master = masterId != BigInteger.Zero ? node.Storage.Nodes[masterId].Node : new NodeService();

                await NotifyChildren(activeTree, fullTree);

                node.Storage.ActiveTree = activeTree;
                node.Storage.FullTree = fullTree;
                await node.CommonHub.Clients.All.Rebuild(node.ID.ToString());
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message} (RebuildStructure)");
                return false;
            }
        }

        private async ValueTask<bool> NotifyChildren(Tree activeTree, Tree fullTree)
        {
            logger.LogDebug($"[{node.ID}] EXECUTING 'NOTIFYCHILDREN' METHOD");

            var masterId = activeTree[node.ID].Master;
            var children = activeTree[node.ID].Children;

            if (children?.Count > 0)
            {
                logger.LogDebug($"[{node.ID}] Children : {children.Aggregate(string.Empty, (f, s) => f += $"{s} ")}. (NotifyChildren)");


                var childrenNodes = node.Storage.GetNodes(children);

                //var ownState = node.Storage.Nodes[node.ID];
                //var includeList = new List<NodeState>();
                //includeList.Add(ownState);

                //if (masterId != BigInteger.Zero)
                //{
                //    var master = node.Storage.Nodes[masterId];
                //    includeList.Add(master);
                //}

                if (childrenNodes != null)
                {
                    foreach (var child in childrenNodes)
                    {
                        //child.Node.Connection.IncludeNodes(includeList.AsEnumerable());
                        var activeOuterRegion = networking.GetOuterRegion(activeTree, child.Node.ID);
                        var fullOuterRegion = networking.GetOuterRegion(fullTree, child.Node.ID);
                        var fullListOuterNodes = fullOuterRegion.Region.Select(x => this.node.Storage.Nodes[x]);
                        var activeListOuterNodes = activeOuterRegion.Region.Select(x => this.node.Storage.Nodes[x]);
                        await child.Node.States.TakeOuter(node, activeListOuterNodes);
                    }
                }
                else throw new Exception("Images of children don't exist!");
            }
            else logger.LogDebug($"[{node.ID}] We don't have children!");

            return true;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var states = this.node.Storage.Nodes.Values.Where(x => x.Node.ID != this.node.ID).ToArray();

            this.node.Storage.Nodes?.Clear();
            this.node.Storage.FullTree.TreeNodes?.Clear();
            this.node.Storage.ActiveTree.TreeNodes?.Clear();
            if(states.Length > 1) this.node.States.Update();         

            foreach (var n in states)
            {
                if (n.Node.Online)
                {
                    await this.node.Connection.Connect(n.Node);
                    break;
                }
            }

            timerDeactivate = new System.Threading.Timer((obj) => Deactivate(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(TMCHECKEXPIREDNODES));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timerDeactivate?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
