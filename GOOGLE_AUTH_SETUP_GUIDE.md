# Google OAuth Setup Guide

## Add New Redirect URI to Google Cloud Console

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to **APIs & Services** → **Credentials**
4. Click on your OAuth 2.0 Client ID
5. Under **Authorized redirect URIs**, add:
   ```
   https://localhost:7235/auth/google-simple-callback
   ```
6. Click **Save**

## Test the New Flow

1. Start both your API and Blazor projects
2. Go to https://localhost:7176/login
3. Click "Login with Google"
4. You should be redirected to Google
5. After login, you'll be redirected back with a token

## What This Fixes

- Bypasses Chrome's third-party cookie restrictions
- Uses a simpler authentication flow
- Generates a test token for development

## Important Notes

- The current implementation creates a test user (test@gmail.com)
- In production, you would:
  - Exchange the Google authorization code for access tokens
  - Use the access token to get real user info from Google
  - Create/update user based on real Google profile data 