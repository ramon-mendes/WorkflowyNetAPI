using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowyNetAPI
{
    public class WFTreeNode
    {
        public WFNode Node { get; set; } = null!;

		public WFTreeNode Parent { get; set; } = null!;

        public List<WFTreeNode> Children { get; set; } = new List<WFTreeNode>();
	}
}
