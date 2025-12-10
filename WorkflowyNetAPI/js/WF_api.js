// === WFAPI Helper =======================================
// Unified API wrapper that matches the standardized backend envelope
// All responses have the format:
// { success: true/false, data: ..., error: ... }
// =========================================================

async function wfapiRequest(endpoint, options = {}) {
    const url = `/WFAPI${endpoint}`;
    const config = {
        headers: { "Content-Type": "application/json" },
        ...options
    };

    try {
        const response = await fetch(url, config);
        const result = await response.json();

        if(!response.ok || !result.success) {
            const message = result.error || `HTTP ${response.status}`;
            throw new Error(message);
        }

        return result.data;
    } catch(error) {
        console.error(`WFAPI error (${endpoint}):`, error);
        throw error;
    }
}

// === Individual endpoint functions ======================

// GET /WFAPI/node/{id}
export async function WF_fetchNode(nodeId) {
    return wfapiRequest(`/node/${nodeId}`);
}

// GET /WFAPI/nodes?parentId=xxx
export async function WF_fetchNodes(parentId = null) {
    const query = parentId ? `?parentId=${encodeURIComponent(parentId)}` : "";
    return wfapiRequest(`/nodes${query}`);
}

// POST /WFAPI/node/{id}
export async function WF_updateNodeName(nodeId, name) {
    return wfapiRequest(`/node/${nodeId}`, {
        method: "POST",
        body: JSON.stringify({ name })
    });
}

// POST /WFAPI/node
export async function WF_createNode({ parentNodeId, name, note = "", layoutMode = "default", position = "last" }) {
    return wfapiRequest(`/node`, {
        method: "POST",
        body: JSON.stringify({ parentNodeId, name, note, layoutMode, position })
    });
}

// DELETE /WFAPI/node/{id}
export async function WF_deleteNode(nodeId) {
    return wfapiRequest(`/node/${nodeId}`, { method: "DELETE" });
}

// POST /WFAPI/node/{id}/complete
export async function WF_completeNode(nodeId) {
    return wfapiRequest(`/node/${nodeId}/complete`, { method: "POST" });
}

// POST /WFAPI/node/{id}/uncomplete
export async function WF_uncompleteNode(nodeId) {
    return wfapiRequest(`/node/${nodeId}/uncomplete`, { method: "POST" });
}

// =========================================================
// Example usage:
// try {
//     const node = await WF_fetchNode("abc123");
//     console.log("Node:", node);
// } catch (err) {
//     alert("Error: " + err.message);
// }
// =========================================================
