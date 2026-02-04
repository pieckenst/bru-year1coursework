#!/bin/bash

# TicketSales Development Environment Startup Script
# This script starts all required infrastructure services for development

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Parse command line arguments
SKIP_BUILD=false
RESET=false
LOGS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --reset)
            RESET=true
            shift
            ;;
        --logs)
            LOGS=true
            shift
            ;;
        *)
            echo "Unknown option $1"
            echo "Usage: $0 [--skip-build] [--reset] [--logs]"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}🚀 Starting TicketSales Development Environment${NC}"

# Change to the project root directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

# Reset environment if requested
if [ "$RESET" = true ]; then
    echo -e "${YELLOW}🔄 Resetting development environment...${NC}"
    docker-compose -f docker-compose.dev.yml down -v
    echo -e "${GREEN}✅ Environment reset complete${NC}"
fi

# Start infrastructure services
echo -e "${BLUE}🐳 Starting infrastructure services...${NC}"
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to be healthy
echo -e "${YELLOW}⏳ Waiting for services to be ready...${NC}"

services=(
    "Redis:ticketsales-redis:6379"
    "MongoDB:ticketsales-mongodb:27017"
    "Elasticsearch:ticketsales-elasticsearch:9200"
    "Jaeger:ticketsales-jaeger:16686"
)

for service_info in "${services[@]}"; do
    IFS=':' read -r name container port <<< "$service_info"
    echo -e "${CYAN}Checking $name...${NC}"
    
    max_attempts=30
    attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        attempt=$((attempt + 1))
        
        if health=$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null); then
            if [ "$health" = "healthy" ]; then
                echo -e "${GREEN}✅ $name is ready${NC}"
                break
            fi
        fi
        
        if [ $attempt -ge $max_attempts ]; then
            echo -e "${RED}❌ $name failed to start within timeout${NC}"
            break
        fi
        
        sleep 2
    done
done

# Display service URLs
echo -e "\n${GREEN}🌐 Service URLs:${NC}"
echo -e "${WHITE}Redis:         localhost:6379 (password: devpassword)${NC}"
echo -e "${WHITE}MongoDB:       localhost:27017 (user: ticketsales_dev, password: devpassword)${NC}"
echo -e "${WHITE}Elasticsearch: http://localhost:9200${NC}"
echo -e "${WHITE}Kibana:        http://localhost:5601${NC}"
echo -e "${WHITE}Prometheus:    http://localhost:9090${NC}"
echo -e "${WHITE}Grafana:       http://localhost:3000 (admin/devpassword)${NC}"
echo -e "${WHITE}Jaeger:        http://localhost:16686${NC}"

# Build and start API server if not skipped
if [ "$SKIP_BUILD" = false ]; then
    echo -e "\n${BLUE}🔨 Building API server...${NC}"
    dotnet build TicketSalesApp.AdminServer/TicketSalesApp.AdminServer.csproj
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Build successful${NC}"
        echo -e "\n${BLUE}🚀 Starting API server...${NC}"
        echo -e "${WHITE}API will be available at:${NC}"
        echo -e "${WHITE}  HTTP:  http://localhost:5000${NC}"
        echo -e "${WHITE}  HTTPS: https://localhost:5001${NC}"
        echo -e "${WHITE}  Swagger: https://localhost:5001/swagger${NC}"
        echo -e "${YELLOW}\nPress Ctrl+C to stop the API server${NC}"
        
        # Start the API server
        cd TicketSalesApp.AdminServer
        dotnet run
    else
        echo -e "${RED}❌ Build failed${NC}"
        exit 1
    fi
else
    echo -e "\n${YELLOW}📝 To start the API server manually:${NC}"
    echo -e "${WHITE}cd TicketSalesApp.AdminServer${NC}"
    echo -e "${WHITE}dotnet run${NC}"
fi

# Show logs if requested
if [ "$LOGS" = true ]; then
    echo -e "\n${BLUE}📋 Showing service logs...${NC}"
    docker-compose -f docker-compose.dev.yml logs -f
fi

echo -e "\n${GREEN}✅ Development environment is ready!${NC}"