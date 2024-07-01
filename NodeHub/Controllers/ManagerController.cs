using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NodeHub.Models;
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
    public class ManagerController : Controller
    {
        private readonly Dictionary<BigInteger, INodeService> nodes;
        public ManagerController(IEnumerable<INodeService> nodes)
        {
            this.nodes = nodes.ToDictionary(k => k.ID);
        }
        [HttpGet]
        public async Task<IActionResult> NodeStop(string id)
        {
            if(nodes.TryGetValue(BigInteger.Parse(id), out var node))
            {
                await node?.StopAsync(default);
            }
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> NodeStart(string id)
        {
            if (nodes.TryGetValue(BigInteger.Parse(id), out var node))
            {
                await node?.StartAsync(default);
            }
            return Ok();
        }
        [HttpGet]
        public IActionResult Connect()
        {
            foreach(var n in nodes)
            {
                foreach(var n2 in nodes)
                {
                    n.Value.Connection.Connect(n2.Value);
                }
            }

            return Ok();
        }
        [HttpGet]
        public IActionResult GetActiveTree(string id)
        {
            if(nodes.TryGetValue(BigInteger.Parse(id), out var node))
            {
                var tree = node.Storage.ActiveTree.TreeNodes;

                if (tree != null)
                {
                    var listNodes = tree
                        .Select(x => nodes[x.Key])
                        .Select(x => new NodeTree
                        {
                            id = x.ID.ToString(),
                            title = x.ID.ToString(),
                            label = x.ID.ToString(),
                            color = "aqua"
                        });
                    List<EdgeTree> edges = new List<EdgeTree>();

                    foreach(var n in tree.Values)
                    {
                        foreach(var c in n.Children)
                        {
                            edges.Add(new EdgeTree { from = c.ToString(), to = n.Id.ToString(), label = $"{c} => {n.Id}" });
                        }
                    }
                    return Ok(new GetTree { Nodes = listNodes, Edges = edges });
                }
            }
            return Ok();
        }
        [HttpGet]
        public IActionResult GetFullTree(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (nodes.TryGetValue(BigInteger.Parse(id), out var node))
                {
                    var tree = node.Storage.FullTree.TreeNodes;

                    if (tree != null)
                    {
                        var listNodes = tree
                            .Select(x => nodes[x.Key])
                            .Select(x => new NodeTree
                            {
                                id = x.ID.ToString(),
                                title = x.ID.ToString(),
                                label = x.ID.ToString(),
                                color = node.Storage.Nodes[x.ID].IsActive ? "green" : "orange"
                            });
                        List<EdgeTree> edges = new List<EdgeTree>();

                        foreach (var n in tree.Values)
                        {
                            foreach (var c in n.Children)
                            {
                                edges.Add(new EdgeTree { from = c.ToString(), to = n.Id.ToString(), label = $"{c} => {n.Id}" });
                            }
                        }
                        return Ok(new GetTree { Nodes = listNodes, Edges = edges });
                    }
                }
            }
            return Ok();
        }
    }
}
