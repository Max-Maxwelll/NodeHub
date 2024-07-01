using MessagePack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodeHub.Core;
using NodeHub.Core.Models;
using Node.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NodeHub.Services.Interfaces;
using NodeHub.Services;

namespace Node.Web.Services
{
    public class StatesService : IStatesService, IHostedService
    {
        private INodeService node;
        private readonly INetworkingService networking;
        private readonly ILogger logger;

        private Timer sendMasterTimer;
        private Timer sendChildrenTimer;

        private const long EXPIRETIME = 60000;
        private const long TMUPDATE = 2500;

        public StatesService(INetworkingService networking, ILogger logger)
        {
            this.networking = networking;
            this.logger = logger;
        }
        public IStatesService InjectionNode(INodeService node)
        {
            if(this.node == null) this.node = node;
            logger.LogInformation($"[{node.ID}] STATES SERVICE STARTED!");
            return this;
        }
        public async ValueTask<bool> SendToMaster()
        {
            try
            {
                Update();
                logger.LogDebug($"[{node.ID}] EXECUTING 'SEND' METHOD");
                var master = GetMaster();
                if (master == null || master.ID == BigInteger.Zero) return false;

                var activeTree = node.Storage.ActiveTree;
                if (activeTree.TreeNodes == null || activeTree.TreeNodes.Count == 0) return false;

                var activeRegion = networking.GetInnerRegion(activeTree, node.ID);

                var nodes = node.Storage.Nodes;

                if (activeRegion.Length > 0)
                {
                    var activeListInnerNodes = activeRegion.Region.Select(x => node.Storage.Nodes[x]);

                    if (master.Online)
                    {
                        await master.States.TakeInner(node, activeListInnerNodes);
                    }
                    else
                    {
                        logger.LogInformation($"[{node.ID}] We have lost connection with the master!");

                        if (nodes.Count() > 0)
                        {
                            var masterState = this.node.Storage.Nodes[master.ID];
                            masterState.IsActive = false;
                            this.node.Storage.Nodes[master.ID] = masterState;
                            await node.Overlay.RebuildStructure();
                        }
                        //else throw new Exception("List of existing nodes is empty!");
                    }
                    
                }
                //else throw new Exception("Inner region is empty!");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                return false;
            }
        }
        public async ValueTask<bool> SendToChildren()
        {
            try
            {
                var activeTree = this.node.Storage.ActiveTree;
                if (activeTree.TreeNodes == null || activeTree.TreeNodes.Count == 0) return false;
                var children = activeTree.TreeNodes[this.node.ID].Children;

                if (children?.Count > 0)
                {

                    var nodes = this.node.Storage.Nodes.ToArray().ToDictionary(k => k.Key, v => v.Value);
                    var childrenNodes = children.Select(x => nodes[x]).ToArray();
                    foreach(var child in childrenNodes)
                    {
                        if (child.Node.Online)
                        {
                            var activeRegion = networking.GetOuterRegion(activeTree, child.Node.ID);
                            var activeListNodes = activeRegion.Region.Select(x => this.node.Storage.Nodes[x]);
                            await child.Node.States.TakeOuter(node, activeListNodes);
                        }
                        else
                        {
                            logger.LogInformation($"[{node.ID}] We have lost connection with the child!");
                            var nodesCount = this.node.Storage.Nodes.Count;

                            if (nodesCount > 0)
                            {
                                var childState = child;
                                childState.IsActive = false;
                                this.node.Storage.Nodes[child.Node.ID] = childState;
                                await node.Overlay.RebuildStructure();
                            }
                        }
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        public async ValueTask<bool> TakeInner(INodeService requester, IEnumerable<NodeState> nodes)
        {
            try
            {
                if (!this.node.Online) return false;
                logger.LogDebug($"[{node.ID}] EXECUTING 'TAKEINNER' METHOD");
                logger.LogInformation($"[{node.ID}] Take inner count: {nodes.Count()}");

                await node.Overlay.AddNewNodes(nodes);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                return false;
            }
        }

        public async ValueTask<bool> TakeOuter(INodeService requester, IEnumerable<NodeState> nodes)
        {
            try
            {
                if (!this.node.Online) return false;
                logger.LogDebug($"[{node.ID}] EXECUTING 'TAKEOUTER' METHOD");
                logger.LogInformation($"[{node.ID}] Take inner count: {nodes.Count()}");

                await node.Overlay.AddNewNodes(nodes);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public void Update()
        {
            try
            {
                logger.LogInformation($"[{this.node.ID}] EXECUTING UPDATE");

                var count = node.Storage.Nodes.Count;
                var nodes = node.Storage.Nodes.ToDictionary(k => k.Key, v => v.Value);

                if (count == 0)
                {
                    logger.LogDebug($"[{this.node.ID}] LIST NODES HAVE 0 KEYS!");
                    node.Storage.Nodes[this.node.ID] = new NodeState { Node = this.node, IP = "0.0.0.0", IsActive = true, Number = DateTime.Now.Ticks ^ this.node.ID, Updated = DateTime.Now };
                }
                else
                {
                    if(nodes.TryGetValue(this.node.ID, out var state))
                    {
                        state.IsActive = true;
                        state.Number = DateTime.Now.Ticks ^ this.node.ID;
                        state.Updated = DateTime.Now;
                        node.Storage.Nodes[this.node.ID] = state;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public INodeService GetMaster()
        {
            try
            {
                var master = node.Storage.Master;
                return master;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            sendMasterTimer = new System.Threading.Timer(async (obj) => await SendToMaster(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            sendChildrenTimer = new System.Threading.Timer(async (obj) => await SendToChildren(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            sendMasterTimer?.Change(Timeout.Infinite, 0);
            sendChildrenTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        //public void Dispose()
        //{
        //    if(images != null)
        //        foreach (var key in images.Keys)
        //            sendTimers[key]?.DisposeAsync();
        //}
        //~StatesService()
        //{
        //    Dispose();
        //}
    }
}
