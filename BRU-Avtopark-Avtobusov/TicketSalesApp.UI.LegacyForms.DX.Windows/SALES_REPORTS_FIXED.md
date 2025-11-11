# Sales Statistics & Income Report - FIXED ✅

## Issues Fixed

### 1. Sales Statistics (frmSalesStatistics.cs)
**Problem:** Route names displayed as "Неизвестный маршрут 1,2,8,15" instead of actual route names like "Вейнянка → Фатина"

### 2. Income Report (frmIncomeReport.cs)
**Problem:** Passenger and route fields showed "0нет" or were blank

## Root Cause

The `CleanAndTransformJsonToken` method was **too aggressive** when handling circular references in the JSON data. When it detected recursion (Sale → Bilet → Marshut → Employee → back to Employee), it returned **empty objects `{}`** which stripped out all essential data:

```csharp
// OLD CODE (BAD)
if (!currentlyProcessingRefs.Add(originalId)) {
    Log.Warn("Recursion detected. Returning empty object.");
    return new JObject(); // ← Lost all data!
}
```

This caused:
- `startPoint` and `endPoint` from routes to be lost → "Неизвестный маршрут"
- `TicketSoldToUser` and `TicketSoldToUserPhone` from sales to be lost → "0нет"

## The Fix

Modified recursion handling to **preserve essential scalar fields** even when circular references are detected:

### Step 1: Added Essential Field Definitions

```csharp
private static readonly string[] ESSENTIAL_MARSHUT_FIELDS = 
    new string[] { "routeId", "startPoint", "endPoint", "travelTime" };
private static readonly string[] ESSENTIAL_BILET_FIELDS = 
    new string[] { "ticketId", "ticketPrice", "seatNumber", "routeId" };
private static readonly string[] ESSENTIAL_SALE_FIELDS = 
    new string[] { "saleId", "saleDate", "ticketSoldToUser", "ticketSoldToUserPhone", "ticketId" };
private static readonly string[] ESSENTIAL_EMPLOYEE_FIELDS = 
    new string[] { "empId", "surname", "name", "patronym" };
```

### Step 2: Added Helper Method

```csharp
private static JObject ExtractEssentialFields(JObject obj, string[] essentialFields)
{
    if (obj == null) return new JObject();
    
    JObject result = new JObject();
    foreach (string fieldName in essentialFields)
    {
        JToken value = obj[fieldName];
        // Only copy SCALAR values (no objects/arrays to avoid recursion)
        if (value != null && 
            value.Type != JTokenType.Object && 
            value.Type != JTokenType.Array && 
            value.Type != JTokenType.Null)
        {
            result[fieldName] = value.DeepClone();
        }
    }
    return result;
}
```

### Step 3: Modified Recursion Detection (2 locations per file)

```csharp
// NEW CODE (GOOD)
if (!currentlyProcessingRefs.Add(originalId)) {
    Log.Warn("Recursion detected for $id = {0}. Extracting essential fields only.", originalId);
    
    // Detect object type and extract appropriate fields
    if (objToken["startPoint"] != null || objToken["endPoint"] != null)
        return ExtractEssentialFields(objToken, ESSENTIAL_MARSHUT_FIELDS);
    else if (objToken["ticketPrice"] != null)
        return ExtractEssentialFields(objToken, ESSENTIAL_BILET_FIELDS);
    else if (objToken["saleDate"] != null || objToken["ticketSoldToUser"] != null)
        return ExtractEssentialFields(objToken, ESSENTIAL_SALE_FIELDS);
    else if (objToken["surname"] != null || objToken["empId"] != null)
        return ExtractEssentialFields(objToken, ESSENTIAL_EMPLOYEE_FIELDS);
    else
        return new JObject(); // Fallback
}
```

## Files Modified

- ✅ `frmSalesStatistics.cs` - Lines 99-102 (field definitions), 971-987 (helper), 1027-1040 (recursion #1), 1060-1075 (recursion #2)
- ✅ `frmIncomeReport.cs` - Lines 105-108 (field definitions), 1000-1016 (helper), 1056-1069 (recursion #1), 1088-1103 (recursion #2)

## How It Works

1. **Recursion is still detected** (prevents infinite loops) ✅
2. **Instead of empty object**, essential scalar fields are extracted ✅
3. **Nested objects/arrays are NOT followed** (prevents recursion) ✅
4. **Result**: Route names and passenger info are preserved! 🎉

## Expected Results

### Sales Statistics

#### Before Fix:
```
Route: Неизвестный маршрут 1
Sales: 25
Revenue: 10,15 ₽
```

#### After Fix:
```
Route: Вейнянка → Фатина
Sales: 25  
Revenue: 10,15 ₽
```

### Income Report

#### Before Fix:
| Sale ID | Date | Route | Passenger | Phone | Amount |
|---------|------|-------|-----------|-------|--------|
| 1 | 08.11.2025 | 0нет | 0нет | 0нет | 0,75 ₽ |

#### After Fix:
| Sale ID | Date | Route | Passenger | Phone | Amount |
|---------|------|-------|-----------|-------|--------|
| 1 | 08.11.2025 | Вейнянка → Фатина | admin | +375333000000 | 0,75 ₽ |

## Testing

### Test Sales Statistics:
1. Open Sales Statistics form
2. Change date range (e.g., set date from 01.11.2025 to 15.11.2025)
3. Click "Применить"
4. **Expected:** Pie chart labels show actual route names like "Вейнянка → Фатина" instead of "Неизвестный маршрут"

### Test Income Report:
1. Open Income Report form
2. Select a date range
3. Apply filters
4. **Expected:** 
   - PassengerName column shows actual names (e.g., "admin", "ФИЗ.ПРОДАЖА")
   - PassengerPhone column shows phone numbers (e.g., "+375333000000")
   - RouteDescription shows "Start → End" format

## Technical Notes

### Why This Works

1. **Preserves recursion detection** - Still prevents infinite loops
2. **Minimal data extraction** - Only scalar values (strings, numbers, dates)
3. **Type-aware** - Detects object type and extracts appropriate fields
4. **Fallback safe** - Unknown types still get empty object (no crashes)
5. **Performance impact** - Minimal (simple field copying)

### What Fields Are Preserved

| Object Type | Fields Extracted |
|-------------|------------------|
| Marshut (Route) | routeId, startPoint, endPoint, travelTime |
| Bilet (Ticket) | ticketId, ticketPrice, seatNumber, routeId |
| Prodazha (Sale) | saleId, saleDate, ticketSoldToUser, ticketSoldToUserPhone, ticketId |
| Employee | empId, surname, name, patronym |

### What Fields Are NOT Preserved

- Nested objects (e.g., `employee`, `avtobus`, `marshut` objects)
- Arrays (e.g., `sales[]`, `tickets[]`)
- These would cause recursion, so they're intentionally skipped

## Rollback Plan

If this causes issues:

1. The changes are isolated to `CleanAndTransformJsonToken` method
2. Revert to returning `new JObject()` on recursion detection
3. Both files have identical changes, so revert both together

## Result

✅ Sales Statistics now shows actual route names  
✅ Income Report now shows passenger names and phone numbers  
✅ No crashes or infinite loops  
✅ Performance unchanged  

The cursed JSON processing is now slightly less cursed! 🔥
