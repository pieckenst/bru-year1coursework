# Quick PowerShell script to check admin user in SQLite database
# Run this from the project root where the .db file is located

param(
    [string]$DbPath = ".\TicketSales.db"
)

if (-not (Test-Path $DbPath)) {
    Write-Host "Database not found at: $DbPath" -ForegroundColor Red
    Write-Host "Please specify the correct path with: .\check-admin.ps1 -DbPath 'path\to\your.db'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Checking admin user in database: $DbPath" -ForegroundColor Cyan
Write-Host ""

# Check if sqlite3.exe is available
$sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqlite) {
    Write-Host "sqlite3.exe not found in PATH." -ForegroundColor Yellow
    Write-Host "Download from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or install via: winget install SQLite.SQLite" -ForegroundColor Yellow
    exit 1
}

Write-Host "=== ADMIN USER DETAILS ===" -ForegroundColor Green
& sqlite3 $DbPath "SELECT 'UserId: ' || UserId || char(10) || 'Login: ' || Login || char(10) || 'Role: ' || Role || ' (0=User, 1=Admin)' || char(10) || 'GuidId: ' || GuidId || char(10) || 'IsActive: ' || IsActive || char(10) || 'CreatedAt: ' || CreatedAt FROM Users WHERE Login = 'admin';"
Write-Host ""

Write-Host "=== ADMIN ROLE ASSIGNMENTS (New System) ===" -ForegroundColor Green
& sqlite3 $DbPath "SELECT 'RoleName: ' || r.Name || char(10) || 'LegacyRoleId: ' || r.LegacyRoleId || char(10) || 'AssignedAt: ' || ur.AssignedAt FROM UserRoles ur JOIN Roles r ON ur.RoleId = r.RoleId JOIN Users u ON ur.UserId = u.GuidId WHERE u.Login = 'admin';"
Write-Host ""

Write-Host "=== QUICK CHECKS ===" -ForegroundColor Green
$roleCheck = & sqlite3 $DbPath "SELECT Role FROM Users WHERE Login = 'admin';"
if ($roleCheck -eq "1") {
    Write-Host "[OK] User.Role = 1 (Admin)" -ForegroundColor Green
} else {
    Write-Host "[FAIL] User.Role = $roleCheck (Expected: 1)" -ForegroundColor Red
}

$activeCheck = & sqlite3 $DbPath "SELECT IsActive FROM Users WHERE Login = 'admin';"
if ($activeCheck -eq "1" -or $activeCheck -eq "True") {
    Write-Host "[OK] User.IsActive = True" -ForegroundColor Green
} else {
    Write-Host "[FAIL] User.IsActive = $activeCheck (Expected: True)" -ForegroundColor Red
}

$userRoleCount = & sqlite3 $DbPath "SELECT COUNT(*) FROM UserRoles ur JOIN Users u ON ur.UserId = u.GuidId WHERE u.Login = 'admin';"
if ([int]$userRoleCount -gt 0) {
    Write-Host "[OK] Admin has $userRoleCount role assignment(s)" -ForegroundColor Green
} else {
    Write-Host "[WARN] Admin has 0 role assignments in UserRoles table" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== FIX COMMANDS (if needed) ===" -ForegroundColor Cyan
Write-Host "To fix admin role:" -ForegroundColor Yellow
Write-Host "  sqlite3 $DbPath `"UPDATE Users SET Role = 1 WHERE Login = 'admin';`"" -ForegroundColor White
Write-Host ""
Write-Host "To activate admin:" -ForegroundColor Yellow  
Write-Host "  sqlite3 $DbPath `"UPDATE Users SET IsActive = 1 WHERE Login = 'admin';`"" -ForegroundColor White
