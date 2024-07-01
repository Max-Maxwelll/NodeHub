using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NodeHub.Core.Models
{
    public struct InnerOuterRegions
    {
        public InnerRegion InnerRegion { get; set; }
        public OuterRegion OuterRegion { get; set; }
        public InnerOuterRegions(InnerRegion inner, OuterRegion outer) => (this.InnerRegion, this.OuterRegion) = (inner, outer);
    }
}
