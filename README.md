# WabbaRush [![Latest Release](https://img.shields.io/github/v/release/ent3m/WabbaRush?colors=blue)](https://github.com/ent3m/WabbaRush/releases)

WabbaRush automates Wabbajack modlist downloads for free Nexus users.

## Features

| | |
| :--- | :--- |
| **Full Automation** | Loads your `.wabbajack` file (from your PC or the cloud) and downloads every mod completely unattended. No manual clicking required. |
| **Smart Resume** | Safely close the app at any time. It automatically scans your folder and picks up where you left off on the next download. |
| **Account Protection** | Automatically backs off if Nexus limits your requests, keeping your account safe from bans. |
| **Modlist Partitioning** | Split a massive modlist across multiple computers or accounts to finish even faster. |
| **Folder Management** | Optionally delete outdated archives and unrelated files from download folder. |
| **Customization** | Fine-tune download behaviors from within the app or via `settings.json`. |

## Usage

1. Select a download folder and a modlist.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/8906557e-a94a-43f1-a681-d1c672d51441">
  <img width="85%" alt="AppSetup" src="https://github.com/user-attachments/assets/06376dc2-4cb1-4562-a072-6dc09434bb2b">
</picture>

2. Customize download behaviors in settings.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/ccc75efe-6f99-4431-9002-faa0b9e58b59">
  <img width="85%" alt="AppSettings" src="https://github.com/user-attachments/assets/25ef11f6-040c-4c9e-a77d-71f8e840fccf">
</picture>

3. Login, click download, and let it run.
<img width="85%" alt="WabbaRushDemo" src="https://github.com/user-attachments/assets/b3bf5d60-4eee-4085-bf3e-1108467459c0" />

## Installation
- Download the latest [release](https://github.com/ent3m/WabbaRush/releases).
- Extract and run **WabbaRush.exe**.
- Requires Windows 10/11 + [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (pre-installed on most machines).

## Build it yourself

### Requirements
- Visual Studio 2026
- .NET desktop development workload
- Desktop development with C++ workload

```
git clone https://github.com/ent3m/WabbaRush.git
cd WabbajackDownloader
dotnet publish -c Release -r win-x64
```

The executable will be generated in `WabbajackDownloader\bin\Release\net10.0\win-x64\publish\WabbaRush.exe`.

## Disclaimer

### Nexus Mods policies
The use of automation is explicitly prohibited by Nexus Mods, as stated in their [Terms of Service](https://help.nexusmods.com/article/18-terms-of-service). Any attempts to automate downloads or related actions may result in account suspension or other enforcement actions by the service provider.

### Use at your own risk
This software is provided for educational, research, and personal use only. It is not officially affiliated, endorsed, or supported by Nexus Mods, Wabbajack, or any other third-party entities. The developer of this application does not endorse or promote any use that would violate the terms of service, policies, or legal rights of any third parties, including Nexus Mods or any other service providers. By using this software, you expressly acknowledge and agree to assume all risks associated with its use, including the possibility of legal action from third parties, and release the developer from any and all liability arising from such use.
