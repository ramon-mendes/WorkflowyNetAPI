using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Types;
using WorkflowyNetAPI;
using WorkflowyNetAPI.DTOs;

namespace WorkflowyNetAPI.Tests
{
	public class E2E_Tests
	{
		private static WFExtendedAPI API;

		static E2E_Tests()
		{
			var key = Environment.GetEnvironmentVariable("WORKFLOWY_APIKEY");

			if(string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException("Environment variable WORKFLOWY_APIKEY must be set to run REAL integration tests.");

			API = new WFExtendedAPI(key);
		}

		[Fact]
		public async Task E2E_EntireFlow()
		{
			var api = API;

			// -------------------------------------------------------
			// CREATE NODE
			// -------------------------------------------------------
			var testNodeId = await api.CreateAsync(
				parent:	NodeIdentifier.HOME,
				name: "🧪 C# Integration Test Node",
				note: "Created during automated integration test",
				layoutMode: "default",
				position: WFAPI.EPosition.TOP
			);

			testNodeId.Should().NotBe(Guid.Empty);

			// -------------------------------------------------------
			// FETCH NODE
			// -------------------------------------------------------
			var node = await api.GetNodeAsync(testNodeId);
			node.Id.Should().Be(testNodeId);
			node.ParentId.Should().BeNull();

			// -------------------------------------------------------
			// UPDATE NODE
			// -------------------------------------------------------
			await api.UpdateNodeAsync(new WFNodeUpdate
			{
				Id = testNodeId.ToString(),
				Name = "🧠 C# Integration Test Node (Updated)"
			});

			// Verify rename
			var updatedNode = await api.GetNodeAsync(testNodeId);
			updatedNode.Name.Should().Contain("(Updated)");

			// -------------------------------------------------------
			// COMPLETE
			// -------------------------------------------------------
			await api.CompleteAsync(testNodeId);

			// Quick check: complete flag should be true
			var completedNode = await api.GetNodeAsync(testNodeId);
			completedNode.Completed.Should().BeTrue();

			// -------------------------------------------------------
			// UNCOMPLETE
			// -------------------------------------------------------
			await api.UncompleteAsync(testNodeId);

			var uncompletedNode = await api.GetNodeAsync(testNodeId);
			uncompletedNode.Completed.Should().BeFalse();

			// -------------------------------------------------------
			// FETCH ROOT NODES
			// -------------------------------------------------------
			var root_nodes = await api.GetRootNodesAsync();
			root_nodes.SingleOrDefault(nd => nd.Id == testNodeId).Should().NotBeNull();

			// -------------------------------------------------------
			// CREATE PARENT NODE
			// -------------------------------------------------------
			var parentId = await api.CreateAsync(
				parent: NodeIdentifier.HOME,
				name: "🧪 C# Integration Test Parent Node",
				note: null,
				layoutMode: "default",
				position: WFAPI.EPosition.BOTTOM
			);

			parentId.Should().NotBe(Guid.Empty);

			// -------------------------------------------------------
			// MOVE NODE under parent
			// -------------------------------------------------------
			await api.MoveAsync(testNodeId, NodeIdentifier.Guid(parentId), WFAPI.EPosition.BOTTOM);

			var movedNode = await api.GetNodeAsync(testNodeId);
			movedNode.ParentId.Should().Be(null);

			// -------------------------------------------------------
			// GET NODE by hash
			// -------------------------------------------------------
			Thread.Sleep(TimeSpan.FromMinutes(1));// for cache to update

			var node_by_hash = await api.FindNodeByHash(testNodeId.ToString().Substring(24), true);
			node_by_hash.Should().NotBeNull();

			// -------------------------------------------------------
			// DELETE PARENT
			// -------------------------------------------------------
			await api.DeleteAsync(parentId);

			// check if deleted
			Func<Task> fetchParent = async () => await api.GetNodeAsync(parentId);
			await fetchParent.Should().ThrowAsync<WFAPIException>();

			// -------------------------------------------------------
			// CHECK IF CHILD WAS DELETED
			// -------------------------------------------------------
			Func<Task> fetchChild = async () => await api.GetNodeAsync(testNodeId);
			await fetchChild.Should().ThrowAsync<WFAPIException>();
		}

		[Fact]
		public async Task E2E_ListTargets()
		{
			var api = API;
			var res = await api.ListTargetsAsync();
			res.Should().NotBeEmpty();
		}

		[Fact]
		public async Task E2E_CacheVerification()
		{
			var api = API;

			// verify cache returns the same result on subsequent calls
			var res = await api.ExportAllNodesCachedAsync();
			var res_cached = await api.ExportAllNodesCachedAsync();
			res_cached.Should().BeEquivalentTo(res); // cache hit
		}

		[Fact]
		public async Task E2E_CreateAndTestIdentifiers()
		{
			var api = API;
			Guid res;

			foreach(var item in NodeIdentifier.AllIdentifiers)
			{
				res = await api.CreateAsync(item, "Test child node under " + item.Identifier);
				res.Should().NotBe(Guid.Empty);
			}

			res = await api.CreateAsync(NodeIdentifier.YearNode(2030), "Test child node under 2030");
			res.Should().NotBe(Guid.Empty);

			res = await api.CreateAsync(NodeIdentifier.MonthNode(2030, 1), "Test child node under 2030 jan");
			res.Should().NotBe(Guid.Empty);

			res = await api.CreateAsync(NodeIdentifier.DateNode(DateTime.Today), "Test child node under TODAY");
			res.Should().NotBe(Guid.Empty);
		}
	}
}
