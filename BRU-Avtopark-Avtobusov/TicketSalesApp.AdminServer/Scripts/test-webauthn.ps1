# WebAuthn API Testing Script
# This script tests the WebAuthn endpoints to ensure they're working correctly

Write-Host "Testing WebAuthn API Endpoints..." -ForegroundColor Green

$baseUrl = "http://localhost:5000"

# Test 1: Begin Login with valid username
Write-Host "`n1. Testing Begin Login with valid username..." -ForegroundColor Yellow
try {
    $body = '{"username":"admin"}'
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/webauthn/login/begin" -Method POST -Body $body -ContentType "application/json"
    
    if ($response.challenge -and $response.timeout -and $response.rpId) {
        Write-Host "✓ Begin Login test PASSED - Challenge generated successfully" -ForegroundColor Green
        Write-Host "  Challenge: $($response.challenge.Substring(0,20))..." -ForegroundColor Gray
        Write-Host "  Timeout: $($response.timeout)" -ForegroundColor Gray
        Write-Host "  RP ID: $($response.rpId)" -ForegroundColor Gray
    } else {
        Write-Host "✗ Begin Login test FAILED - Invalid response structure" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Begin Login test FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Begin Login with empty username
Write-Host "`n2. Testing Begin Login with empty username..." -ForegroundColor Yellow
try {
    $body = '{"username":""}'
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/webauthn/login/begin" -Method POST -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "✗ Empty username test FAILED - Should have returned error" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✓ Empty username test PASSED - Correctly returned BadRequest" -ForegroundColor Green
    } else {
        Write-Host "✗ Empty username test FAILED - Wrong status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# Test 3: Get Credentials without authentication
Write-Host "`n3. Testing Get Credentials without authentication..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/webauthn/credentials" -Method GET -ErrorAction Stop
    Write-Host "✗ Unauthorized access test FAILED - Should have returned 401" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ Unauthorized access test PASSED - Correctly returned Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "✗ Unauthorized access test FAILED - Wrong status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# Test 4: Check Swagger documentation
Write-Host "`n4. Testing Swagger documentation..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method GET
    $webauthnEndpoints = $swaggerResponse.paths.PSObject.Properties | Where-Object { $_.Name -like "*webauthn*" }
    
    if ($webauthnEndpoints.Count -ge 6) {
        Write-Host "✓ Swagger documentation test PASSED - Found $($webauthnEndpoints.Count) WebAuthn endpoints" -ForegroundColor Green
        foreach ($endpoint in $webauthnEndpoints) {
            Write-Host "  - $($endpoint.Name)" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Swagger documentation test FAILED - Expected at least 6 endpoints, found $($webauthnEndpoints.Count)" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Swagger documentation test FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nWebAuthn API Testing Complete!" -ForegroundColor Green
Write-Host "Note: Full WebAuthn functionality requires a FIDO2 authenticator device for complete testing." -ForegroundColor Cyan