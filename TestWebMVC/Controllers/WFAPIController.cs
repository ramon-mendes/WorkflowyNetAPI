using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using WorkflowyNetAPI;

namespace TestWebMVC.Controllers
{
	public class NodeCreateRequest
	{
		public string ParentNodeId { get; set; }
		public string Name { get; set; }
		public string Note { get; set; }
		public string LayoutMode { get; set; }
		public string Position { get; set; }
	}

	public class NodeUpdateRequest
	{
		public string Name { get; set; }
	}

	public class ApiResponse<T>
	{
		public bool Success { get; set; }
		public T Data { get; set; }
		public string Error { get; set; }

		public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
		public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
	}

	[Route("WFAPI")]
	[ApiController]
	public class WFAPIController : ControllerBase
	{
		private readonly WFAPI _wfClient;

		public WFAPIController()
		{
			var api_key = Environment.GetEnvironmentVariable("workflowy_apikey");
			if(api_key == null)
				throw new Exception("Environment variable 'workflowy_apikey' is not set.");

			_wfClient = new WFAPI(api_key);
		}

		// GET /WFAPI/node/{id}
		[HttpGet("node/{id}")]
		public async Task<IActionResult> GetNode(string id)
		{
			try
			{
				var node = await _wfClient.GetNodeAsync(id);
				return Ok(ApiResponse<WFNode>.Ok(node));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		// GET /WFAPI/nodes?parentId=xxx
		[HttpGet("nodes")]
		public async Task<IActionResult> GetNodes([FromQuery] string parentId = null)
		{
			try
			{
				var nodes = await _wfClient.GetNodesAsync(parentId);
				return Ok(ApiResponse<WFNode[]>.Ok(nodes));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		// POST /WFAPI/node/{id}
		[HttpPost("node/{id}")]
		public async Task<IActionResult> UpdateNodeName(string id, [FromBody] NodeUpdateRequest request)
		{
			if(request == null || string.IsNullOrWhiteSpace(request.Name))
				return BadRequest(ApiResponse<object>.Fail("Name is required."));

			try
			{
				var updatedNode = await _wfClient.UpdateNodeNameAsync(id, request.Name);
				return Ok(ApiResponse<WFNode>.Ok(updatedNode));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		/// <summary>
		/// Create a new node
		/// POST /WFAPI/node
		/// </summary>
		[HttpPost("node")]
		public async Task<IActionResult> CreateNode([FromBody] NodeCreateRequest request)
		{
			if(request == null || string.IsNullOrWhiteSpace(request.Name))
				return BadRequest(ApiResponse<object>.Fail("Name is required."));

			try
			{
				var response = await _wfClient.CreateAsync(
					request.ParentNodeId,
					request.Name,
					request.Note,
					request.LayoutMode,
					request.Position
				);

				return Ok(ApiResponse<string>.Ok(response));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		/// <summary>
		/// Delete a node permanently
		/// DELETE /WFAPI/node/{id}
		/// </summary>
		[HttpDelete("node/{id}")]
		public async Task<IActionResult> DeleteNode(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return BadRequest(ApiResponse<object>.Fail("Node ID is required."));

			try
			{
				bool success = await _wfClient.DeleteAsync(id);
				if(success)
					return Ok(ApiResponse<object>.Ok(new { status = "ok" }));
				else
					return StatusCode(500, ApiResponse<object>.Fail("Unknown response from Workflowy API."));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		/// <summary>
		/// Mark a node as completed
		/// POST /WFAPI/node/{id}/complete
		/// </summary>
		[HttpPost("node/{id}/complete")]
		public async Task<IActionResult> CompleteNode(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return BadRequest(ApiResponse<object>.Fail("Node ID is required."));

			try
			{
				bool success = await _wfClient.CompleteAsync(id);
				if(success)
					return Ok(ApiResponse<object>.Ok(new { status = "ok" }));
				else
					return StatusCode(500, ApiResponse<object>.Fail("Unknown response from Workflowy API."));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}

		/// <summary>
		/// Mark a node as uncompleted
		/// POST /WFAPI/node/{id}/uncomplete
		/// </summary>
		[HttpPost("node/{id}/uncomplete")]
		public async Task<IActionResult> UncompleteNode(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return BadRequest(ApiResponse<object>.Fail("Node ID is required."));

			try
			{
				bool success = await _wfClient.UncompleteAsync(id);
				if(success)
					return Ok(ApiResponse<object>.Ok(new { status = "ok" }));
				else
					return StatusCode(500, ApiResponse<object>.Fail("Unknown response from Workflowy API."));
			}
			catch(Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
			}
		}
	}
}
