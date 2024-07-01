using NodeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Services.Interfaces
{
    public interface IReplicationService
    {
        IReplicationService InjectionNode(INodeService node);
        ValueTask<GetBlockResult> Get(BigInteger block, List<BigInteger> chain = default);
        ValueTask<bool> Send(BigInteger file, BigInteger block, byte[] bytes);
        ValueTask<bool> Add(IEnumerable<INodeService> members, IEnumerable<BigInteger> files, BigInteger blockID, byte[] bytes);
        ValueTask<bool> Delete(BigInteger hash);
        ValueTask<bool> ResponseAdd(IEnumerable<BigInteger> members, BigInteger replicator, IEnumerable<BigInteger> files, BigInteger block);
        ValueTask<bool> Restore(BigInteger lostNode);
        ValueTask<bool> ClearGroups();
    }
}
