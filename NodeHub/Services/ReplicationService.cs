using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public class ReplicationService : IReplicationService, IHostedService
    {
        private INodeService node;
        private readonly INetworkingService networking;
        private readonly IHubContext<CommonHub, ICommonHub> commonHub;
        private readonly ILogger logger;
        private Timer clearGroupsTimer;
        private Timer restoreTimer;
        public ReplicationService(INetworkingService networking, IHubContext<CommonHub, ICommonHub> commonHub, ILogger logger)
        {
            this.networking = networking;
            this.commonHub = commonHub;
            this.logger = logger;
        }
        public IReplicationService InjectionNode(INodeService node)
        {
            if (this.node == null) this.node = node;
            logger.LogInformation($"[{node.ID}] REPLICATION SERVICE STARTED!");
            return this;
        }

        public async ValueTask<bool> Send(BigInteger file, BigInteger block, byte[] bytes)
        {
            try
            {
                logger.LogDebug("EXECUTING 'SEND BLOCK' METHOD");
                var nodes = this.node.Storage.Nodes.Where(x => x.Value.IsActive).ToDictionary(k => k.Key, v => v.Value);
                var distances = networking.GetDistances(block, nodes.Select(x => x.Value.Node.ID)).ToArray();
                Array.Sort(distances, new DistanceBlockComparer());

                var firstThreeNodes = distances.Take(3)?.Select(x => nodes[x.Node]);

                foreach (var n in firstThreeNodes)
                {
                    await n.Node.Replication.Add(firstThreeNodes.Select(x => x.Node), new List<BigInteger> { file }, block, bytes);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public ValueTask<bool> Add(IEnumerable<INodeService> members, IEnumerable<BigInteger> files, BigInteger blockID, byte[] bytes)
        {
            logger.LogDebug("EXECUTING 'ADD' METHOD");
            try
            {
                var groupID = members.Aggregate(BigInteger.Zero, (f, s) => f ^= s.ID);
                if (this.node.Storage.Groups.TryGetValue(groupID, out var group))
                {
                    if (group.Blocks.TryGetValue(blockID, out var block))
                    {
                        foreach(var file in files)
                        {
                            if (!block.Files.Contains(file))
                            {
                                block.Files.Add(file);
                                group.Blocks[blockID] = block;
                            }
                        }
                        block.Replicas[this.node.ID] = new Replica { Type = ReplicaType.Primary, Ready = true };
                        block.LastUpdate = DateTime.Now;
                    }
                    else
                    {
                        block = new Block(files);
                        block.Replicas[this.node.ID] = new Replica { Type = ReplicaType.Primary, Ready = true };
                        group.Blocks[blockID] = block;
                    }
                    group.Hash ^= blockID;
                }
                else
                {
                    var block = new Block(files);
                    block.Replicas[this.node.ID] = new Replica { Type = ReplicaType.Primary, Ready = true };
                    group = new Group() {
                        ID = groupID,
                        Members = members.Select(x => x.ID),
                        Blocks = new Dictionary<BigInteger, Block> { { blockID, block } },
                        Hash = blockID
                    };
                    this.node.Storage.Groups.Add(groupID, group);
                        
                }
                this.node.Storage.Blocks[blockID] = bytes;
                NotifyGroup(members, this.node.ID, files, blockID);
                commonHub.Clients.All.AddBlock(this.node.ID.ToString());
                return new ValueTask<bool>(true);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public ValueTask<bool> Replicated(IEnumerable<BigInteger> members, BigInteger replica, IEnumerable<BigInteger> files, BigInteger blockID)
        {
            logger.LogDebug("EXECUTING 'REPLICATED' METHOD");
            try
            {
                var groupID = members.Aggregate(BigInteger.Zero, (f, s) => f ^= s);

                if (this.node.Storage.Groups.TryGetValue(groupID, out var group))
                {
                    if (group.Blocks.TryGetValue(blockID, out var block))
                    {
                        foreach (var file in files)
                        {
                            if (!block.Files.Contains(file))
                            {
                                block.Files.Add(file);
                                group.Blocks[blockID] = block;
                            }
                        }
                        block.Replicas[replica] = new Replica { Type = ReplicaType.Secondary, Ready = true };
                        block.LastUpdate = DateTime.Now;
                    }
                    else
                    {
                        block = new Block(files);
                        block.Replicas[replica] = new Replica { Type = ReplicaType.Secondary, Ready = true };
                        group.Blocks.Add(blockID, block);
                    }
                }
                else
                {
                    var block = new Block(files);
                    block.Replicas[replica] = new Replica { Type = ReplicaType.Secondary, Ready = true };
                    group = new Group() {
                        ID = groupID,
                        Members = members,
                        Blocks = new Dictionary<BigInteger, Block>() 
                    };
                    group.Blocks.Add(blockID, block);

                    this.node.Storage.Groups.Add(groupID, group);
                }

                if (this.node.Storage.Members.TryGetValue(replica, out var member))
                {
                    if (!member.Groups.Contains(groupID))
                    {
                        member.Groups.Add(groupID);
                        this.node.Storage.Members[replica] = member;
                    }
                }
                else
                {
                    this.node.Storage.Members[replica] = new Member(groupID);
                }
                commonHub.Clients.All.AddBlock(this.node.ID.ToString());
                return new ValueTask<bool>(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public ValueTask<GetBlockResult> Get(BigInteger blockID, List<BigInteger> chain = null)
        {
            try
            {
                chain = chain ?? new List<BigInteger>();

                chain.Add(this.node.ID);
                if (this.node.Storage.Blocks.TryGetValue(blockID, out var block))
                {
                    return new ValueTask<GetBlockResult>(new GetBlockResult(chain, block));
                }
                else
                {
                    var nodes = this.node.Storage.Nodes.Where(x => x.Value.IsActive).ToDictionary(k => k.Key, v => v.Value);
                    var distances = networking.GetDistances(blockID, nodes.Select(x => x.Key)).ToArray();
                    Array.Sort(distances, new DistanceBlockComparer());
                    foreach (var n in distances)
                    {
                        if (!chain.Contains(n.Node))
                        {
                            var nearestNode = nodes[n.Node];
                            return nearestNode.Node.Replication.Get(blockID, chain);
                        }
                    }
                }
                return new ValueTask<GetBlockResult>(new GetBlockResult(chain, new byte[0]));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public async ValueTask<bool> Restore(BigInteger lostNode)
        {
            try
            {
                var members = this.node.Storage.Members;
                if (members.TryGetValue(lostNode, out var member))
                {
                    var groups = this.node.Storage.Groups;
                    var needToRestore = member.Groups.ToList().Select(x => groups[x]).ToList();

                    await Restore(needToRestore);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        private ValueTask<IEnumerable<BigInteger>> Restore(IEnumerable<Group> groups)
        {
            try
            {
                logger.LogDebug("EXECUTING 'RESTORE' METHOD");
                if (this.node.Storage.ActiveTree.TreeNodes == null) return new ValueTask<IEnumerable<BigInteger>>();
                var restored = new List<BigInteger>();
                var activeTree = this.node.Storage.ActiveTree;
                var nodes = activeTree.TreeNodes.Select(x => this.node.Storage.Nodes[x.Key]).ToDictionary(k => k.Node.ID);

                foreach (var group in groups.Where(x => !x.Restored))
                {
                    var blocks = group.Blocks.ToList().ToDictionary(k => k.Key, v => v.Value);
                    foreach (var block in blocks)
                    {
                        var distances = networking.GetDistances(block.Key, nodes.Select(x => x.Value.Node.ID)).ToArray();
                        Array.Sort(distances, new DistanceBlockComparer());

                        var nearestNodes = distances.Take(3)?.Select(x => nodes[x.Node]);
                        foreach (var n in nearestNodes.Where(x => x.Node.ID != this.node.ID))
                        {
                            var data = this.node.Storage.Blocks[block.Key];
                            var res = n.Node.Replication.Add(nearestNodes.Select(x => x.Node), block.Value.Files, block.Key, data);
                        }
                    }
                    var updatedGroup = group;
                    updatedGroup.Restored = true;
                    this.node.Storage.Groups[group.ID] = updatedGroup;
                    restored.Add(group.ID);
                }
                return new ValueTask<IEnumerable<BigInteger>>(restored);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public async ValueTask<bool> RestoreLoop()
        {
            try
            {
                logger.LogDebug("EXECUTING 'RestoreLoop' METHOD");
                var nodes = this.node.Storage.Nodes.ToList().Select(x => x.Value).Where(x => !x.IsActive);

                foreach (var n in nodes)
                {
                    var period = (DateTime.Now - n.Updated);
                    if (period.TotalMilliseconds > 5000)
                    {
                        var res = await Restore(n.Node.ID);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public async ValueTask<bool> ClearGroups()
        {
            try
            {
                logger.LogDebug("EXECUTING 'CLEAR GROUP' METHOD");
                var groups = this.node.Storage.Groups.Select(x => x.Value).ToList();
                var nodes = this.node.Storage.Nodes.ToList().ToDictionary(k => k.Key, v => v.Value);
                foreach (var group in groups)
                {
                    await Task.Run(() =>
                    {
                        var members = group.Members.ToList();
                        if (group.Restored && members.Where(m => nodes[m].IsActive).Count() != members.Count())
                        {
                            //var delBlocks = group.Blocks.ToList();
                            //foreach (var delBlock in delBlocks)
                            //{
                            //    this.node.Storage.Blocks.Remove(delBlock.Key);
                            //}
                            this.node.Storage.Groups.Remove(group.ID);
                            foreach(var memberId in group.Members.Where(x => x != this.node.ID))
                            {
                                var member = this.node.Storage.Members[memberId];

                                if(member.Groups != null)
                                {
                                    member.Groups.Remove(group.ID);
                                }
                            }
                            commonHub.Clients.All.AddBlock(this.node.ID.ToString());
                        }
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[{this.node.ID}] {ex.Message}");
                throw ex;
            }
        }

        public ValueTask<bool> Delete(BigInteger hash)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> ResponseAdd(IEnumerable<BigInteger> members, BigInteger replicator, IEnumerable<BigInteger> files, BigInteger block)
        {
            return await Replicated(members, replicator, files, block);
        }

        private ValueTask<bool> NotifyGroup(IEnumerable<INodeService> members, BigInteger requester, IEnumerable<BigInteger> files, BigInteger block)
        {
            foreach (var member in members.Where(x => x.ID != this.node.ID))
            {
                member.Replication.ResponseAdd(members.Select(x => x.ID), requester, files, block);
            }
            return new ValueTask<bool>(true);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            clearGroupsTimer = new System.Threading.Timer(async (obj) => await ClearGroups(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(5000));
            restoreTimer = new System.Threading.Timer(async (obj) => await RestoreLoop(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(5000));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            clearGroupsTimer?.Change(Timeout.Infinite, 0);
            restoreTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
