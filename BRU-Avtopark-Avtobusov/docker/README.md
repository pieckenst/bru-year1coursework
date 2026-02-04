# TicketSales Development Environment

This directory contains Docker configuration for the TicketSales development environment, including all required infrastructure services.

## Services

### Core Infrastructure
- **Redis**: Caching, session state, and SignalR backplane
- **MongoDB**: Document storage for logs and analytics
- **Elasticsearch**: Log aggregation and search
- **Kibana**: Log visualization and analysis

### Monitoring & Observability
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization and dashboards
- **Jaeger**: Distributed tracing

## Quick Start

1. **Start all services:**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Check service health:**
   ```bash
   docker-compose -f docker-compose.dev.yml ps
   ```

3. **View logs:**
   ```bash
   docker-compose -f docker-compose.dev.yml logs -f [service-name]
   ```

4. **Stop all services:**
   ```bash
   docker-compose -f docker-compose.dev.yml down
   ```

## Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| Redis | localhost:6379 | Password: `devpassword` |
| MongoDB | localhost:27017 | User: `ticketsales_dev`, Password: `devpassword` |
| Elasticsearch | http://localhost:9200 | No authentication |
| Kibana | http://localhost:5601 | No authentication |
| Prometheus | http://localhost:9090 | No authentication |
| Grafana | http://localhost:3000 | User: `admin`, Password: `devpassword` |
| Jaeger UI | http://localhost:16686 | No authentication |

## Configuration

### Redis Configuration
- **Default Database (0)**: General purpose
- **Cache Database (1)**: Application caching
- **Session Database (2)**: User sessions
- **SignalR Database (3)**: SignalR backplane

### MongoDB Configuration
- **Database**: `ticketsales`
- **Collections**: `logs`, `analytics`, `exports`, `notifications`
- **Indexes**: Automatically created for performance

### Elasticsearch Configuration
- **Index Pattern**: `ticketsales-logs-*` (production), `ticketsales-dev-logs-*` (development)
- **Retention**: Managed by Elasticsearch ILM policies

## Development Tips

### Connecting to Services

**Redis CLI:**
```bash
docker exec -it ticketsales-redis redis-cli -a devpassword
```

**MongoDB Shell:**
```bash
docker exec -it ticketsales-mongodb mongosh -u ticketsales_dev -p devpassword --authenticationDatabase ticketsales
```

**Elasticsearch:**
```bash
curl http://localhost:9200/_cluster/health?pretty
```

### Viewing Logs

**Application logs in Kibana:**
1. Open http://localhost:5601
2. Go to "Discover"
3. Create index pattern: `ticketsales-*-logs-*`
4. Set time field: `@timestamp`

**Metrics in Grafana:**
1. Open http://localhost:3000
2. Login with admin/devpassword
3. Import dashboard from `/docker/grafana/dashboards/`

### Troubleshooting

**Service won't start:**
```bash
# Check logs
docker-compose -f docker-compose.dev.yml logs [service-name]

# Restart specific service
docker-compose -f docker-compose.dev.yml restart [service-name]
```

**Port conflicts:**
- Modify port mappings in `docker-compose.dev.yml`
- Update corresponding configuration in `appsettings.Development.json`

**Data persistence:**
- Data is stored in Docker volumes
- To reset all data: `docker-compose -f docker-compose.dev.yml down -v`

## Production Considerations

This configuration is for **development only**. For production:

1. **Security**: Change all default passwords
2. **SSL/TLS**: Enable encryption for all services
3. **Authentication**: Enable proper authentication
4. **Resource Limits**: Set appropriate memory/CPU limits
5. **Backup**: Configure data backup strategies
6. **Monitoring**: Set up proper alerting rules

## Environment Variables

Create a `.env` file in the same directory as `docker-compose.dev.yml`:

```env
# Redis
REDIS_PASSWORD=your-secure-password

# MongoDB
MONGO_ROOT_USERNAME=admin
MONGO_ROOT_PASSWORD=your-secure-password
MONGO_APP_USERNAME=ticketsales_dev
MONGO_APP_PASSWORD=your-secure-password

# Elasticsearch
ELASTIC_PASSWORD=your-secure-password

# Grafana
GRAFANA_ADMIN_PASSWORD=your-secure-password
```

## Health Checks

All services include health checks. Monitor with:

```bash
# Overall health
docker-compose -f docker-compose.dev.yml ps

# Detailed health status
docker inspect --format='{{.State.Health.Status}}' ticketsales-redis
```

## Scaling

For load testing, scale services:

```bash
# Scale API server (when containerized)
docker-compose -f docker-compose.dev.yml up -d --scale api=3

# Scale Redis (cluster mode)
# Requires Redis cluster configuration
```

## Backup & Restore

**MongoDB:**
```bash
# Backup
docker exec ticketsales-mongodb mongodump --uri="mongodb://ticketsales_dev:devpassword@localhost:27017/ticketsales" --out=/backup

# Restore
docker exec ticketsales-mongodb mongorestore --uri="mongodb://ticketsales_dev:devpassword@localhost:27017/ticketsales" /backup/ticketsales
```

**Redis:**
```bash
# Backup
docker exec ticketsales-redis redis-cli -a devpassword --rdb /data/backup.rdb

# Restore (copy backup.rdb to Redis data directory and restart)
```