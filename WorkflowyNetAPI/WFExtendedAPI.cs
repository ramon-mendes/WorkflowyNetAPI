using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowyNetAPI
{
    public class WFExtendedAPI : WFAPI
    {
        public WFExtendedAPI(string api_key) : base(api_key)
		{
            
        }

		// method that calls ExportAllNodes and reconstructs the tree structure
        public async Task<WFNode[]> GetAllNodesAsTreeAsync()
        {
            var allNodes = await ExportAllNodesAsync();

            // Create a dictionary to hold nodes by their IDs for easy lookup
            var nodeDict = allNodes.ToDictionary(n => n.Id, n => n);

            // Create a list to hold root nodes
            var rootNodes = new List<WFNode>();

            // Iterate through all nodes and build the tree structure
            foreach (var node in allNodes)
            {
                if (node.ParentId != null && nodeDict.ContainsKey(node.ParentId))
                {
                    // If the node has a parent, add it to the parent's children
                    var parentNode = nodeDict[node.ParentId];
                    if (parentNode.Data.Children == null)
                    {
                        parentNode.Data.Children = new List<WFNode>();
                    }
                    parentNode.Data.Children.Add(node);
                }
                else
                {
                    // If the node has no parent, it's a root node
                    rootNodes.Add(node);
                }
            }
            return rootNodes.ToArray();
		}
	}
}
