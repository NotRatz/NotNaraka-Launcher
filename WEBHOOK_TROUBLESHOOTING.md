# Webhook Troubleshooting Guide

## Changes Made

I've fixed several critical issues that were preventing webhooks from firing on remote devices:

### 1. **Hardcoded Webhook Now Takes Priority**
   - Previously, `GetWebhookUrl()` didn't check the hardcoded webhook URL at all
   - Now the hardcoded webhook URL is **always tried first** as the primary method
   - Falls back to file-based configuration only if hardcoded fails

### 2. **Comprehensive Logging Added**
   - All webhook attempts are now logged to `scan_log.txt`
   - Startup diagnostics log webhook configuration status
   - Each webhook send attempt logs:
     - URL being used (first 50 chars for security)
     - HTTP response status
     - Payload size
     - Success/failure reasons

### 3. **Improved Error Handling**
   - Exceptions no longer fail silently
   - All errors written to `webhook_error.txt` in the app directory
   - HTTP timeout increased to 30 seconds to handle slow connections

### 4. **Better Fallback Logic**
   - Primary: Hardcoded webhook URL
   - Fallback 1: Environment variables
   - Fallback 2: `discord_oauth.json`
   - Fallback 3: `webhook.txt`, `discord_webhook.secret`, etc.

## How to Diagnose Webhook Issues on Remote Devices

### Step 1: Check the Log Files

After running the app on a remote device, check these files in the app directory:

1. **`scan_log.txt`** - Contains detailed execution logs
   - Look for "Hardcoded webhook URL: VALID" or "INVALID"
   - Check for "Webhook POST succeeded" or error messages
   - Search for "SendWebhook" to see all webhook attempts

2. **`webhook_error.txt`** - Contains error details if webhook fails
   - Shows HTTP status codes and Discord API error messages

### Step 2: Verify Hardcoded Webhook URL

In `MainWindow.xaml.cs`, line ~30:
```csharp
private const string HardcodedWebhookUrl = "https://discord.com/api/webhooks/...";
```

Make sure:
- ? URL starts with `https://discord.com/api/webhooks/`
- ? URL contains both webhook ID and token
- ? URL has no trailing spaces or quotes
- ? Webhook is not deleted or rate-limited in Discord

### Step 3: Test Webhook Manually

You can test the webhook URL from command line on the remote device:

**PowerShell:**
```powershell
$url = "YOUR_WEBHOOK_URL_HERE"
$payload = '{"username":"Test","content":"Test from ' + $env:COMPUTERNAME + '"}'
Invoke-WebRequest -Uri $url -Method Post -Body $payload -ContentType "application/json"
```

**Expected Response:** HTTP 200 or 204 (success)

### Step 4: Check Network Connectivity

On the remote device:
```powershell
# Test DNS resolution
Resolve-DnsName discord.com

# Test HTTPS connectivity
Test-NetConnection -ComputerName discord.com -Port 443

# Check if firewall is blocking
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*Discord*"}
```

### Step 5: Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| "Webhook URL invalid" | Malformed URL | Verify URL format in code |
| "No webhook sent" | All methods failed | Check `scan_log.txt` for which method failed |
| HTTP 400 | Invalid JSON payload | Check `webhook_error.txt` for Discord error message |
| HTTP 401 | Invalid webhook token | Webhook may be deleted - create new one |
| HTTP 404 | Webhook doesn't exist | Verify webhook wasn't deleted in Discord |
| HTTP 429 | Rate limited | Wait 1 hour or use different webhook |
| Timeout | Network/firewall blocking | Check firewall rules and proxy settings |
| No log file | App crashed early | Check Windows Event Viewer |

## Testing on Your Device vs Remote Device

### On Your Device (Works) ?
- Check `scan_log.txt` to see which webhook method succeeded
- This tells you which configuration method is working for you

### On Remote Device (Doesn't Work) ?
1. Copy the **same executable** to remote device
2. Run the app once completely
3. Retrieve these files:
   - `scan_log.txt`
   - `webhook_error.txt` (if exists)
4. Compare logs to identify difference

## Webhook Configuration Priority (in order)

1. **Hardcoded URL in code** ? Most reliable for distributed apps
2. Environment variable: `DISCORD_WEBHOOK_URL`
3. File: `discord_oauth.json` in app directory
4. File: `webhook.txt` in app directory
5. File: `discord_webhook.secret` in app directory

## Quick Test

To verify the fix works, compile the app and check `scan_log.txt` immediately after startup. You should see:

```
=== APPLICATION STARTUP ===
...
Hardcoded webhook URL: VALID
Hardcoded webhook URL (first 50 chars): https://discord.com/api/webhooks/14070893632377...
...
=== STARTUP COMPLETE ===
```

Then after a scan completes:
```
Attempting webhook: hard-coded URL (primary method)
SendWebhookIfConfigured: preparing webhook to: https://discord.com/api/webhooks/14070893632377...
...
Webhook POST succeeded (multipart): OK
```

## Additional Recommendations

### For Maximum Reliability:

1. **Use hardcoded webhook** for distributed builds (already done)
2. **Add webhook health check** at startup to warn users if webhook is unreachable
3. **Consider Discord webhook limits:**
   - Max 30 requests per minute per webhook
   - Max 5MB per request
   - Max 10 embeds per request

### Security Note:

The hardcoded webhook URL in your compiled .exe can be extracted by reverse engineering. Consider:
- Creating a separate webhook for each deployment/tournament
- Deleting/rotating webhooks after events
- Using webhook permissions in Discord to limit abuse

## Need More Help?

If webhooks still don't work on remote devices after these changes:

1. ? Verify the app is actually running (check Task Manager)
2. ? Confirm scan completes (check DETECTION_REPORT.txt is created)
3. ? Send me both `scan_log.txt` and `webhook_error.txt` from the failing device
4. ? Compare timestamps between local (working) and remote (failing) logs
