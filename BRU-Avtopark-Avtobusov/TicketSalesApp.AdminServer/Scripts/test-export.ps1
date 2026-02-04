#!/usr/bin/env pwsh

# Test script for the Bulk Export System
# This script tests the export functionality by making HTTP requests to the export endpoints

Write-Host "Testing Bulk Export System..." -ForegroundColor Green

# Configuration
$baseUrl = "https://localhost:7001"
$apiUrl = "$baseUrl/api/v1/exports"

# Test 1: Get supported formats for users
Write-Host "`n1. Testing supported formats endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/users/formats" -Method GET -SkipCertificateCheck
    Write-Host "✓ Supported formats retrieved successfully" -ForegroundColor Green
    Write-Host "Formats: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to get supported formats: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get available fields for users
Write-Host "`n2. Testing available fields endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/users/fields" -Method GET -SkipCertificateCheck
    Write-Host "✓ Available fields retrieved successfully" -ForegroundColor Green
    Write-Host "Fields: $($response -join ', ')" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to get available fields: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Start a CSV export
Write-Host "`n3. Testing export creation..." -ForegroundColor Yellow
$exportRequest = @{
    entityType = "users"
    format = "CSV"
    includeHeaders = $true
    maxRecords = 10
    requestedBy = "test-user"
} | ConvertTo-Json

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    $response = Invoke-RestMethod -Uri $apiUrl -Method POST -Body $exportRequest -Headers $headers -SkipCertificateCheck
    $jobId = $response.jobId
    Write-Host "✓ Export job created successfully" -ForegroundColor Green
    Write-Host "Job ID: $jobId" -ForegroundColor Cyan
    
    # Test 4: Check export status
    Write-Host "`n4. Testing export status..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    try {
        $statusResponse = Invoke-RestMethod -Uri "$apiUrl/$jobId/status" -Method GET -SkipCertificateCheck
        Write-Host "✓ Export status retrieved successfully" -ForegroundColor Green
        Write-Host "Status: $($statusResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
    } catch {
        Write-Host "✗ Failed to get export status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Failed to create export: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: List all exports
Write-Host "`n5. Testing export list..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method GET -SkipCertificateCheck
    Write-Host "✓ Export list retrieved successfully" -ForegroundColor Green
    Write-Host "Exports count: $($response.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to get export list: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nBulk Export System test completed!" -ForegroundColor Green
Write-Host "Note: Make sure the AdminServer is running on $baseUrl before running this test." -ForegroundColor Yellow