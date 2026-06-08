using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WorkflowyNetAPI.DTOs;
using WorkflowyNetAPI.Tree;

namespace WorkflowyNetAPI
{
    public class WFExtendedAPI : WFAPI
    {
		// Simple in-memory cache shared across instances of WFExtendedAPI.
		// Keep fields private and thread-safe with a lock.
		private WFNode[]? _exportCache;
		private DateTime _exportCacheAtUtc;
		private readonly object _exportCacheLock = new();

		public WFExtendedAPI(string api_key) : base(api_key)
		{
        }
		public async Task<WFNode[]> GetRootNodesAsync()
		{
			return await GetChildNodesAsync(NodeIdentifier.HOME);
		}

		// Calls ExportAllNodes and if it throws WFAPIException with "HTTP 429 - Too Many Requests" StatusCode
		// return from cache if available, otherwise rethrow the exception
		public async Task<(WFNode[] Nodes, DateTime Dt)> ExportAllNodesCachedAsync()
		{
			try
			{
				WFNodesResponse resp = await ExportAllNodesAsync();

				// Update cache
				var now = DateTime.UtcNow;
				lock(_exportCacheLock)
				{
					_exportCache = resp.Nodes;
					_exportCacheAtUtc = now;
				}

				return (resp.Nodes, now);
			}
			catch(WFAPIException ex) when(ex.StatusCode == 429)
			{
				// Rate limited — return cached value if available
				lock(_exportCacheLock)
				{
					if(_exportCache != null)
					{
						return (_exportCache, _exportCacheAtUtc);
					}
				}

				throw;
			}
		}

		public void ClearCache()
		{
			lock(_exportCacheLock)
			{
				_exportCache = null;
				_exportCacheAtUtc = default;
			}
		}

		// Calls ExportAllNodes and reconstructs the tree structure, returning a WFTree instance.
		public async Task<WFTree> GetNodesTreeAsync()
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

				// Treat missing parent as either null or Guid.Empty (some deserialization paths set default to Guid.Empty)
				if (node.ParentId.HasValue && node.ParentId.Value != Guid.Empty && nodeTreeDict.TryGetValue(node.ParentId.Value, out var parentTreeNode))
                {
					// Attach to parent
					nodeTree.Parent = parentTreeNode;
					parentTreeNode.Children.Add(nodeTree);
				}
                else
                {
                    // If the node has no parent or parent is missing, treat it as a root node
                    rootNodes.Add(nodeTree);
                }
            }

            // Sort children of every node by priority (ascending) only
            foreach (var treeNode in nodeTreeDict.Values)
            {
				if (treeNode.Children.Count <= 1)
					continue;

				treeNode.Children.Sort((a, b) =>
				{
					return a.Node.Priority.CompareTo(b.Node.Priority);
				});
            }

            // Sort root nodes by priority (ascending) only
            rootNodes.Sort((a, b) =>
            {
				return a.Node.Priority.CompareTo(b.Node.Priority);
            });

            Debug.Assert(rootNodes.All(node => node.Parent == null), "All root nodes should have no parent.");
            return new WFTree { RootNodes = rootNodes.ToArray() };
		}

		public async Task<WFNode?> FindNodeByHash(string hash, bool enforce_new_cache = false)
		{
			if(hash.Length != 12)
				throw new ArgumentException("Hash must be 12 characters long.", nameof(hash));

			// Clear cache to force fresh data retrieval
			if(enforce_new_cache)
				ClearCache();

			var cache = await ExportAllNodesCachedAsync();
			return cache.Nodes.FirstOrDefault(n => n.Id.ToString().EndsWith(hash));
		}
	}
}
