# TOTP (Two-Factor Authentication) Testing Script
# This script tests the TOTP functionality of the TicketSalesApp.AdminServer

param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$Username = "admin",
    [string]$Password = "admin"
)

Write-Host "=== TOTP (Two-Factor Authentication) Testing Script ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Username: $Username" -ForegroundColor Yellow

# Function to make HTTP requests
function Invoke-ApiRequest {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        # Ignore SSL certificate errors for development
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message; Response = $_.Exception.Response }
    }
}

# Step 1: Login to get JWT token
Write-Host "`n1. Logging in to get JWT token..." -ForegroundColor Cyan
$loginUrl = "$BaseUrl/api/v1/auth/login"
$loginBody = @{
    username = $Username
    password = $Password
}

$loginResult = Invoke-ApiRequest -Url $loginUrl -Method "POST" -Body $loginBody

if (-not $loginResult.Success) {
    Write-Host "❌ Login failed: $($loginResult.Error)" -ForegroundColor Red
    exit 1
}

$token = $loginResult.Data.token
Write-Host "✅ Login successful" -ForegroundColor Green
Write-Host "Token: $($token.Substring(0, 20))..." -ForegroundColor Gray

# Set up headers with JWT token
$headers = @{
    "Authorization" = "Bearer $token"
}

# Step 2: Check current TOTP status
Write-Host "`n2. Checking current TOTP status..." -ForegroundColor Cyan
$statusUrl = "$BaseUrl/api/v1/auth/2fa/status"
$statusResult = Invoke-ApiRequest -Url $statusUrl -Headers $headers

if ($statusResult.Success) {
    Write-Host "✅ TOTP Status retrieved" -ForegroundColor Green
    Write-Host "Is Enabled: $($statusResult.Data.isEnabled)" -ForegroundColor Yellow
    Write-Host "User ID: $($statusResult.Data.userId)" -ForegroundColor Yellow
} else {
    Write-Host "❌ Failed to get TOTP status: $($statusResult.Error)" -ForegroundColor Red
}

