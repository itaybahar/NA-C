# Google Authentication Troubleshooting Guide

## Quick Checklist

1. **Both Projects Running**
   - [ ] API is running on `https://localhost:7235`
   - [ ] Blazor WebAssembly is running on `https://localhost:7176`

2. **Browser Settings**
   - [ ] Third-party cookies are NOT blocked
   - [ ] Try in Incognito/Private mode
   - [ ] Clear cookies for localhost and try again

3. **Test the Flow**
   - [ ] Click "Login with Google" button (don't enter URLs manually)
   - [ ] Complete Google login
   - [ ] Check browser console for errors
   - [ ] Check API console logs for errors

## Common Issues and Solutions

### 1. "The oauth state was missing or invalid"
**Cause**: Cookie/session state lost between requests
**Solutions**:
- Enable third-party cookies in browser
- Clear all cookies for localhost
- Try in a different browser
- Check that both API and client use HTTPS

### 2. "Token is missing"
**Cause**: API didn't generate/send JWT after Google login
**Solutions**:
- Check API logs for errors
- Verify Google callback is completing successfully
- Ensure JWT generation is working

### 3. "google-auth-failed"
**Cause**: Google authentication didn't complete
**Solutions**:
- Check Google Cloud Console settings
- Verify redirect URIs match exactly
- Check API logs for specific error

## Debug Mode

To enable detailed debugging, add these to your API's `AuthController.cs`:

```csharp
Console.WriteLine($"Google callback - Email: {email}");
Console.WriteLine($"Google callback - User exists: {user != null}");
Console.WriteLine($"Generated token length: {existingUserToken?.Length ?? 0}");
```

## Test Endpoints

1. **Test API Health**: https://localhost:7235/health
2. **Test Cookies**: https://localhost:7235/api/test-cookies
3. **Test Auth**: https://localhost:7235/auth/ping

## Browser Cookie Settings

### Chrome
1. Settings → Privacy and security → Cookies
2. Allow all cookies OR
3. Add [*.]localhost to "Sites that can always use cookies"

### Firefox
1. Settings → Privacy & Security → Cookies
2. Standard protection OR
3. Manage Exceptions → Add https://localhost

### Edge
1. Settings → Cookies and site permissions
2. Allow sites to save cookies
3. Add localhost to allowed sites 