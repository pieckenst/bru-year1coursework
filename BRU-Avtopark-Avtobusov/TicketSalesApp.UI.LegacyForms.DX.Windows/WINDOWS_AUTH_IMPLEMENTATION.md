# Windows Authentication Implementation for WinForms

## Overview
Implemented legacy Windows authentication for the WinForms login form using the classic Windows XP/Vista-style credential dialog (`CredUI.dll`), compatible with .NET Framework 4.0.

## Files Created/Modified

### 1. **WindowsCredentialHelper.cs** (NEW)
- Classic Windows credential dialog wrapper using P/Invoke
- Uses `CredUIPromptForCredentials` from `credui.dll` (Windows XP era API)
- Compatible with .NET Framework 4.0
- No modern dependencies (unlike Avalonia which uses Vanara)
- Returns domain, username, and password as a Tuple

**Key Features:**
- Shows the classic Windows credential dialog
- Parses domain\username and username@domain formats
- Handles user cancellation gracefully
- No persistence of credentials (DO_NOT_PERSIST flag)

### 2. **frmLogin.cs** (MODIFIED)
Added Windows authentication flow:

#### New Method: `WindowsAuthLoginAsync()`
```csharp
- Shows credential dialog via WindowsCredentialHelper
- Creates HttpClient with NetworkCredential
- Calls /api/auth/windows-login endpoint
- Handles account linking requirement (directs to Avalonia app)
- Processes JWT token and authenticates user
- Full error handling and logging
```

#### New Event Handler: `btnWindowsAuth_Click()`
```csharp
- Triggers WindowsAuthLoginAsync on button click
```

### 3. **frmLogin.Designer.cs** (MODIFIED)
- Added `btnWindowsAuth` button to password panel
- Position: Below "Вернуться к QR-коду" button
- Text: "Войти через Windows"

## Authentication Flow

1. **User clicks "Войти через Windows"**
2. **Classic Windows credential dialog appears** (XP/Vista style)
3. **User enters domain\username and password**
4. **Credentials sent to backend** via `GET /api/auth/windows-login` with NetworkCredential
5. **Backend validates** via Windows authentication
6. **JWT token returned** with user role and linking status
7. **Account linking check:**
   - If `does_windows_account_need_linking = true`:
     - Show message directing user to Avalonia app
     - Login cancelled (account linking only available in Avalonia)
   - Else:
     - Token stored in ApiClientService
     - User authenticated and logged in

## Differences from Avalonia Implementation

| Feature | Avalonia | WinForms |
|---------|----------|----------|
| Credential Dialog | Modern Windows 10/11 UI via Vanara | Classic XP/Vista UI via CredUI.dll |
| Account Linking | Full UI flow with dialogs | Message directing to Avalonia |
| API | `CredUIPromptForWindowsCredentials` | `CredUIPromptForCredentials` |
| Dependencies | Vanara.PInvoke | System.Runtime.InteropServices only |
| .NET Version | .NET 6+ | .NET Framework 4.0 |

## Why Classic Dialog?

The WinForms app targets .NET Framework 4.0 and cannot use:
- Modern Windows 10 credential APIs (require newer Windows SDK)
- Vanara.PInvoke packages (require .NET Standard 2.0+)
- Windows Runtime (WinRT) APIs

The classic `CredUIPromptForCredentials` API from `credui.dll`:
- ✅ Available since Windows XP
- ✅ Works with .NET Framework 4.0
- ✅ No external dependencies
- ✅ P/Invoke compatible
- ✅ Shows familiar credential dialog

## Testing

1. Run the WinForms application
2. Click "Войти через Windows" button
3. Enter valid Windows credentials (domain\username)
4. If account is linked → Login succeeds
5. If account is not linked → Message shown directing to Avalonia app

## Security Notes

- Credentials are **not** persisted (DO_NOT_PERSIST flag)
- Password is securely cleared after use
- NetworkCredential used for Windows authentication
- JWT token follows same security as regular login
- All authentication flows are logged

## Backend Requirements

The backend `AuthController` must have:
```csharp
[Route("windows-login")]
[Authorize(AuthenticationSchemes = "Windows")]
[HttpGet]
public async Task<IActionResult> WindowsLogin()
```

This endpoint is already implemented and tested with the Avalonia app.