# Step 3: Setup TOTP (if not already enabled)
if (-not $statusResult.Data.isEnabled) {
    Write-Host "`n3. Setting up TOTP..." -ForegroundColor Cyan
    $setupUrl = "$BaseUrl/api/v1/auth/2fa/setup"
    $setupResult = Invoke-ApiRequest -Url $setupUrl -Method "POST" -Headers $headers

    if ($setupResult.Success) {
        Write-Host "✅ TOTP Setup successful" -ForegroundColor Green
        Write-Host "Secret Key: $($setupResult.Data.secretKey)" -ForegroundColor Yellow
        Write-Host "Manual Entry Key: $($setupResult.Data.manualEntryKey)" -ForegroundColor Yellow
        Write-Host "Issuer: $($setupResult.Data.issuer)" -ForegroundColor Yellow
        Write-Host "Username: $($setupResult.Data.username)" -ForegroundColor Yellow
        
        # Save QR code data URL to file for manual inspection
        $qrCodeData = $setupResult.Data.qrCodeDataUrl
        if ($qrCodeData) {
            $qrCodeFile = "totp-qr-code.html"
            $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>TOTP QR Code</title>
</head>
<body>
    <h1>TOTP Setup QR Code</h1>
    <p>Scan this QR code with your authenticator app:</p>
    <img src="$qrCodeData" alt="TOTP QR Code" />
    <p><strong>Manual Entry Key:</strong> $($setupResult.Data.manualEntryKey)</p>
    <p><strong>Secret Key:</strong> $($setupResult.Data.secretKey)</p>
    <p><strong>Issuer:</strong> $($setupResult.Data.issuer)</p>
    <p><strong>Username:</strong> $($setupResult.Data.username)</p>
    <h3>Instructions:</h3>
    <ol>
        <li>Install an authenticator app (Google Authenticator, Authy, Microsoft Authenticator, etc.)</li>
        <li>Scan the QR code or manually enter the secret key</li>
        <li>Enter the 6-digit code from your authenticator app to verify setup</li>
        <li>Save your recovery codes in a secure location</li>
    </ol>
</body>
</html>
"@
            $htmlContent | Out-File -FilePath $qrCodeFile -Encoding UTF8
            Write-Host "📄 QR Code saved to: $qrCodeFile" -ForegroundColor Blue
        }
        
        Write-Host "`n⚠️  Please scan the QR code with your authenticator app and enter the 6-digit code to continue..." -ForegroundColor Yellow
        $verificationCode = Read-Host "Enter the 6-digit verification code from your authenticator app"
        
        # Step 4: Enable TOTP with verification code
        Write-Host "`n4. Enabling TOTP with verification code..." -ForegroundColor Cyan
        $enableUrl = "$BaseUrl/api/v1/auth/2fa/enable"
        $enableBody = @{
            verificationCode = $verificationCode
        }
        
        $enableResult = Invoke-ApiRequest -Url $enableUrl -Method "POST" -Headers $headers -Body $enableBody
        
        if ($enableResult.Success) {
            Write-Host "✅ TOTP Enabled successfully" -ForegroundColor Green
            Write-Host "Message: $($enableResult.Data.message)" -ForegroundColor Yellow
            
            if ($enableResult.Data.recoveryCodes) {
                Write-Host "`n🔑 Recovery Codes (save these in a secure location):" -ForegroundColor Magenta
                $enableResult.Data.recoveryCodes | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
                
                # Save recovery codes to file
                $recoveryFile = "totp-recovery-codes.txt"
                $enableResult.Data.recoveryCodes | Out-File -FilePath $recoveryFile -Encoding UTF8
                Write-Host "📄 Recovery codes saved to: $recoveryFile" -ForegroundColor Blue
            }
        } else {
            Write-Host "❌ Failed to enable TOTP: $($enableResult.Error)" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Failed to setup TOTP: $($setupResult.Error)" -ForegroundColor Red
    }
} else {
    Write-Host "`n3. TOTP is already enabled for this user" -ForegroundColor Yellow
}

# Step 5: Test TOTP validation
Write-Host "`n5. Testing TOTP validation..." -ForegroundColor Cyan
$testCode = Read-Host "Enter a 6-digit TOTP code from your authenticator app to test validation"

$validateUrl = "$BaseUrl/api/v1/auth/2fa/validate"
$validateBody = @{
    code = $testCode
}

$validateResult = Invoke-ApiRequest -Url $validateUrl -Method "POST" -Headers $headers -Body $validateBody

if ($validateResult.Success) {
    Write-Host "✅ TOTP Validation result: $($validateResult.Data.isValid)" -ForegroundColor Green
    Write-Host "Message: $($validateResult.Data.message)" -ForegroundColor Yellow
} else {
    Write-Host "❌ Failed to validate TOTP: $($validateResult.Error)" -ForegroundColor Red
}

# Step 6: Test recovery code generation (optional)
$generateRecovery = Read-Host "`n6. Generate new recovery codes? (y/n)"
if ($generateRecovery -eq "y" -or $generateRecovery -eq "Y") {
    Write-Host "Generating new recovery codes..." -ForegroundColor Cyan
    $newCode = Read-Host "Enter a 6-digit TOTP code to authorize recovery code generation"
    
    $recoveryUrl = "$BaseUrl/api/v1/auth/2fa/recovery-codes"
    $recoveryBody = @{
        code = $newCode
    }
    
    $recoveryResult = Invoke-ApiRequest -Url $recoveryUrl -Method "POST" -Headers $headers -Body $recoveryBody
    
    if ($recoveryResult.Success) {
        Write-Host "✅ New recovery codes generated" -ForegroundColor Green
        Write-Host "Message: $($recoveryResult.Data.message)" -ForegroundColor Yellow
        
        if ($recoveryResult.Data.recoveryCodes) {
            Write-Host "`n🔑 New Recovery Codes:" -ForegroundColor Magenta
            $recoveryResult.Data.recoveryCodes | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
            
            # Save new recovery codes to file
            $newRecoveryFile = "totp-recovery-codes-new.txt"
            $recoveryResult.Data.recoveryCodes | Out-File -FilePath $newRecoveryFile -Encoding UTF8
            Write-Host "📄 New recovery codes saved to: $newRecoveryFile" -ForegroundColor Blue
        }
    } else {
        Write-Host "❌ Failed to generate recovery codes: $($recoveryResult.Error)" -ForegroundColor Red
    }
}

Write-Host "`n=== TOTP Testing Complete ===" -ForegroundColor Green
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "- TOTP Status: Retrieved" -ForegroundColor White
Write-Host "- TOTP Setup: Available" -ForegroundColor White
Write-Host "- TOTP Validation: Tested" -ForegroundColor White
Write-Host "- Recovery Codes: Available" -ForegroundColor White

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Test TOTP with different authenticator apps" -ForegroundColor White
Write-Host "2. Test recovery code validation" -ForegroundColor White
Write-Host "3. Test TOTP disable functionality" -ForegroundColor White
Write-Host "4. Test TOTP in production environment" -ForegroundColor White