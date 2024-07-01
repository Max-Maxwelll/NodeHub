using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeHub.Models.Monitor;
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
    public class MonitorController : Controller
    {
        private readonly IEnumerable<INodeService> nodes;
        public MonitorController(IEnumerable<INodeService> nodes)
        {
            this.nodes = nodes;
        }
        [HttpGet]
        public IActionResult _GetNodes()
        {
            var model = nodes.Select(x =>
            new NodeModel
            {
                ID = x.ID,
                Online = x.Online,
                IP = x.IP,
                Master = x.Storage.Master?.ID ?? BigInteger.Zero
            });
            return PartialView(model);
        }
        [HttpGet]
        public IActionResult _GetActiveNodes(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var node = nodes.FirstOrDefault(x => x.ID == BigInteger.Parse(id));
                var model = node.Storage.Nodes.Where(x => x.Value.IsActive)
                    .Select(x =>
                    new NodeModel
                    {
                        ID = x.Key,
                        Online = false,
                        IP = x.Value.IP,
                        Master = x.Value.Node.Storage.Master?.ID ?? BigInteger.Zero
                    }).OrderBy(x => x.ID);
                return PartialView(model);
            }
            return PartialView(new List<NodeModel>());
        }
    }
}
