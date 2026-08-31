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
			var key = Environment.GetEnvironmentVariable("WORKFLOWY_APIKEY_TEST");

			if(string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException("Environment variable WORKFLOWY_APIKEY_TEST must be set to run REAL integration tests.");

			// Optional: 'Production' (default) or 'Beta'. The mirror endpoints require Beta.
			var environment = Environment.GetEnvironmentVariable("WORKFLOWY_ENV_TEST");

			API = Enum.TryParse<WFEnvironment>(environment, true, out var wfEnv)
				? new WFExtendedAPI(key, wfEnv)
				: new WFExtendedAPI(key);
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
			movedNode.ParentId.Should().Be(parentId);

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

		// Requires WORKFLOWY_ENV_TEST=Beta - the mirror endpoints do not exist on Production.
		[Fact]
		public async Task E2E_Mirrors()
		{
			var api = API;

			// -------------------------------------------------------
			// CREATE ORIGIN + DESTINATION NODES
			// -------------------------------------------------------
			var originId = await api.CreateAsync(
				parent: NodeIdentifier.HOME,
				name: "🪞 C# Mirror Origin",
				note: "Created during automated mirror integration test",
				layoutMode: "default",
				position: WFAPI.EPosition.TOP
			);

			originId.Should().NotBe(Guid.Empty);

			var destId = await api.CreateAsync(
				parent: NodeIdentifier.HOME,
				name: "🪞 C# Mirror Destination",
				note: null,
				layoutMode: "default",
				position: WFAPI.EPosition.BOTTOM
			);

			destId.Should().NotBe(Guid.Empty);

			// -------------------------------------------------------
			// CREATE MIRROR
			// -------------------------------------------------------
			var mirror = await api.CreateMirrorAsync(originId, NodeIdentifier.Guid(destId), WFAPI.EPosition.TOP);

			mirror.MirrorId.Should().NotBe(Guid.Empty);
			mirror.MirrorId.Should().NotBe(originId);
			mirror.OriginId.Should().Be(originId);

			// -------------------------------------------------------
			// MIRROR NODE reflects the origin's content
			// -------------------------------------------------------
			var mirrorNode = await api.GetNodeAsync(mirror.MirrorId);
			mirrorNode.IsMirror.Should().BeTrue();
			mirrorNode.Data.Mirror!.OriginId.Should().Be(originId);
			mirrorNode.Name.Should().Be("🪞 C# Mirror Origin");

			// -------------------------------------------------------
			// ORIGIN NODE now points back at the mirror
			// -------------------------------------------------------
			var originNode = await api.GetNodeAsync(originId);
			originNode.IsMirrorOrigin.Should().BeTrue();
			originNode.Data.Mirror!.MirrorIds.Should().Contain(mirror.MirrorId);

			// -------------------------------------------------------
			// DELETE MIRROR (origin stays intact)
			// -------------------------------------------------------
			await api.DeleteMirrorAsync(mirror.MirrorId);

			var originAfter = await api.GetNodeAsync(originId);
			originAfter.IsMirrorOrigin.Should().BeFalse();

			// -------------------------------------------------------
			// DELETING A NON-MIRROR MUST FAIL
			// -------------------------------------------------------
			Func<Task> deleteNonMirror = async () => await api.DeleteMirrorAsync(originId);
			await deleteNonMirror.Should().ThrowAsync<WFAPIException>();

			// -------------------------------------------------------
			// CLEANUP
			// -------------------------------------------------------
			await api.DeleteAsync(destId);
			await api.DeleteAsync(originId);
		}

		// Covers the two documented mirror behaviours E2E_Mirrors does not reach:
		// CreateMirrorAsync following a mirror through to its true origin, and a node
		// with live mirrors refusing to be deleted.
		[Fact]
		public async Task E2E_MirrorOfMirrorAndOriginDeletion()
		{
			var api = API;

			var originId = await api.CreateAsync(NodeIdentifier.HOME, "🪞 C# Mirror-of-Mirror Origin", position: WFAPI.EPosition.TOP);
			var destA = await api.CreateAsync(NodeIdentifier.HOME, "🪞 C# Mirror Dest A", position: WFAPI.EPosition.BOTTOM);
			var destB = await api.CreateAsync(NodeIdentifier.HOME, "🪞 C# Mirror Dest B", position: WFAPI.EPosition.BOTTOM);

			try
			{
				var first = await api.CreateMirrorAsync(originId, NodeIdentifier.Guid(destA));
				first.OriginId.Should().Be(originId);

				// Mirroring a mirror resolves through to the true origin
				var second = await api.CreateMirrorAsync(first.MirrorId, NodeIdentifier.Guid(destB));
				second.OriginId.Should().Be(originId);
				second.MirrorId.Should().NotBe(first.MirrorId);

				// The origin tracks both mirrors
				var originNode = await api.GetNodeAsync(originId);
				originNode.IsMirrorOrigin.Should().BeTrue();
				originNode.Data.Mirror!.MirrorIds.Should().Contain(new[] { first.MirrorId, second.MirrorId });

				// A node with live mirrors cannot be deleted
				Func<Task> deleteOrigin = async () => await api.DeleteAsync(originId);
				await deleteOrigin.Should().ThrowAsync<WFAPIException>();

				// Dropping the mirror roots first releases the origin
				await api.DeleteMirrorAsync(first.MirrorId);
				await api.DeleteMirrorAsync(second.MirrorId);

				var originAfter = await api.GetNodeAsync(originId);
				originAfter.IsMirrorOrigin.Should().BeFalse();
			}
			finally
			{
				// Runs against a real account: never leave the nodes behind.
				foreach(var id in new[] { destA, destB, originId })
				{
					try
					{
						await api.DeleteAsync(id);
					}
					catch(WFAPIException)
					{
					}
				}
			}
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

			// Within CacheTtl the second call is served from the cache: same nodes, same timestamp.
			var res = await api.ExportAllNodesCachedAsync();
			var res_cached = await api.ExportAllNodesCachedAsync();
			res_cached.Should().BeEquivalentTo(res); // cache hit

			// forceRefresh bypasses a still-fresh cache and hits the API again
			var res_fresh = await api.ExportAllNodesCachedAsync(forceRefresh: true);
			res_fresh.Dt.Should().BeAfter(res.Dt);
			res_fresh.Nodes.Should().BeEquivalentTo(res.Nodes);
		}

		[Fact]
		public async Task E2E_CreateAndTestIdentifiers()
		{
			var api = API;
			var created = new List<Guid>();

			async Task CreateUnder(NodeIdentifier parent, string name)
			{
				var res = await api.CreateAsync(parent, name);
				res.Should().NotBe(Guid.Empty);
				created.Add(res);
			}

			try
			{
				foreach(var item in NodeIdentifier.AllIdentifiers)
					await CreateUnder(item, "Test child node under " + item.Identifier);

				await CreateUnder(NodeIdentifier.YearNode(2030), "Test child node under 2030");
				await CreateUnder(NodeIdentifier.MonthNode(2030, 1), "Test child node under 2030 jan");
				await CreateUnder(NodeIdentifier.DateNode(DateTime.Today), "Test child node under TODAY");
			}
			finally
			{
				// This test runs against a real account: do not leave the nodes behind.
				// Swallow cleanup errors so they never mask the actual test failure.
				foreach(var id in created)
				{
					try
					{
						await api.DeleteAsync(id);
					}
					catch(WFAPIException)
					{
					}
				}
			}
		}
	}
}
