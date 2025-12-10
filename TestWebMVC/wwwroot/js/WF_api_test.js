// === WFAPI Test Scenario =====================================
// This script tests your entire API flow against the new standardized backend.
// Run it in the browser console or inside your frontend JS after loading wfapi.js.
// =============================================================

import {
    WF_createNode,
    WF_fetchNode,
    WF_fetchNodes,
    WF_updateNodeName,
    WF_completeNode,
    WF_uncompleteNode,
    WF_deleteNode
} from './WF_api.js';

async function runWFAPITest() {
    console.log("=== WFAPI Integration Test ===");

    try {
        // Create a new node
        console.log("Creating new node...");
        const newNode = await WF_createNode({
            parentitem_id: null, // root
            name: "🧪 Test Node",
            note: "Created during automated test",
            layoutMode: "default",
            position: "bottom"
        });
        console.log("✅ Node created:", newNode);

        const item_id = newNode?.item_id || null;
        debugger;
        if(!item_id) throw new Error("Node ID not returned in creation response!");

        // Fetch node info
        console.log("Fetching node info...");
        const node = await WF_fetchNode(item_id);
        console.log("✅ Node fetched:", node);

        // Update node name
        console.log("Updating node name...");
        const updated = await WF_updateNodeName(item_id, "🧠 Test Node (Renamed)");
        console.log("✅ Node renamed:", updated);

        // Mark node as completed
        console.log("Completing node...");
        const completed = await WF_completeNode(item_id);
        console.log("✅ Node completed:", completed);

        // Uncomplete the node
        console.log("Uncompleting node...");
        const uncompleted = await WF_uncompleteNode(item_id);
        console.log("✅ Node uncompleted:", uncompleted);

        // List nodes under the same parent (optional)
        console.log("Fetching sibling nodes...");
        const siblings = await WF_fetchNodes(null);
        console.log("✅ Sibling nodes:", siblings);

        // Delete the node
        console.log("Deleting node...");
        const deleted = await WF_deleteNode(item_id);
        console.log("✅ Node deleted:", deleted);

        console.log("🎉 All API tests passed successfully!");

    } catch(error) {
        console.error("❌ Test failed:", error);
        alert("WFAPI test failed: " + error.message);
    }
}

// Run automatically after DOM loads (if in browser)
if(typeof window !== "undefined") {
    window.addEventListener("load", () => {
        runWFAPITest();
    });
}

// Or call manually:
// runWFAPITest();
