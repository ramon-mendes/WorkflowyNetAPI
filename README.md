[![.NET Unit Tests](https://github.com/ramon-mendes/WorkflowyNetAPI/actions/workflows/test.yml/badge.svg)](https://github.com/ramon-mendes/WorkflowyNetAPI/actions/workflows/test.yml)

A complete C#.NET lib for Workflowy API, also with .js/MVC Controller for your backend/frontend integration needs.

.NET Core 8 and onwards.

---

WorkFlowy API docs: https://beta.workflowy.com/api-reference/

Grab your api key at: https://workflowy.com/api-key

NuGet: https://www.nuget.org/packages/WorkflowyNetAPI/

---

ToDo's:

- [ ] JS API should have a C# controller/backend that just does the proxy call to WF API, so the REST call signature is in the frontend, but the actual call is done in the backend (to avoid CORS issues and exposing API key in frontend).
  - [ ] Ask AI if there a lib for that

- [ ] WFAPI class: implement a method to get all items recursively (get all children of a given item id, and their children, etc)
  - [ ] Method to export in various formats (txt, json, xml, etc)
