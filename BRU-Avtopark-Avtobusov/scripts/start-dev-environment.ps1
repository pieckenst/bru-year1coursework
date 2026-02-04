# TicketSales Development Environment Startup Script
# This script starts all required infrastructure services for development

param(
    [switch]$SkipBuild,
    [switch]$Reset,
    [switch]$Logs
)

Write-Host "🚀 Starting TicketSales Development Environment" -ForegroundColor Green

# Change to the project root directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

# Reset environment if requested
if ($Reset) {
    Write-Host "🔄 Resetting development environment..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml down -v
    Write-Host "✅ Environment reset complete" -ForegroundColor Green
}

# Start infrastructure services
Write-Host "🐳 Starting infrastructure services..." -ForegroundColor Blue
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to be healthy
Write-Host "⏳ Waiting for services to be ready..." -ForegroundColor Yellow

$services = @(
    @{Name="Redis"; Container="ticketsales-redis"; Port=6379},
    @{Name="MongoDB"; Container="ticketsales-mongodb"; Port=27017},
    @{Name="Elasticsearch"; Container="ticketsales-elasticsearch"; Port=9200},
    @{Name="Jaeger"; Container="ticketsales-jaeger"; Port=16686}
)

foreach ($service in $services) {
    Write-Host "Checking $($service.Name)..." -ForegroundColor Cyan
    
    $maxAttempts = 30
    $attempt = 0
    
    do {
        $attempt++
        $health = docker inspect --format='{{.State.Health.Status}}' $service.Container 2>$null
        
        if ($health -eq "healthy") {
            Write-Host "✅ $($service.Name) is ready" -ForegroundColor Green
            break
        }
        
        if ($attempt -ge $maxAttempts) {
            Write-Host "❌ $($service.Name) failed to start within timeout" -ForegroundColor Red
            break
        }
        
        Start-Sleep -Seconds 2
    } while ($true)
}

# Display service URLs
Write-Host "`n🌐 Service URLs:" -ForegroundColor Green
Write-Host "Redis:         localhost:6379 (password: devpassword)" -ForegroundColor White
Write-Host "MongoDB:       localhost:27017 (user: ticketsales_dev, password: devpassword)" -ForegroundColor White
Write-Host "Elasticsearch: http://localhost:9200" -ForegroundColor White
Write-Host "Kibana:        http://localhost:5601" -ForegroundColor White
Write-Host "Prometheus:    http://localhost:9090" -ForegroundColor White
Write-Host "Grafana:       http://localhost:3000 (admin/devpassword)" -ForegroundColor White
Write-Host "Jaeger:        http://localhost:16686" -ForegroundColor White

# Build and start API server if not skipped
if (-not $SkipBuild) {
    Write-Host "`n🔨 Building API server..." -ForegroundColor Blue
    dotnet build TicketSalesApp.AdminServer/TicketSalesApp.AdminServer.csproj
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
        Write-Host "`n🚀 Starting API server..." -ForegroundColor Blue
        Write-Host "API will be available at:" -ForegroundColor White
        Write-Host "  HTTP:  http://localhost:5000" -ForegroundColor White
        Write-Host "  HTTPS: https://localhost:5001" -ForegroundColor White
        Write-Host "  Swagger: https://localhost:5001/swagger" -ForegroundColor White
        Write-Host "`nPress Ctrl+C to stop the API server" -ForegroundColor Yellow
        
        # Start the API server
        Set-Location TicketSalesApp.AdminServer
        dotnet run
    } else {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n📝 To start the API server manually:" -ForegroundColor Yellow
    Write-Host "cd TicketSalesApp.AdminServer" -ForegroundColor White
    Write-Host "dotnet run" -ForegroundColor White
}

# Show logs if requested
if ($Logs) {
    Write-Host "`n📋 Showing service logs..." -ForegroundColor Blue
    docker-compose -f docker-compose.dev.yml logs -f
}

Write-Host "`n✅ Development environment is ready!" -ForegroundColor Green