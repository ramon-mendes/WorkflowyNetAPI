using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using WorkflowyNetAPI;
using Xunit;

namespace WorkflowyNetAPI.Tests
{
	public class WFAPITests
	{
		private static HttpClient CreateMockedHttpClient(
			Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
		{
			var handler = new Mock<HttpMessageHandler>();

			handler
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>()
				)
				.ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
				{
					return handlerFunc(req);
				});

			return new HttpClient(handler.Object)
			{
				BaseAddress = new Uri("https://workflowy.com/api/v1/")
			};
		}

		private WFAPI CreateApiWithMock(Func<HttpRequestMessage, HttpResponseMessage> handler)
		{
			var client = CreateMockedHttpClient(handler);

			var api = new WFAPI("test_key");

			// replace internal HttpClient via reflection
			typeof(WFAPI)
				.GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.SetValue(api, client);

			return api;
		}

		[Fact]
		public async Task Full_API_Flow_Matches_JS_Test_Scenario()
		{
			// === PREPARED RESPONSES IN ORDER OF JS SCENARIO ===
			var queue = new Queue<HttpResponseMessage>();

			// 1. Create node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"item_id\": \"id_1\"}", Encoding.UTF8, "application/json")
			});

			// 2. Fetch node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"node\": {\"id\": \"id_1\", \"name\": \"🧪 Test Node\"}}", Encoding.UTF8, "application/json")
			});

			// 3. Update node (status ok)
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// 4. Complete
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// 5. Uncomplete
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// 6. Fetch nodes list
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"nodes\": []}", Encoding.UTF8, "application/json")
			});

			// 7. Create parent node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"item_id\": \"parent_1\"}", Encoding.UTF8, "application/json")
			});

			// 8. Move node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// 9. Delete node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// 10. Delete parent node
			queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

			// === MOCK REQUEST HANDLER ===
			var api = CreateApiWithMock(req =>
			{
				if(queue.Count == 0)
					throw new InvalidOperationException("No more responses queued");

				return queue.Dequeue();
			});

			// === RUN THE FLOW ===

			// Create node
			var newId = await api.CreateAsync(null, "🧪 Test Node", "note", "default", WFAPI.EPosition.BOTTOM);
			Assert.Equal("id_1", newId);

			// Fetch node
			var node = await api.GetNodeAsync("id_1");
			Assert.Equal("id_1", node.Id);
			Assert.Equal("🧪 Test Node", node.Name);

			// Update node
			await api.UpdateNodeAsync(new WFNodeUpdate { Id = "id_1", Name = "Renamed" });

			// Complete
			await api.CompleteAsync("id_1");

			// Uncomplete
			await api.UncompleteAsync("id_1");

			// Fetch siblings
			var siblings = await api.GetNodesAsync();
			Assert.Empty(siblings);

			// Create parent
			var parentId = await api.CreateAsync(null, "🧪 Parent Node", null, "default", WFAPI.EPosition.BOTTOM);
			Assert.Equal("parent_1", parentId);

			// Move node
			await api.MoveAsync("id_1", parentId, WFAPI.EPosition.BOTTOM);

			// Delete child
			await api.DeleteAsync("id_1");

			// Delete parent
			await api.DeleteAsync(parentId);

			Assert.Empty(queue);
		}
	}
}
