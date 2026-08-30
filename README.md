[![.NET Unit Tests](https://github.com/ramon-mendes/WorkflowyNetAPI/actions/workflows/test.yml/badge.svg)](https://github.com/ramon-mendes/WorkflowyNetAPI/actions/workflows/test.yml)

A complete C#.NET lib for Workflowy API, also for your frotend needs, a JS lib with the correspoing C# MVC controller

.NET Core 8 and onwards.

---

WorkFlowy API docs: https://beta.workflowy.com/api-reference/

Grab your api key at: https://workflowy.com/api-key

NuGet: https://www.nuget.org/packages/WorkflowyNetAPI/

---

## Usage

### Choosing the environment

```csharp
// Production (default) -> https://workflowy.com/api/v1/
var api = new WFExtendedAPI(apiKey);

// Beta -> https://beta.workflowy.com/api/v1/ (required by the mirror endpoints)
var beta = new WFExtendedAPI(apiKey, WFEnvironment.Beta);

// Custom host (proxy / staging). Must point at the '/api/v1/' root.
var custom = new WFExtendedAPI(apiKey, "https://my-proxy.example.com/api/v1/");
```

`WFAPI` exposes the same three constructors, plus a read-only `BaseUrl` property.

### Mirrors

```csharp
var beta = new WFExtendedAPI(apiKey, WFEnvironment.Beta);

// Create a mirror of nodeId under destinationId
var mirror = await beta.CreateMirrorAsync(nodeId, NodeIdentifier.Guid(destinationId));
Console.WriteLine($"{mirror.MirrorId} mirrors {mirror.OriginId}");

// Identify mirrors on any fetched node
var node = await beta.GetNodeAsync(mirror.MirrorId);
bool isMirror       = node.IsMirror;        // node.Data.Mirror.OriginId  is set
bool isMirrorOrigin = node.IsMirrorOrigin;  // node.Data.Mirror.MirrorIds is set

// Remove the mirror root - pass the MIRROR's id, the origin stays intact
await beta.DeleteMirrorAsync(mirror.MirrorId);
```

Note: deleting a node fails while that node (or any node in its subtree) is a mirror origin with live mirrors. Delete the mirror roots first.

### Environment variables

| Variable | Used by | Values |
| --- | --- | --- |
| `workflowy_apikey` | `TestWebMVC` | your API key |
| `workflowy_environment` | `TestWebMVC` | `Production` (default) or `Beta` |
| `WORKFLOWY_APIKEY_TEST` | `TestProject` E2E tests | your API key |
| `WORKFLOWY_ENV_TEST` | `TestProject` E2E tests | `Production` (default) or `Beta` |

---

Comercial SUPPORT:

This project is open source under GPL-3.0 License. If you need commercial support under a different license, reach me out [for inquiries](mailto:rfmendesbrazil@gmail.com)
