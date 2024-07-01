using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeHub.Models
{
    public class GetTree
    {
        public IEnumerable<NodeTree> Nodes { get; set; }
        public IEnumerable<EdgeTree> Edges { get; set; }
    }

    public struct NodeTree
    {
        public string id { get; set; }
        public string title { get; set; }
        public string label { get; set; }
        public string color { get; set; }
    }

    public struct EdgeTree
    {
        public string from { get; set; }
        public string to { get; set; }
        public string label { get; set; }
    }
}
