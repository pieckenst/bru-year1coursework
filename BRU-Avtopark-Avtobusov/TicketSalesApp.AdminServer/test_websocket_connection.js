// Simple JavaScript test to verify SignalR WebSocket connection
// This can be run in a browser console or with Node.js

const testWebSocketConnection = async () => {
    try {
        console.log('Testing SignalR WebSocket connection...');
        
        // Create SignalR connection
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/hubs/notifications", {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .build();

        // Set up event handlers
        connection.on("Notification", (notification) => {
            console.log("Received notification:", notification);
        });

        connection.on("DataChange", (change) => {
            console.log("Received data change:", change);
        });

        connection.on("ExportProgress", (progress) => {
            console.log("Received export progress:", progress);
        });

        // Connect
        await connection.start();
        console.log("✅ SignalR connection established successfully!");

        // Test sending a message (this would normally require authentication)
        try {
            await connection.invoke("JoinGroup", "TestGroup");
            console.log("✅ Successfully joined test group");
        } catch (error) {
            console.log("⚠️ Could not join group (authentication required):", error.message);
        }

        // Clean up
        await connection.stop();
        console.log("✅ Connection closed successfully");
        
        return true;
    } catch (error) {
        console.error("❌ WebSocket connection test failed:", error);
        return false;
    }
};

// For browser usage
if (typeof window !== 'undefined') {
    window.testWebSocketConnection = testWebSocketConnection;
    console.log('WebSocket test function loaded. Run testWebSocketConnection() to test.');
}

// For Node.js usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { testWebSocketConnection };
}