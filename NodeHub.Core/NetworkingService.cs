using Microsoft.Extensions.Logging;
using NodeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Core
{
    public class NetworkingService : INetworkingService
    {
        private readonly ILogger logger;
        public NetworkingService(ILogger logger)
        {
            this.logger = logger;
        }

        public Tree GetTree(IEnumerable<BigInteger> nodes)
        {
            var tree = new Dictionary<BigInteger, TreeNode>();

            var countNodes = nodes.Count();
            // Calculating distances
            var distances = GetDistances(nodes);

            Action<IEnumerable<BigInteger>, int> func = (n, l) => { };
            func = (nodes, level) =>
            {
                var countNodes = nodes.Count();
                var masters = new List<BigInteger>();

                Parallel.For(0, countNodes, (i) =>
                {
                    // for current
                    var dists = nodes.Select(n => distances[nodes.ElementAt(i)][n]).ToArray();
                    //logger.LogInformation($"DISTS ({nodes.ElementAt(i)}) {JsonConvert.SerializeObject(dists)}");
                    Array.Sort(dists, new DistanceComparer());
                    //logger.LogInformation($"DISTS 2 ({nodes.ElementAt(i)}) {JsonConvert.SerializeObject(dists)}");
                    var nearFirst = dists.Skip(1).FirstOrDefault();
                    if (nearFirst.Id == BigInteger.Zero) return;
                    // for nearest
                    var secondDists = nodes.Select(n => distances[nearFirst.Id][n]).ToArray();
                    //logger.LogInformation($"SECOND DISTS ({nearFirst.Id}) {JsonConvert.SerializeObject(dists)}");
                    Array.Sort(secondDists, new DistanceComparer());
                    //logger.LogInformation($"SECOND DISTS 2 ({nearFirst.Id}) {JsonConvert.SerializeObject(dists)}");
                    var nearSecond = secondDists.Skip(1).FirstOrDefault();
                    if (nearSecond.Id == BigInteger.Zero) return;

                    if (nodes.ElementAt(i) < nearFirst.Id && nearSecond.Id == nodes.ElementAt(i))
                    {
                        lock (masters)
                        {
                            masters.Add(nodes.ElementAt(i));
                        }

                        return;
                    }

                    lock (tree)
                    {
                        if (tree.TryGetValue(nodes.ElementAt(i), out var treeNode))
                        {
                            treeNode.Master = nearFirst.Id;
                            treeNode.Level = level;
                            tree[nodes.ElementAt(i)] = treeNode;
                        }
                        else
                        {
                            treeNode.Id = nodes.ElementAt(i);
                            treeNode.Master = nearFirst.Id;
                            treeNode.Children = new List<BigInteger>();
                            treeNode.Level = level;
                            tree.Add(nodes.ElementAt(i), treeNode);
                        }

                        if (tree.TryGetValue(nearFirst.Id, out var treeMasterNode))
                        {
                            treeMasterNode.Children.Add(nodes.ElementAt(i));
                            tree[nearFirst.Id] = treeMasterNode;
                        }
                        else
                        {
                            treeMasterNode.Id = nearFirst.Id;
                            treeMasterNode.Children = new List<BigInteger>();
                            treeMasterNode.Children.Add(nodes.ElementAt(i));
                            tree.Add(nearFirst.Id, treeMasterNode);
                        }
                    }
                });

                if (masters.Count > 1)
                    func?.Invoke(masters, ++level);
            };

            func(nodes, 0);
            return new Tree { TreeNodes = tree };
        }

        public Dictionary<BigInteger, Dictionary<BigInteger, Distance>> GetDistances(IEnumerable<BigInteger> nodes)
        {
            try
            {
                var distances = new Dictionary<BigInteger, Dictionary<BigInteger, Distance>>();
                foreach (var x in nodes)
                {
                    distances.Add(x, new Dictionary<BigInteger, Distance>());
                    foreach (var y in nodes)
                    {
                        var distance = new Distance
                        {
                            Id = y,
                            Dis = BigInteger.Abs(x ^ y)
                        };
                        distances[x].Add(y, distance);
                    }
                }
                return distances;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        public IEnumerable<DistanceBlock> GetDistances(BigInteger block, IEnumerable<BigInteger> nodes)
        {
            try
            {
                var distances = new List<DistanceBlock>();
                foreach (var n in nodes)
                {
                    distances.Add(new DistanceBlock
                    {
                        Node = n,
                        Block = block,
                        Dis = BigInteger.Abs(n ^ block)
                    });
                }
                return distances;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        public InnerOuterRegions GetRegions(Tree tree, BigInteger baselineID)
        {
            try
            {
                var treeNodes = tree.TreeNodes;
                var innerRegion = GetInnerRegion(tree, baselineID);
                var outerRegion = GetOuterRegion(tree, baselineID);
                var innerOuterRegions = new InnerOuterRegions(innerRegion, outerRegion);

                return innerOuterRegions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        public InnerRegion GetInnerRegion(Tree tree, BigInteger baselineID)
        {
            var innerRegion = new List<BigInteger>();
            Action<BigInteger, Dictionary<BigInteger, TreeNode>> innerAction = (c, t) => { };

            innerAction = (current, tree) =>
            {
                innerRegion.Add(current);
                if(tree[current].Children != null)
                {
                    foreach (var node in tree[current].Children)
                    {
                        innerAction?.Invoke(node, tree);
                    }
                }
            };

            innerAction(baselineID, tree.TreeNodes);
            return new InnerRegion(innerRegion);
        }

        public OuterRegion GetOuterRegion(Tree tree, BigInteger baselineID)
        {
            Action<BigInteger, BigInteger, Dictionary<BigInteger, TreeNode>> outerAction = (e, c, t) => { };
            Action<BigInteger, Dictionary<BigInteger, TreeNode>> childrenOuterAction = (c, t) => { };
            var outerRegion = new List<BigInteger>();

            outerAction = (except, current, tree) =>
            {
                if (current == 0) return;

                lock (outerRegion)
                {
                    outerRegion.Add(current);
                }
                outerAction?.Invoke(current, tree[current].Master, tree);

                Parallel.ForEach(tree[current].Children, (node) =>
                {
                    if (node != except) childrenOuterAction?.Invoke(node, tree);
                });
            };

            childrenOuterAction = (current, tree) =>
            {
                lock (outerRegion)
                {
                    outerRegion.Add(current);
                }

                Parallel.ForEach(tree[current].Children, (node) =>
                {
                    childrenOuterAction?.Invoke(node, tree);
                });
            };

            outerAction(baselineID, tree[baselineID].Master, tree.TreeNodes);

            return new OuterRegion(outerRegion);
        }

        public BigInteger GetNodesHash(IEnumerable<Node.Core.Models.Node> nodes)
        {
            var hash = nodes.Aggregate(BigInteger.Zero, (h, s) => h ^= s.Id);
            return hash;
        }
    }

    public class DistanceComparer : IComparer<Distance>
    {
        int IComparer<Distance>.Compare([AllowNull] Distance x, [AllowNull] Distance y)
        {
            if (x.Dis > y.Dis) return 1;
            else if (x.Dis < y.Dis) return -1;
            else return 0;
        }
    }

    public class DistanceBlockComparer : IComparer<DistanceBlock>
    {
        int IComparer<DistanceBlock>.Compare([AllowNull] DistanceBlock x, [AllowNull] DistanceBlock y)
        {
            if (x.Dis > y.Dis) return 1;
            else if (x.Dis < y.Dis) return -1;
            else return 0;
        }
    }
}
