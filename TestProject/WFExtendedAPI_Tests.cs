using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using WorkflowyNetAPI.DTOs;

namespace WorkflowyNetAPI.Tests
{
	public class WFExtendedAPI_Tests
	{
		private class TestableWFExtendedAPI : WFExtendedAPI
		{
			private readonly WFNodesResponse _resp;
			public TestableWFExtendedAPI(WFNodesResponse resp) : base("dummy") => _resp = resp;

			public override Task<WFNodesResponse> ExportAllNodesAsync() => Task.FromResult(_resp);
		}

		[Fact]
		public async Task GetAllNodesAsTreeAsync_ReconstructsTreeAndOrdersChildren()
		{
			// Arrange: root r1 with two children c1 (priority 200) and c2 (priority 150),
			// and an orphan node (parent missing) o1 which should be treated as root.
			// Use the same CreatedAt for children so ordering is driven only by Priority.
			var createdBase = DateTime.UtcNow;

			var r1Id = Guid.NewGuid();
			var c1Id = Guid.NewGuid();
			var c2Id = Guid.NewGuid();
			var o1Id = Guid.NewGuid();
			var missingParent = Guid.NewGuid();

			var nodes = new[]
			{
				new WFNode { Id = r1Id, Name = "Root 1", ParentId = null, Priority = 100, CreatedAt = createdBase.AddSeconds(0) },
				new WFNode { Id = c1Id, Name = "Child 1", ParentId = r1Id, Priority = 200, CreatedAt = createdBase.AddSeconds(5) },
				new WFNode { Id = c2Id, Name = "Child 2", ParentId = r1Id, Priority = 150, CreatedAt = createdBase.AddSeconds(5) },
				new WFNode { Id = o1Id, Name = "Orphan", ParentId = missingParent, Priority = 50, CreatedAt = createdBase.AddSeconds(20) }
			};

			var resp = new WFNodesResponse { Nodes = nodes };

			var api = new TestableWFExtendedAPI(resp);

			// Act
			var tree = await api.GetNodesTreeAsync();

			// Assert
			tree.Should().NotBeNull();
			tree.RootNodes.Should().HaveCount(2); // r1 and orphan treated as root

			var root1 = tree.RootNodes.Single(r => r.Node.Id == r1Id);
			root1.Children.Should().HaveCount(2);

			// children should be ordered by priority ascending (150 then 200)
			root1.Children[0].Node.Id.Should().Be("c2");
			root1.Children[1].Node.Id.Should().Be("c1");

			// orphan node should be present as root
			tree.RootNodes.Should().Contain(r => r.Node.Id == o1Id);
		}
	}
}