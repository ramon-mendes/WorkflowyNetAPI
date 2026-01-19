using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using WorkflowyNetAPI;
using Xunit;

namespace WorkflowyNetAPI.Tests
{
	public class WFAPI_RealIntegrationTests
	{
		private static WFAPI CreateAPI()
		{
			var key = Environment.GetEnvironmentVariable("WORKFLOWY_APIKEY");

			if(string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException(
                    "Environment variable WORKFLOWY_APIKEY must be set to run REAL integration tests.");

			return new WFAPI(key);
		}

		/// <summary>
		/// REAL end-to-end test that calls Workflowy backend.
		/// </summary>
		[Fact]
		public async Task WFAPI_RealEndToEndFlow()
		{
			var api = CreateAPI();

			// -------------------------------------------------------
			// 1. CREATE NODE
			// -------------------------------------------------------
			var testNodeId = await api.CreateAsync(
				parentNodeId: null,
				name: "🧪 C# Integration Test Node",
				note: "Created during automated integration test",
				layoutMode: "default",
				position: WFAPI.EPosition.TOP
			);

			testNodeId.Should().NotBeNullOrWhiteSpace();

			// -------------------------------------------------------
			// 2. FETCH NODE
			// -------------------------------------------------------
			var node = await api.GetNodeAsync(testNodeId);
			node.Id.Should().Be(testNodeId);

			// -------------------------------------------------------
			// 3. UPDATE NODE
			// -------------------------------------------------------
			await api.UpdateNodeAsync(new WFNodeUpdate
			{
				Id = testNodeId,
				Name = "🧠 C# Integration Test Node (Updated)"
			});

			// Verify rename
			var updatedNode = await api.GetNodeAsync(testNodeId);
			updatedNode.Name.Should().Contain("(Updated)");

			// -------------------------------------------------------
			// 4. COMPLETE
			// -------------------------------------------------------
			await api.CompleteAsync(testNodeId);

			// Quick check: complete flag should be true
			var completedNode = await api.GetNodeAsync(testNodeId);
			completedNode.Completed.Should().BeTrue();

			// -------------------------------------------------------
			// 5. UNCOMPLETE
			// -------------------------------------------------------
			await api.UncompleteAsync(testNodeId);

			var uncompletedNode = await api.GetNodeAsync(testNodeId);
			uncompletedNode.Completed.Should().BeFalse();

			// -------------------------------------------------------
			// 6. FETCH ROOT NODES (siblings)
			// -------------------------------------------------------
			var siblings = await api.GetNodesAsync();
			siblings.Should().NotBeNull();

			// -------------------------------------------------------
			// 7. CREATE PARENT NODE
			// -------------------------------------------------------
			var parentId = await api.CreateAsync(
				parentNodeId: null,
				name: "🧪 C# Integration Test Parent Node",
				note: null,
				layoutMode: "default",
				position: WFAPI.EPosition.BOTTOM
			);

			parentId.Should().NotBeNullOrWhiteSpace();

			// TODO... 
			var list = await api.ExportAllNodesAsync();

			// -------------------------------------------------------
			// 8. MOVE NODE under parent
			// -------------------------------------------------------
			await api.MoveAsync(testNodeId, parentId, WFAPI.EPosition.BOTTOM);

			// Validate move
			var movedNode = await api.GetNodeAsync(testNodeId);
			Debug.Assert(false);
			//movedNode.ParentId.Should().Be(parentId);

			// -------------------------------------------------------
			// 9. DELETE CHILD
			// -------------------------------------------------------
			await api.DeleteAsync(testNodeId);

			Func<Task> fetchDeleted = async () => await api.GetNodeAsync(testNodeId);
			await fetchDeleted.Should().ThrowAsync<WFAPIException>();

			// -------------------------------------------------------
			// 10. DELETE PARENT
			// -------------------------------------------------------
			await api.DeleteAsync(parentId);

			Func<Task> fetchParent = async () => await api.GetNodeAsync(parentId);
			await fetchParent.Should().ThrowAsync<WFAPIException>();
		}
	}
}
