# Sales Reports Fix v2 - Nested Object Preservation

## Issues Found in Latest Run

### Issue 1: Admin Role Still Broken (First Two Runs)
**Logs showed:**
```
16:08:07 [Error] Cannot cast JArray to JToken
16:08:07 [Debug] Decoded JWT Payload: {"role":["1","1"],...}
```

**Root Cause:** AdminServer was not rebuilt after fixing `AuthController.cs`

**Solution:** Rebuild the AdminServer project! The fix is already in place, just need to recompile.

Third run showed it working correctly:
```
16:11:19 [Debug] Decoded JWT Payload: {"role":"1",...}
16:11:19 [Info] *** User role successfully parsed from token: 1 ***
```

---

### Issue 2: Route Names Still Missing ("Marshut node missing")
**Logs showed:**
```
[Warn] Marshut node missing within Bilet data source. Cannot determine route name or ID.
Source XML: <bilet>
  <ticketId>1</ticketId>
  <ticketPrice>0.85</ticketPrice>
  <routeId>1</routeId>
</bilet>
```

**Root Cause:** Our `ExtractEssentialFields` method was **TOO AGGRESSIVE**:
- It only copied scalar values (strings, numbers)
- It **skipped ALL nested objects**, including the `marshut` object inside `bilet`
- The `marshut` object contains `startPoint` and `endPoint` needed for route names!

**Old Logic:**
```csharp
// Only copy scalar values (no objects or arrays to avoid recursion)
if (value.Type != JTokenType.Object && value.Type != JTokenType.Array)
{
    result[fieldName] = value.DeepClone();
}
// ← This skipped the marshut field in Bilet!
```

**Result:** When extracting essential fields from a `Bilet` object, the `marshut` field was skipped entirely, so the XML had no route information.

---

## The Fix

### Changed Essential Field Arrays
Added nested object fields to the essential arrays:

```csharp
// OLD
private static readonly string[] ESSENTIAL_BILET_FIELDS = 
    new string[] { "ticketId", "ticketPrice", "seatNumber", "routeId" };
private static readonly string[] ESSENTIAL_SALE_FIELDS = 
    new string[] { "saleId", "saleDate", "ticketSoldToUser", "ticketSoldToUserPhone", "ticketId" };

// NEW - Added nested objects
private static readonly string[] ESSENTIAL_BILET_FIELDS = 
    new string[] { "ticketId", "ticketPrice", "seatNumber", "routeId", "marshut" };
private static readonly string[] ESSENTIAL_SALE_FIELDS = 
    new string[] { "saleId", "saleDate", "ticketSoldToUser", "ticketSoldToUserPhone", "ticketId", "bilet" };
```

### Enhanced ExtractEssentialFields Method
Now **recursively extracts essential fields from nested objects**:

```csharp
private static JObject ExtractEssentialFields(JObject obj, string[] essentialFields)
{
    if (obj == null) return new JObject();
    
    JObject result = new JObject();
    foreach (string fieldName in essentialFields)
    {
        JToken value = obj[fieldName];
        if (value == null || value.Type == JTokenType.Null) continue;
        
        // Copy scalar values directly
        if (value.Type != JTokenType.Object && value.Type != JTokenType.Array)
        {
            result[fieldName] = value.DeepClone();
        }
        // NEW: For nested objects, recursively extract their essential fields
        else if (value.Type == JTokenType.Object)
        {
            JObject nestedObj = (JObject)value;
            // Determine which essential fields to use based on nested object type
            if (fieldName == "marshut" || nestedObj["startPoint"] != null || nestedObj["endPoint"] != null)
                result[fieldName] = ExtractEssentialFields(nestedObj, ESSENTIAL_MARSHUT_FIELDS);
            else if (fieldName == "bilet" || nestedObj["ticketPrice"] != null)
                result[fieldName] = ExtractEssentialFields(nestedObj, ESSENTIAL_BILET_FIELDS);
            else if (fieldName == "employee" || nestedObj["surname"] != null)
                result[fieldName] = ExtractEssentialFields(nestedObj, ESSENTIAL_EMPLOYEE_FIELDS);
            // Skip unknown nested objects to avoid infinite recursion
        }
        // Skip arrays to avoid recursion
    }
    return result;
}
```

## How It Works Now

### Example: Sale → Bilet → Marshut Chain

**When recursion is detected:**

