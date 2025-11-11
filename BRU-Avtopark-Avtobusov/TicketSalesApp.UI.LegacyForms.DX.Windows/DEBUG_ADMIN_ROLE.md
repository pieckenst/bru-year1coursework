# Admin Role Debugging Guide

## The Problem
Admin user (login: "admin") is being treated as regular user (role 0) instead of admin (role 1).

## What We've Added
Enhanced logging at every step of the authentication flow:

1. **Login API Response** - Shows the JWT token returned by server
2. **Token Parsing** - Shows the role claim extraction from JWT
3. **Permission Application** - Shows which buttons are enabled/disabled

## How to Debug

### Step 1: Check the Log File
Run the app, login as "admin", and check the log file for these sections:

#### A. Token Parsing (from ApiClientService)
Look for:
```
[DEBUG] Starting JWT token parsing...
[DEBUG] Decoded JWT Payload: {...}
[DEBUG] Found 'role' claim in token. Raw value: X
[INFO] *** User role successfully parsed from token: X ***
[INFO] *** UserRole is now: ADMIN (0=User, 1=Admin) ***
```

**Expected:** Role value should be `1`

#### B. Login Complete (from frmLogin)
Look for:
```
[PASSWORD_LOGIN] *** LOGIN COMPLETE - User: admin, Role: 1 (Expected: 1 for admin) ***
```

**Expected:** Role should be `1`, not `NULL` or `0`

#### C. Permission Application (from Form1)
Look for:
```
[INFO] Permission check - IsAdmin: True, IsEmployee: True, Role value: 1
[DEBUG] bbiBuses enabled: True
[DEBUG] bbiRoutes enabled: True
[DEBUG] bbiEmployees enabled: True
```

**Expected:** All should show `True`

### Step 2: Database Check

Open your database and run:

```sql
SELECT UserId, Login, Role, GuidId, IsActive, CreatedAt 
FROM Users 
WHERE Login = 'admin';
```

**Expected Result:**
- `Login` = "admin"
- `Role` = 1
- `IsActive` = 1

Also check the new roles system:

```sql
SELECT ur.*, r.Name, r.LegacyRoleId
FROM UserRoles ur
JOIN Roles r ON ur.RoleId = r.RoleId
JOIN Users u ON ur.UserId = u.GuidId
WHERE u.Login = 'admin';
```

**Expected Result:**
- At least one role with `LegacyRoleId` = 1

### Step 3: JWT Token Inspection

If the log shows a JWT token, copy it and decode it at https://jwt.io

Check the payload for:
```json
{
  "role": "1",  // <-- This should be 1, not "0" or missing
  "unique_name": "admin",
  // ... other claims
}
```

## Common Issues & Fixes

### Issue 1: Role claim is "0" or missing from JWT
**Fix:** Check `AuthController.GenerateJwtToken()` line 1754
Should have: `new Claim("role", user.Role.ToString())`

### Issue 2: User.Role is 0 in database
**Fix:** Run this SQL to fix:
```sql
UPDATE Users SET Role = 1 WHERE Login = 'admin';
```

Then restart the API server.

### Issue 3: UserRole assignment missing
**Fix:** The DbInitializer should create a UserRole entry. Check if seeding ran:
```sql
SELECT COUNT(*) FROM UserRoles;
```

If 0, delete the database and let it re-seed.

### Issue 4: Token parsing fails silently
**Check the log for:**
- "Failed to convert role claim"
- "JWT payload does not contain 'role' claim"
- "Available claims: ..." (will show what claims ARE present)

## Manual Fix Script

If all else fails, run this in your database:

```sql
-- Ensure admin user exists with role 1
UPDATE Users SET Role = 1 WHERE Login = 'admin';

-- Ensure admin has the admin role in new system
INSERT OR IGNORE INTO UserRoles (Id, UserId, RoleId, AssignedAt, AssignedBy)
SELECT 
    lower(hex(randomblob(16))),
    u.GuidId,
    r.RoleId,
    datetime('now'),
    'Manual Fix'
FROM Users u
CROSS JOIN Roles r
WHERE u.Login = 'admin' AND r.LegacyRoleId = 1
AND NOT EXISTS (
    SELECT 1 FROM UserRoles ur2 
    WHERE ur2.UserId = u.GuidId AND ur2.RoleId = r.RoleId
);
```

Then restart both the API server and WinForms app.

## Expected Log Output (Success)

When everything works, you should see:

```
[DEBUG] Starting JWT token parsing...
[DEBUG] JWT payload parsed successfully. Claims count: 5
[DEBUG] Found 'role' claim in token. Raw value: 1
[INFO] *** User role successfully parsed from token: 1 ***
[INFO] *** UserRole is now: ADMIN (0=User, 1=Admin) ***
[INFO] Username parsed from token: admin
[PASSWORD_LOGIN] *** LOGIN COMPLETE - User: admin, Role: 1 (Expected: 1 for admin) ***
[INFO] Permission check - IsAdmin: True, IsEmployee: True, Role value: 1
[DEBUG] bbiBuses enabled: True
[DEBUG] bbiRoutes enabled: True
[DEBUG] bbiEmployees enabled: True
[DEBUG] bbiJobs enabled: True
[DEBUG] bbiMaintenance enabled: True
[INFO] Permissions applied successfully.
```

## Next Steps

1. Login as admin
2. Check the log file
3. Find which step is failing
4. Apply the appropriate fix
5. Report back with the log output if still not working
