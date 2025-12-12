// === WFAPI Helper =======================================
// Assumes server always returns the canonical ProblemDetails-shaped envelope:
// { type?, title, status, data?, errors?, traceId }
// Successful responses return data; errors throw with the first validation message (if any).
// =========================================================

async function wfapiRequest(endpoint, options = {}) {
    const url = `/WFAPI${endpoint}`;
    const config = {
        headers: { "Content-Type": "application/json" },
        ...options
    };

    const response = await fetch(url, config);

    // Parse JSON body (throw generic if no JSON and non-OK)
    let body;
    try {
        body = await response.json();
    } catch(e) {
        if(!response.ok) throw new Error(`HTTP ${response.status}`);
        return null;
    }

    // Treat body as canonical envelope
    const status = body.status ?? response.status;

    if(status >= 200 && status < 300) {
        return body.data;
    }

    // Non-2xx -> prefer validation error messages if present
    const errors = body.errors || {};
    const firstField = Object.keys(errors)[0];
    const firstMsg = firstField && Array.isArray(errors[firstField]) && errors[firstField][0];
    const message = firstMsg || body.detail || body.title || `HTTP ${status}`;
    const err = new Error(message);
    err.details = body;
    throw err;
}

// === Individual endpoint functions ======================

export async function WF_createNode({ parentid, name, note = "", layoutMode = "default", position = "last" }) {
    return wfapiRequest(`/node`, {
        method: "POST",
        body: JSON.stringify({ parentid, name, note, layoutMode, position })
    });
}

export async function WF_updateNode(item_id, name, note, layoutMode) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}`, {
        method: "POST",
        body: JSON.stringify({ name, note, layoutMode })
    });
}

export async function WF_fetchNode(item_id) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}`);
}

export async function WF_fetchNodes(parentId = null) {
    const query = parentId ? `?parentId=${encodeURIComponent(parentId)}` : "";
    return wfapiRequest(`/nodes${query}`);
}

export async function WF_deleteNode(item_id) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}`, { method: "DELETE" });
}

export async function WF_completeNode(item_id) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}/complete`, { method: "POST" });
}

export async function WF_uncompleteNode(item_id) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}/uncomplete`, { method: "POST" });
}

export async function WF_moveNode(item_id, parent_item_id, position) {
    return wfapiRequest(`/node/${encodeURIComponent(item_id)}/move`, {
        method: "POST",
        body: JSON.stringify({ parent_item_id, position })
    });
}
