using System.Collections.Generic;

namespace NodeHub.Models.Data
{
    public class Block
    {
        public string ID { get; set; }
        public IEnumerable<Replica> Replicas { get; set; }
        public IEnumerable<string> Files { get; set; }
    }
}
