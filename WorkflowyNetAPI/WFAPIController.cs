using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowyNetAPI
{
	public class NodeCreateRequest
	{
		public string? Parentitem_id { get; set; }

		[Required(ErrorMessage = "The Name field is required.")]
		public string Name { get; set; }

		public string? Note { get; set; }
		public string? LayoutMode { get; set; }
		public string? Position { get; set; }
	}

	public class NodeUpdateRequest
	{
		[Required(ErrorMessage = "The Name field is required.")]
		public string Name { get; set; }
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

		// Helper: build a consistent ProblemDetails-shaped envelope for success
		private ActionResult EnvelopeOk(object? data)
		{
			var pd = new ProblemDetails
			{
				Type = null,
				Title = "OK",
				Status = StatusCodes.Status200OK
			};
			pd.Extensions["data"] = data;
			pd.Extensions["errors"] = new { };
			pd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
			return Ok(pd);
		}

		// Helper: build a consistent ProblemDetails-shaped envelope for errors
		private ObjectResult EnvelopeProblem(string title, int status, object? errors = null, string? detail = null)
		{
			var pd = new ProblemDetails
			{
				Type = "about:blank",
				Title = title,
				Status = status,
				Detail = detail
			};
			pd.Extensions["data"] = null;
			pd.Extensions["errors"] = errors ?? new { };
			pd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
			return StatusCode(status, pd);
		}

		// GET /WFAPI/node/{id}
		[HttpGet("node/{id}")]
		public async Task<IActionResult> GetNode(string id)
		{
			try
			{
				var node = await _wfClient.GetNodeAsync(id);
				return EnvelopeOk(node);
			}
			catch(WFAPIException wfex)
			{
				// propagate original error content from WFAPI
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while fetching the node.", 500, null, ex.Message);
			}
		}

		// GET /WFAPI/nodes?parentId=xxx
		[HttpGet("nodes")]
		public async Task<IActionResult> GetNodes([FromQuery] string? parentId = null)
		{
			try
			{
				var nodes = await _wfClient.GetNodesAsync(parentId);
				return EnvelopeOk(nodes);
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while fetching nodes.", 500, null, ex.Message);
			}
		}

		// POST /WFAPI/node/{id}
		[HttpPost("node/{id}")]
		public async Task<IActionResult> UpdateNodeName(string id, [FromBody] NodeUpdateRequest request)
		{
			if(!ModelState.IsValid)
			{
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};
				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
				return BadRequest(vpd);
			}

			try
			{
				var updatedNode = await _wfClient.UpdateNodeNameAsync(id, request.Name);
				return EnvelopeOk(updatedNode);
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while updating the node.", 500, null, ex.Message);
			}
		}

		/// <summary>
		/// Create a new node
		/// POST /WFAPI/node
		/// </summary>
		[HttpPost("node")]
		public async Task<IActionResult> CreateNode([FromBody] NodeCreateRequest request)
		{
			if(!ModelState.IsValid)
			{
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};
				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
				return BadRequest(vpd);
			}

			try
			{
				var response = await _wfClient.CreateAsync(
					request.Parentitem_id,
					request.Name,
					request.Note,
					request.LayoutMode,
					request.Position
				);

				return EnvelopeOk(response);
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while creating the node.", 500, null, ex.Message);
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
			{
				ModelState.AddModelError(nameof(id), "Node ID is required.");
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};
				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
				return BadRequest(vpd);
			}

			try
			{
				await _wfClient.DeleteAsync(id);
				return EnvelopeOk(new { status = "ok" });
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while deleting the node.", 500, null, ex.Message);
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
			{
				ModelState.AddModelError(nameof(id), "Node ID is required.");
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};
				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
				return BadRequest(vpd);
			}

			try
			{
				await _wfClient.CompleteAsync(id);
				return EnvelopeOk(new { status = "ok" });
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while completing the node.", 500, null, ex.Message);
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
			{
				ModelState.AddModelError(nameof(id), "Node ID is required.");
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};
				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
				return BadRequest(vpd);
			}

			try
			{
				await _wfClient.UncompleteAsync(id);
				return EnvelopeOk(new { status = "ok" });
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error", wfex.StatusCode > 0 ? wfex.StatusCode : 500, wfex.Error, wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An error occurred while uncompleting the node.", 500, null, ex.Message);
			}
		}
	}
}