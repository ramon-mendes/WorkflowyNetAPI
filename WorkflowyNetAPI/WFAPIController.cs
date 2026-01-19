using System;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowyNetAPI
{
	// ------------------------------------------------------
	// DTOs — using RECORDS instead of classes
	// ------------------------------------------------------

	public record NodeCreateRequest(
		string? ParentId,

		// Validation must be on constructor parameter (not on property)
		[Required(ErrorMessage = "The Name field is required.")]
		string Name,

		string? Note,
		string? LayoutMode,
		string? Position
	);

	public record NodeUpdateRequest(
		string? Name,
		string? Note,
		string? LayoutMode
	);

	public record NodeMoveRequest(
		// JsonPropertyName must stay on the property, not the parameter
		[property: JsonPropertyName("parent_item_id")]
		string ParentItemId,
		string? Position
	);

	// ------------------------------------------------------
	// Controller
	// ------------------------------------------------------

	[Route("WFAPI")]
	[ApiController]
	public class WFAPIController : ControllerBase
	{
		private readonly WFExtendedAPI _wfClient;

		public WFAPIController()
		{
			var api_key = Environment.GetEnvironmentVariable("workflowy_apikey");
			if(api_key == null)
				throw new Exception("Environment variable 'workflowy_apikey' is not set.");

			_wfClient = new WFExtendedAPI(api_key);
		}

		// ------------------------------------------------------
		// Envelope Helpers
		// ------------------------------------------------------

		private ActionResult EnvelopeOk(object? data = null)
		{
			var pd = new ProblemDetails
			{
				Title = "OK",
				Status = StatusCodes.Status200OK
			};

			pd.Extensions["errors"] = new { };
			pd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

			if(data != null)
				pd.Extensions["data"] = data;

			return Ok(pd);
		}

		private ObjectResult EnvelopeProblem(
			string title,
			int status,
			object? errors = null,
			string? detail = null)
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

		// ------------------------------------------------------
		// Helpers
		// ------------------------------------------------------

		private IActionResult ValidateId(string id)
		{
			if(!string.IsNullOrWhiteSpace(id))
				return null!;

			ModelState.AddModelError(nameof(id), "Node ID is required.");
			return ValidateModel();
		}

		private IActionResult ValidateModel()
		{
			if(!ModelState.IsValid)
			{
				var vpd = new ValidationProblemDetails(ModelState)
				{
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest
				};

				vpd.Extensions["data"] = null;
				vpd.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

				return BadRequest(vpd);
			}

			return null!;
		}

		private async Task<IActionResult> Try(Func<Task<IActionResult>> action)
		{
			try
			{
				return await action();
			}
			catch(WFAPIException wfex)
			{
				return EnvelopeProblem("Workflowy API error",
					wfex.StatusCode > 0 ? wfex.StatusCode : 500,
					wfex.Error,
					wfex.Message);
			}
			catch(Exception ex)
			{
				return EnvelopeProblem("An unexpected error occurred.", 500, null, ex.Message);
			}
		}

		// ------------------------------------------------------
		// ENDPOINTS
		// ------------------------------------------------------

		// GET /WFAPI/node/{id}
		[HttpGet("node/{id}")]
		public Task<IActionResult> GetNode(string id) => Try(async () =>
		{
			var err = ValidateId(id);
			if(err != null) return err;

			var node = await _wfClient.GetNodeAsync(id);
			return EnvelopeOk(node);
		});

		// GET /WFAPI/nodes?parentId=...
		[HttpGet("nodes")]
		public Task<IActionResult> GetNodes([FromQuery] string? parentId = null) => Try(async () =>
		{
			var nodes = await _wfClient.GetNodesAsync(parentId);
			return EnvelopeOk(nodes);
		});

		// POST /WFAPI/node/{id}
		[HttpPost("node/{id}")]
		public Task<IActionResult> UpdateNode(string id, [FromBody] NodeUpdateRequest request) => Try(async () =>
		{
			var err = ValidateId(id) ?? ValidateModel();
			if(err != null) return err;

			await _wfClient.UpdateNodeAsync(new WFNodeUpdate
			{
				Id = id,
				Name = request.Name,
				Note = request.Note,
				LayoutMode = request.LayoutMode
			});

			return EnvelopeOk();
		});

		// POST /WFAPI/node
		[HttpPost("node")]
		public Task<IActionResult> CreateNode([FromBody] NodeCreateRequest request) => Try(async () =>
		{
			var err = ValidateModel();
			if(err != null) return err;

			Enum.TryParse<WFAPI.EPosition>(request.Position, true, out var position);

			var created = await _wfClient.CreateAsync(
				request.ParentId,
				request.Name,
				request.Note,
				request.LayoutMode,
				position);

			return EnvelopeOk(created);
		});

		// DELETE /WFAPI/node/{id}
		[HttpDelete("node/{id}")]
		public Task<IActionResult> DeleteNode(string id) => Try(async () =>
		{
			var err = ValidateId(id);
			if(err != null) return err;

			await _wfClient.DeleteAsync(id);
			return EnvelopeOk();
		});

		// POST /WFAPI/node/{id}/complete
		[HttpPost("node/{id}/complete")]
		public Task<IActionResult> CompleteNode(string id) => Try(async () =>
		{
			var err = ValidateId(id);
			if(err != null) return err;

			await _wfClient.CompleteAsync(id);
			return EnvelopeOk();
		});

		// POST /WFAPI/node/{id}/uncomplete
		[HttpPost("node/{id}/uncomplete")]
		public Task<IActionResult> UncompleteNode(string id) => Try(async () =>
		{
			var err = ValidateId(id);
			if(err != null) return err;

			await _wfClient.UncompleteAsync(id);
			return EnvelopeOk();
		});

		// POST /WFAPI/node/{id}/move
		[HttpPost("node/{id}/move")]
		public Task<IActionResult> MoveNode(string id, [FromBody] NodeMoveRequest request) => Try(async () =>
		{
			var err = ValidateId(id) ?? ValidateModel();
			if(err != null) return err;

			if(!Enum.TryParse<WFAPI.EPosition>(request.Position, true, out var position))
				return EnvelopeProblem($"Invalid position: '{request.Position}'.", 400);

			await _wfClient.MoveAsync(id, request.ParentItemId, position);
			return EnvelopeOk();
		});
	}
}
