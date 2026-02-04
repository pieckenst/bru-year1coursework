# TicketSales API Server - Development Setup

This document provides comprehensive setup instructions for the modernized TicketSales API Server development environment.

## Prerequisites

### Required Software
- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/downloads)

### Optional Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **Postman** for API testing
- **MongoDB Compass** for MongoDB GUI
- **Redis Insight** for Redis GUI

## Quick Start

### 1. Clone and Setup
```bash
git clone <repository-url>
cd BRU-Avtopark-Avtobusov
```

### 2. Start Infrastructure Services
```bash
# Windows PowerShell
.\scripts\start-dev-environment.ps1

# Linux/macOS
./scripts/start-dev-environment.sh

# Or manually with Docker Compose
docker-compose -f docker-compose.dev.yml up -d
```

### 3. Verify Services
All services should be healthy. Check with:
```bash
docker-compose -f docker-compose.dev.yml ps
```

### 4. Start API Server
```bash
cd TicketSalesApp.AdminServer
dotnet run
```

### 5. Access Services
- **API Server**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger
- **Grafana**: http://localhost:3000 (admin/devpassword)
- **Kibana**: http://localhost:5601
- **Jaeger**: http://localhost:16686

## Infrastructure Services

### Redis (Port 6379)
- **Purpose**: Caching, session state, SignalR backplane
- **Connection**: `localhost:6379` (password: `devpassword`)
- **Databases**:
  - 0: General purpose
  - 1: Application caching
  - 2: User sessions
  - 3: SignalR backplane

### MongoDB (Port 27017)
- **Purpose**: Document storage for logs and analytics
- **Connection**: `mongodb://ticketsales_dev:devpassword@localhost:27017/ticketsales`
- **Collections**: `logs`, `analytics`, `exports`, `notifications`

### Elasticsearch (Port 9200)
- **Purpose**: Log aggregation and search
- **URL**: http://localhost:9200
- **Index Pattern**: `ticketsales-*-logs-*`

### Kibana (Port 5601)
- **Purpose**: Log visualization
- **URL**: http://localhost:5601
- **Setup**: Create index pattern for `ticketsales-*-logs-*`

### Prometheus (Port 9090)
- **Purpose**: Metrics collection
- **URL**: http://localhost:9090
- **Targets**: API server metrics endpoint

### Grafana (Port 3000)
- **Purpose**: Metrics visualization
- **URL**: http://localhost:3000
- **Credentials**: admin/devpassword
- **Dashboards**: Pre-configured for API metrics

### Jaeger (Port 16686)
- **Purpose**: Distributed tracing
- **URL**: http://localhost:16686
- **Integration**: OpenTelemetry traces from API

## Configuration

### Environment-Specific Settings

**Development** (`appsettings.Development.json`):
- SQLite database
- Local infrastructure services
- Debug logging enabled
- Detailed error messages

**Production** (`appsettings.json`):
- SQL Server database
- Production-ready settings
- Structured logging
- Security hardening

### Key Configuration Sections

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ticketsales.db",
    "Redis": "localhost:6379,password=devpassword",
    "MongoDB": "mongodb://ticketsales_dev:devpassword@localhost:27017/ticketsales"
  },
  "OpenTelemetry": {
    "ServiceName": "TicketSales.AdminServer",
    "Jaeger": {
      "Endpoint": "http://localhost:14268/api/traces"
    }
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File" },
      { "Name": "Elasticsearch" }
    ]
  }
}
```

## New Features Added

### 1. WebSocket Support (SignalR)
- Real-time notifications
- Connection management
- Redis backplane for scaling

### 2. Background Jobs (Hangfire)
- Export processing
- Maintenance tasks
- Job monitoring dashboard

### 3. Enhanced Security
- WebAuthn (FIDO2) support
- Two-Factor Authentication (TOTP)
- Policy-based authorization

### 4. Caching (Redis)
- Response caching
- Session state
- Cache invalidation strategies

### 5. Document Storage (MongoDB)
- Structured logging
- Analytics data
- Export metadata

### 6. Observability
- Distributed tracing (OpenTelemetry)
- Structured logging (Serilog)
- Metrics collection (Prometheus)
- Dashboards (Grafana)

## Development Workflow

### 1. Code Changes
```bash
# Make changes to code
# Build and test
dotnet build
dotnet test

# Run with hot reload
dotnet watch run
```

### 2. Database Changes
```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### 3. Testing
```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration
```

### 4. Monitoring
- **Logs**: Check Kibana for application logs
- **Metrics**: Monitor Grafana dashboards
- **Traces**: View request traces in Jaeger
- **Jobs**: Monitor Hangfire dashboard at `/hangfire`

## Troubleshooting

### Common Issues

**Services won't start:**
```bash
# Check Docker status
docker ps

# View service logs
docker-compose -f docker-compose.dev.yml logs [service-name]

# Restart services
docker-compose -f docker-compose.dev.yml restart
```

**Database connection issues:**
```bash
# Reset database
rm ticketsales.db
dotnet ef database update
```

**Port conflicts:**
- Modify ports in `docker-compose.dev.yml`
- Update corresponding configuration in `appsettings.Development.json`

**Build errors:**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### Logs and Debugging

**Application logs:**
- Console output during development
- File logs in `logs/` directory
- Elasticsearch logs viewable in Kibana

**Service logs:**
```bash
# All services
docker-compose -f docker-compose.dev.yml logs -f

# Specific service
docker-compose -f docker-compose.dev.yml logs -f redis
```

## Testing

### Unit Tests
```bash
dotnet test --filter Category=Unit
```

### Integration Tests
```bash
# Start test infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Run integration tests
dotnet test --filter Category=Integration
```

### API Testing
- Use Swagger UI at https://localhost:5001/swagger
- Import Postman collection (generated from OpenAPI spec)
- Test WebSocket connections using browser dev tools

## Production Deployment

### Environment Preparation
1. **Security**: Change all default passwords
2. **SSL/TLS**: Configure proper certificates
3. **Database**: Use production database server
4. **Monitoring**: Set up alerting rules
5. **Backup**: Configure backup strategies

### Configuration Updates
- Update connection strings
- Enable security features
- Configure production logging
- Set resource limits

### Deployment Steps
1. Build release version
2. Deploy infrastructure services
3. Run database migrations
4. Deploy application
5. Verify health checks

## Additional Resources

### Documentation
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
- [Hangfire Documentation](https://docs.hangfire.io)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net)

### Tools
- [Postman](https://www.postman.com) - API testing
- [MongoDB Compass](https://www.mongodb.com/products/compass) - MongoDB GUI
- [Redis Insight](https://redislabs.com/redis-enterprise/redis-insight) - Redis GUI
- [Elasticsearch Head](https://github.com/mobz/elasticsearch-head) - Elasticsearch GUI

### Support
- Check logs in Kibana for application issues
- Monitor metrics in Grafana for performance issues
- Use Jaeger for request tracing and debugging
- Review Hangfire dashboard for background job issues