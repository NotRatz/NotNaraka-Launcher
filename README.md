# RatzTweaks
**Disclaimer:** *This tool will interact with your computer or computer that this is ran on. I will always be willing to help for things that break, but ultimately am not responsible for any damage that this tool can cause. It's always recommended to make a back-up of your user data first.*
RatzTweaks is a compact Windows tweaks GUI built in PowerShell (Windows PowerShell 5.1). It applies recommended system and GPU tweaks and provides optional tweaks the user may choose.

This logs only minimal usage information via a Discord webhook. When a user runs the tool they will authenticate with Discord (OAuth2 identify) and the tool will send a small embed to a webhook notifying the owner of the username, user ID, and run time for support!

## Quick start

- Right-click `RatzTweaks.ps1` and choose "Run with PowerShell" (use Windows PowerShell 5.1).
- Click Start to apply main tweaks.
- Optionally select extra tweaks and click Apply Selected.

## Privacy & Data

- This version does NOT store or transmit IP addresses or system network information.
- Only the Discord username, user ID, and timestamp of when the tool was run are sent to the configured webhook.

## OAuth & Webhook

- The script uses Discord OAuth2 (identify) so the user can provide a Discord identity for the owner to thank.
- A webhook (configured by the script author) receives an embed containing the username, user ID, and timestamp.

## Requirements

- Windows PowerShell 5.1
- Run as Administrator for some tweaks

## License

MIT License
