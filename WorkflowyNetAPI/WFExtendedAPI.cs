using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WorkflowyNetAPI.Tree;

namespace WorkflowyNetAPI
{
    public class WFExtendedAPI : WFAPI
    {
        public WFExtendedAPI(string api_key) : base(api_key)
		{
        }

		// method that calls ExportAllNodes and reconstructs the tree structure
        public async Task<WFTreeNode[]> GetAllNodesAsTreeAsync()
        {
            var allNodes = (await ExportAllNodesAsync()).Nodes;

			// Create a dictionary to hold all nodes as WFTreeNode by their IDs
			var nodeTreeDict = allNodes.ToDictionary(n => n.Id, n => new WFTreeNode()
			{
				Node = n
			});

            // Create a list to hold root nodes
            var rootNodes = new List<WFTreeNode>();

            // Iterate through all nodes and build the tree structure
            foreach (var node in allNodes)
            {
                var nodeTree = nodeTreeDict[node.Id];

				if (node.ParentId != null)
                {
                    var parentTreeNode = nodeTreeDict[node.ParentId];
                    Debug.Assert(parentTreeNode != null, "Parent node should exist in the dictionary.");

					nodeTree.Parent = parentTreeNode;
					parentTreeNode.Children.Add(nodeTree);
				}
                else
                {
                    // If the node has no parent, it's a root node
                    rootNodes.Add(nodeTree);
                }
            }

            Debug.Assert(rootNodes.All(node => node.Parent == null), "All root nodes should have no parent.");
            return rootNodes.ToArray();
		}
	}
}
