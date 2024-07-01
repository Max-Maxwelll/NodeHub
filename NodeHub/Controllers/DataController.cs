using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeHub.Core.Models;
using NodeHub.Models.Data;
using NodeHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NodeHub.Controllers
{
    [Route("[controller]/[action]")]
    [AllowAnonymous]
    public class DataController : Controller
    {
        private readonly Dictionary<BigInteger, INodeService> nodes;
        public DataController(IEnumerable<INodeService> nodes)
        {
            this.nodes = nodes.ToDictionary(k => k.ID);
        }
        [HttpGet]
        public IActionResult SendBlock(string receiver, string file, string block)
        {
            var r = BigInteger.Parse(receiver);
            var f = BigInteger.Parse(file);
            var b = BigInteger.Parse(block);
            if(this.nodes.TryGetValue(r, out var node))
            {
                node.Replication.Send(f, b, new byte[] { 1, 2, 3 });
            }
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetBlock(string receiver, string block)
        {
            var r = BigInteger.Parse(receiver);
            var b = BigInteger.Parse(block);

            if (this.nodes.TryGetValue(r, out var node))
            {
                var result = await node.Replication.Get(b);
                return Ok(new BlockResult()
                {
                    Chain = result.Chain.Select(x => x.ToString()),
                    BlockLength = result.Block != null ? result.Block.Length : 0
                });
            }
            return Ok();
        }
        [HttpGet]
        public IActionResult _GetBlocks()
        {
            var nodes = this.nodes.Values
                .Select(x => new Models.Data.Node
                {
                    ID = x.ID.ToString(),
                    IsActive = x.Online,
                    Groups = x.Storage.Groups
                    .Select(g => new Models.Data.Group
                    {
                        ID = g.Value.ID.ToString(),
                        Blocks = g.Value.Blocks
                        .Select(b => new Models.Data.Block
                        {
                            ID = b.Key.ToString(),
                            Files = b.Value.Files.Select(x => x.ToString()),
                            Replicas = b.Value.Replicas
                            .Select(r => new Models.Data.Replica
                            {
                                ID = r.Key.ToString(),
                                Type = r.Value.Type.ToString()
                            })
                        })
                    })
                }).ToList();
            return View(nodes);
        }
    }
}
