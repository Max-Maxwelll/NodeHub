using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct Tree
    {
        public TreeNode this[BigInteger key]
        {
            get
            {
                TreeNode value = new TreeNode();
                TreeNodes?.TryGetValue(key, out value);
                return value;
            }
        }
        public static Guid Key { get; set; } = new Guid("b89da7c0-22c2-4728-ab40-f740d858ccfb");

        public Dictionary<BigInteger, TreeNode> TreeNodes { get; set; }
    }
}