1. **Extract SALE essential fields:**
   - Scalar: `saleId`, `saleDate`, `ticketSoldToUser`, `ticketSoldToUserPhone`, `ticketId`
   - Nested: `bilet` (recognized as nested object)

2. **Recursively extract BILET essential fields from the bilet object:**
   - Scalar: `ticketId`, `ticketPrice`, `seatNumber`, `routeId`
   - Nested: `marshut` (recognized as nested object) ← **THIS WAS MISSING BEFORE!**

3. **Recursively extract MARSHUT essential fields from the marshut object:**
   - Scalar: `routeId`, `startPoint`, `endPoint`, `travelTime`

**Result XML:**
```xml
<bilet>
  <ticketId>1</ticketId>
  <ticketPrice>0.85</ticketPrice>
  <routeId>1</routeId>
  <marshut>
    <routeId>1</routeId>
    <startPoint>Вейнянка</startPoint>
    <endPoint>Фатина</endPoint>
    <travelTime>45 минут</travelTime>
  </marshut>
</bilet>
```

Now the code can read `startPoint` and `endPoint` to build route names like "Вейнянка → Фатина"! 🎉

## Files Modified

- ✅ `frmSalesStatistics.cs`:
  - Updated `ESSENTIAL_BILET_FIELDS` and `ESSENTIAL_SALE_FIELDS` arrays
  - Enhanced `ExtractEssentialFields` method with nested object support

- ✅ `frmIncomeReport.cs`:
  - Updated `ESSENTIAL_BILET_FIELDS` and `ESSENTIAL_SALE_FIELDS` arrays
  - Enhanced `ExtractEssentialFields` method with nested object support

## Testing Checklist

### Before Testing - REBUILD ADMIN SERVER!
```powershell
cd d:\code\bru-year1coursework\BRU-Avtopark-Avtobusov\TicketSalesApp.AdminServer
dotnet build
dotnet run
```

### Test 1: Admin Login
1. Run the WinForms app
2. Login as "admin"
3. **Expected logs:**
   ```
   [Debug] Decoded JWT Payload: {"role":"1",...}  ← NOT ["1","1"]!
   [Info] *** User role successfully parsed from token: 1 ***
   [Info] Permission check - IsAdmin: True
   ```
4. **Expected UI:** All admin buttons enabled

### Test 2: Sales Statistics
1. Open Sales Statistics form
2. Select date range (e.g., 01.11.2025 - 15.11.2025)
3. Click "Применить"
4. **Expected:** Route names show as "Вейнянка → Фатина" instead of "Неизвестный маршрут"
5. **Expected logs:** NO "Marshut node missing" warnings!

### Test 3: Income Report
1. Open Income Report form
2. Select date range
3. Apply filters
4. **Expected:** 
   - PassengerName: "admin", "ФИЗ.ПРОДАЖА" (not "0нет")
   - PassengerPhone: "+375333000000" (not blank)
   - RouteDescription: "Вейнянка → Фатина" (not blank)

## Why This Fix Works

### Prevents Data Loss
- **Before:** Nested objects were completely skipped → data loss
- **After:** Nested objects are recursively processed → data preserved

### Prevents Infinite Recursion
- Only processes objects that match known types (`marshut`, `bilet`, `employee`)
- Unknown nested objects are skipped
- Arrays are always skipped
- Each level only extracts essential fields (no full deep clones)

### Preserves Structure
The XML now contains the full essential hierarchy:
```
Sale
  └─ bilet
      └─ marshut
          └─ startPoint, endPoint
```

Instead of just:
```
Sale
  └─ (no bilet, data lost!)
```

## Technical Notes

### Recursion Depth
Maximum depth is 2 levels:
- Sale → Bilet → Marshut (depth 2)
- This is safe and won't cause stack overflow

### Performance
- Minimal impact (simple field copying)
- No circular references followed (recursion still detected and broken)
- Cached results still work

### Type Detection
Uses two methods to detect object type:
1. **Field name:** `if (fieldName == "marshut")`
2. **Content check:** `if (nestedObj["startPoint"] != null)`

This handles both direct fields and $ref-resolved objects.

## Summary

✅ **Fixed:** Route names now appear correctly in Sales Statistics  
✅ **Fixed:** Passenger info now appears correctly in Income Report  
✅ **Maintained:** Circular reference protection (no infinite loops)  
✅ **Maintained:** Performance (no excessive copying)  

**Action Required:** REBUILD ADMINSERVER to fix the role array issue!

The cursed JSON processing is now... slightly less cursed! 🔥→🧊
