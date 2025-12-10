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
export async function WF_fetchNode(item_id) {
    return wfapiRequest(`/node/${item_id}`);
}

// GET /WFAPI/nodes?parentId=xxx
export async function WF_fetchNodes(parentId = null) {
    const query = parentId ? `?parentId=${encodeURIComponent(parentId)}` : "";
    return wfapiRequest(`/nodes${query}`);
}

// POST /WFAPI/node/{id}
export async function WF_updateNodeName(item_id, name) {
    return wfapiRequest(`/node/${item_id}`, {
        method: "POST",
        body: JSON.stringify({ name })
    });
}

// POST /WFAPI/node
export async function WF_createNode({ parentitem_id, name, note = "", layoutMode = "default", position = "last" }) {
    return wfapiRequest(`/node`, {
        method: "POST",
        body: JSON.stringify({ parentitem_id, name, note, layoutMode, position })
    });
}

// DELETE /WFAPI/node/{id}
export async function WF_deleteNode(item_id) {
    return wfapiRequest(`/node/${item_id}`, { method: "DELETE" });
}

// POST /WFAPI/node/{id}/complete
export async function WF_completeNode(item_id) {
    return wfapiRequest(`/node/${item_id}/complete`, { method: "POST" });
}

// POST /WFAPI/node/{id}/uncomplete
export async function WF_uncompleteNode(item_id) {
    return wfapiRequest(`/node/${item_id}/uncomplete`, { method: "POST" });
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
