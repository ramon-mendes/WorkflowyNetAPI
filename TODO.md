
- [ ] Test: how mirrors are shown?

- Utilities
  - [ ] WFAPI class: implement a method to get all items recursively (get all children of a given item id, and their children, etc)
  - [ ] Method to export in various formats (txt, json, xml, etc)


- [ ] Add a secure a C# controller/backend for the JS API that just does the proxy call to WF API, so the REST call signature is in the frontend, but the actual call is done in the backend (to avoid CORS issues and exposing API key in frontend).
  - [ ] Ask AI if there a lib for that
  - [ ] Keep the normal controller for Swagger docs and direct backend calls
