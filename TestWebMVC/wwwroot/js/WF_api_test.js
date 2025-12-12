// === WFAPI Test Scenario =====================================
// This script tests your entire API flow against the new standardized backend.
// Run it in the browser console or inside your frontend JS after loading wfapi.js.
// =============================================================

import {
    WF_createNode,
    WF_fetchNode,
    WF_fetchNodes,
    WF_updateNode,
    WF_completeNode,
    WF_uncompleteNode,
    WF_deleteNode,
    WF_moveNode
} from './WF_api.js';

async function runWFAPITest() {
    console.log("=== WFAPI Integration Test ===");

    try {
        // Create a new node
        console.log("Creating new node...");
        const item_id = await WF_createNode({
            parentitem_id: null, // root
            name: "🧪 Test Node",
            note: "Created during automated test",
            layoutMode: "default",
            position: "bottom"
        });
        if(!item_id) throw new Error("Node ID not returned in creation response!");
        console.log("✅ Node created:", item_id);

        // Fetch node info
        console.log("Fetching node info...");
        const node = await WF_fetchNode(item_id);
        console.log("✅ Node fetched:", node);

        // Update node name
        console.log("Updating node...");
        await WF_updateNode(item_id, "🧠 Test Node (Renamed)");
        console.log("✅ Node renamed");

        // Mark node as completed
        console.log("Completing node...");
        await WF_completeNode(item_id);
        console.log("✅ Node completed");

        // Uncomplete the node
        console.log("Uncompleting node...");
        await WF_uncompleteNode(item_id);
        console.log("✅ Node uncompleted");

        // List nodes under the same parent (optional)
        console.log("Fetching sibling nodes...");
        const siblings = await WF_fetchNodes();
        console.log("✅ Sibling nodes:", siblings);

        // Create a new parent node
        console.log("Creating new parent node...");
        const parent_item_id = await WF_createNode({
            parentitem_id: null,
            name: "🧪 Parent Node",
            layoutMode: "default",
            position: "bottom"
        });
        if(!parent_item_id) throw new Error("Node ID not returned in creation response!");
        console.log("✅ Node created:", parent_item_id);

        // Move the node
        console.log("Moving node...");
        await WF_moveNode(item_id, parent_item_id, "bottom");
        console.log("✅ Node moved");

        // Delete the node
        console.log("Deleting node...");
        await WF_deleteNode(item_id);
        console.log("✅ Node deleted");

        // Delete the node
        console.log("Deleting node...");
        await WF_deleteNode(parent_item_id);
        console.log("✅ Node deleted");

        console.log("🎉 All API tests passed successfully!");

    } catch(error) {
        console.error("❌ Test failed:", error);
        alert("WFAPI test failed: " + error.message);
    }
}

// Run automatically after DOM loads
window.addEventListener("load", () => {
    runWFAPITest();
});
