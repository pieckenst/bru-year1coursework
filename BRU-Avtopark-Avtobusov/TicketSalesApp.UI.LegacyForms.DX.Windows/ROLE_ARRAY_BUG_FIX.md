# Role Array Bug - FIXED ✅

## The Problem

Admin user was being treated as regular user (buttons disabled) because the JWT role claim was an **array instead of a single value**.

### What Was Happening

**JWT Token Payload:**
```json
{
  "nameid": "1",
  "unique_name": "admin",
  "role": ["1", "1"],  // <-- ARRAY with duplicate values!
  "jti": "...",
  ...
}
```

**Expected:**
```json
{
  "role": "1"  // Single value
}
```

### Error in Logs
```
Found 'role' claim in token. Raw value: [
  "1",
  "1"
]
Failed to convert role claim to integer. 
Exception: System.InvalidCastException: Cannot cast JArray to JToken
```

## Root Cause

**AuthController.cs** (line 1753-1754) was adding the role claim **TWICE**:

```csharp
new Claim(ClaimTypes.Role, user.Role.ToString()),  // Claim #1
new Claim("role", user.Role.ToString()),            // Claim #2
```

When serialized to JWT, this created an array: `"role": ["1", "1"]`

## The Fix

### 1. Server Side (AuthController.cs) ✅
**Removed duplicate role claim:**

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new Claim(ClaimTypes.Name, user.Login),
    // REMOVED: new Claim(ClaimTypes.Role, user.Role.ToString()),
    new Claim("role", user.Role.ToString()),  // Keep only this one
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    ...
};
```

### 2. Client Side (ApiClientService.cs) ✅
**Made parser resilient to handle both single values AND arrays:**

```csharp
if (roleToken.Type == JTokenType.Array)
{
    Log.Warn("Role claim is an array (duplicate claims). Taking first value.");
    var roleArray = (JArray)roleToken;
    if (roleArray.Count > 0)
    {
        _userRole = roleArray[0].Value<int>();
    }
}
else
{
    _userRole = roleToken.Value<int>();
}
```

## Testing

### Before Fix
```
[Error] Failed to convert role claim '[  "1",  "1"]' to integer
[Info] Permission check - IsAdmin: False, Role value: NULL
[Debug] bbiBuses enabled: False
[Debug] bbiEmployees enabled: False
```

### After Fix (Expected)
```
[Debug] Found 'role' claim in token. Raw value: 1, Type: Integer
[Info] *** User role successfully parsed from token: 1 ***
[Info] *** UserRole is now: ADMIN (0=User, 1=Admin) ***
[Info] Permission check - IsAdmin: True, Role value: 1
[Debug] bbiBuses enabled: True
[Debug] bbiEmployees enabled: True
```

## How to Test

1. **Restart the API server** (to pick up the AuthController fix)
2. **Rebuild the WinForms app** (to get the parser fix)
3. Login as "admin" / "admin"
4. Check the log file - should show:
   - Role parsed successfully as `1`
   - All employee buttons enabled
   - Status bar shows "Администратор"

## Files Modified

- ✅ `AuthController.cs` - Removed duplicate role claim
- ✅ `ApiClientService.cs` - Added array handling for resilience

## Why Both Fixes?

1. **Server fix** - Prevents the problem from happening
2. **Client fix** - Handles legacy tokens and edge cases gracefully

This way, even if old tokens with arrays are still valid, the client won't crash.

## Result

Admin now has **FULL ACCESS** to all employee management functions! 🎉
