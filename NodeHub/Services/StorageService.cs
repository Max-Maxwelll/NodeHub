using Node.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodeHub.Core.Models;
using NodeHub.Services.Interfaces;
using System.Numerics;
using NodeHub.Services;

namespace Node.Web.Services
{
    public class StorageService : IStorageService
    {
        public INodeService Master
        {
            get
            {
                lock (locks[0])
                {
                    return master;
                }
            }
            set
            {
                lock (locks[0])
                {
                    master = value;
                }
            }
        }
        public Tree ActiveTree
        {
            get
            {
                lock(locks[1])
                {
                    return activeTree;
                }
            }
            set
            {
                lock(locks[1])
                {
                    activeTree = value;
                }
            }
        }
        public Tree FullTree
        {
            get
            {
                lock (locks[2])
                {
                    return fullTree;
                }
            }
            set
            {
                lock (locks[2])
                {
                    fullTree = value;
                }
            }
        }
        public InnerRegion InnerRegion
        {
            get
            {
                lock (locks[3])
                {
                    return innerRegion;
                }
            }
            set
            {
                lock (locks[3])
                {
                    innerRegion = value;
                }
            }
        }
        public OuterRegion OuterRegion
        {
            get
            {
                lock (locks[4])
                {
                    return outerRegion;
                }
            }
            set
            {
                lock(locks[4])
                {
                    outerRegion = value;
                }
            }
        }

        public Dictionary<BigInteger, Group> Groups
        {
            get
            {
                lock (groups)
                {
                    return groups;
                }
            }
        }

        public Dictionary<BigInteger, byte[]> Blocks
        {
            get
            {
                lock (blocks)
                {
                    return blocks;
                }
            }
        }

        public Dictionary<BigInteger, Member> Members
        {
            get
            {
                lock (members)
                {
                    return members;
                }
            }
        }

        public Dictionary<BigInteger, List<BigInteger>> NumbersStates => numbersStates;

        public Dictionary<BigInteger, NodeState> Nodes => nodes;

        private INodeService master;
        private Tree activeTree;
        private Tree fullTree;
        private InnerRegion innerRegion;
        private OuterRegion outerRegion;
        private Dictionary<int, object> locks;
        private Dictionary<BigInteger, NodeState> nodes = new Dictionary<BigInteger, NodeState>();
        private Dictionary<BigInteger, Group> groups = new Dictionary<BigInteger, Group>();
        private Dictionary<BigInteger, Member> members = new Dictionary<BigInteger, Member>();
        private Dictionary<BigInteger, byte[]> blocks = new Dictionary<BigInteger, byte[]>();
        private Dictionary<BigInteger, List<BigInteger>> numbersStates = new Dictionary<BigInteger, List<BigInteger>>();
        public StorageService()
        {
            this.locks = new Dictionary<int, object>
            {
                { 0, new object() },
                { 1, new object() },
                { 2, new object() },
                { 3, new object() },
                { 4, new object() }
            };
        }
        public IEnumerable<NodeState> GetNodes(IEnumerable<BigInteger> keys)
        {
            foreach (var key in keys)
                if (nodes.TryGetValue(key, out var value))
                    yield return value;
        }
    }
}
