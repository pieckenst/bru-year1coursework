// MongoDB initialization script for development environment
db = db.getSiblingDB('ticketsales');

// Create collections for the application
db.createCollection('logs');
db.createCollection('analytics');
db.createCollection('exports');
db.createCollection('notifications');

// Create indexes for better performance
db.logs.createIndex({ "timestamp": 1 });
db.logs.createIndex({ "level": 1 });
db.logs.createIndex({ "correlationId": 1 });
db.logs.createIndex({ "userId": 1 });

db.analytics.createIndex({ "timestamp": 1 });
db.analytics.createIndex({ "eventType": 1 });
db.analytics.createIndex({ "userId": 1 });

db.exports.createIndex({ "userId": 1 });
db.exports.createIndex({ "createdAt": 1 });
db.exports.createIndex({ "status": 1 });

db.notifications.createIndex({ "userId": 1 });
db.notifications.createIndex({ "createdAt": 1 });
db.notifications.createIndex({ "read": 1 });

// Create a development user with read/write access
db.createUser({
  user: "ticketsales_dev",
  pwd: "devpassword",
  roles: [
    {
      role: "readWrite",
      db: "ticketsales"
    }
  ]
});

print("MongoDB initialization completed for TicketSales development environment");