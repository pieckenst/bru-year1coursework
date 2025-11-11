# Sales Statistics & Income Report Fix Plan

## Issues Identified

### 1. Sales Statistics (frmSalesStatistics)
**Problem:** Route names show as "Неизвестный маршрут 1,2,8,15" instead of actual names
**Root Cause:** `ProcessJsonToXml` replaces circular-referenced objects with empty `{}`, losing `startPoint` and `endPoint`

### 2. Income Report (frmIncomeReport)  
**Problem:** Passenger fields show "0нет" or blank
**Root Cause:** Same - essential fields like `TicketSoldToUser` get stripped during circular reference handling

## Technical Analysis

The `CleanAndTransformJsonToken` method (line 964+) detects recursion to prevent infinite loops:

```csharp
// Lines 1025-1029
if (!currentlyProcessingRefs.Add(refValue)) { 
    Log.Warn("Recursion detected: trying to resolve $ref = {0} ...", refValue);
    return new JObject(); // ← PROBLEM: Returns empty object!
}
```

When the JSON contains:
```json
{
  "saleId": 1,
  "bilet": {
    "$id": "10",
    "marshut": {
      "$id": "12",
      "startPoint": "Вейнянка",
      "endPoint": "Фатина",
      "employee": { "$ref": "3" }  // Circular back to employee
    }
  }
}
```

The recursion detection triggers and replaces the entire object with `{}`, losing `startPoint` and `endPoint`.

## Solution Strategy

Instead of returning empty objects on recursion, return a **minimal object** containing only essential scalar fields (strings, numbers, dates) without following further nested objects.

### Essential Fields to Preserve

For **Marshut** (Routes):
- `routeId`
- `startPoint`  
- `endPoint`
- `travelTime`

For **Sales** (Prodazha):
- `saleId`
- `saleDate`
- `ticketSoldToUser`
- `ticketSoldToUserPhone`
- `ticketPrice` (from Bilet)

For **Bilet** (Tickets):
- `ticketId`
- `ticketPrice`
- `seatNumber`

## Implementation Plan

### Step 1: Add Essential Field Extractor Method
Create a helper that extracts only scalar fields from an object:

```csharp
private static JObject ExtractEssentialFields(JObject obj, string[] essentialFields)
{
    JObject result = new JObject();
    foreach (string fieldName in essentialFields)
    {
        JToken value = obj[fieldName];
        if (value != null && value.Type != JTokenType.Object && value.Type != JTokenType.Array)
        {
            result[fieldName] = value.DeepClone();
        }
    }
    return result;
}
```

### Step 2: Define Essential Field Lists
```csharp
private static readonly string[] ESSENTIAL_MARSHUT_FIELDS = 
    { "routeId", "startPoint", "endPoint", "travelTime" };
private static readonly string[] ESSENTIAL_BILET_FIELDS = 
    { "ticketId", "ticketPrice", "seatNumber", "routeId" };
private static readonly string[] ESSENTIAL_SALE_FIELDS = 
    { "saleId", "saleDate", "ticketSoldToUser", "ticketSoldToUserPhone", "ticketId" };
```

### Step 3: Modify Recursion Handling
Replace empty object returns with essential field extraction:

```csharp
// Line ~1025-1029 (current)
if (!currentlyProcessingRefs.Add(refValue)) {
    Log.Warn("Recursion detected for $ref = {0}. Extracting essential fields only.", refValue);
    
    // Try to determine object type and extract appropriate fields
    JObject cachedObj = _resolvedRefCache.ContainsKey(refValue) 
        ? _resolvedRefCache[refValue] 
        : null;
        
    if (cachedObj != null)
    {
        // Detect type and extract
        if (cachedObj["startPoint"] != null || cachedObj["endPoint"] != null)
            return ExtractEssentialFields(cachedObj, ESSENTIAL_MARSHUT_FIELDS);
        else if (cachedObj["ticketPrice"] != null)
            return ExtractEssentialFields(cachedObj, ESSENTIAL_BILET_FIELDS);
        else if (cachedObj["saleDate"] != null)
            return ExtractEssentialFields(cachedObj, ESSENTIAL_SALE_FIELDS);
    }
    
    return new JObject(); // Fallback
}
```

### Step 4: Apply Similar Fix to Line ~1002-1004
```csharp
if (!currentlyProcessingRefs.Add(originalId)) {
    Log.Warn("Recursion detected for $id = {0}. Extracting essential fields.", originalId);
    // Same extraction logic as above
}
```

## Files to Modify

1. **frmSalesStatistics.cs**
   - Add `ExtractEssentialFields` helper method
   - Add essential field constant arrays  
   - Modify recursion handling in `CleanAndTransformJsonToken` (2 locations)

2. **frmIncomeReport.cs**
   - Same changes (it has identical JSON processing code)

## Testing Plan

1. **Sales Statistics:**
   - Change date range
   - Verify route names show as "Start → End" instead of "Неизвестный маршрут"
   - Check pie chart labels

2. **Income Report:**
   - Apply filters
   - Verify PassengerName column shows actual names
   - Verify PassengerPhone shows phone numbers
   - Check RouteDescription shows route details

## Expected Results

### Before:
```
Route: Неизвестный маршрут 1
Passenger: 0нет
Phone: 0нет
```

### After:
```
Route: Вейнянка -> Фатина
Passenger: admin
Phone: +375333000000
```

## Rollback Plan

If the fix causes issues:
1. The changes are isolated to `CleanAndTransformJsonToken` method
2. Revert to returning `new JObject()` on recursion
3. Alternative: Increase recursion depth limit before cutting off

## Notes

- This preserves the recursion detection (prevents infinite loops)
- Only extracts scalar values (no nested objects/arrays)
- Minimal performance impact (simple field extraction)
- Maintains backward compatibility with existing XML parsing code
