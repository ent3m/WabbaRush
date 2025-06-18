# Wabbajack Downloader
An automatic downloader for [Wabbajack](https://github.com/wabbajack-tools/wabbajack). It works by extracting the required mods from a wabbajack file and download them automatically so you don't have to manually click on **Slow Download** thousands of times. Works for all Nexus Mods accounts, free and premium.
# Usage
![Image](https://github.com/ent3m/WabbajackDownloader/blob/master/WabbajackDownloader/Assets/screenshot.png)
1. Select a *.wabbajack* file. A progress bar will appear showing how many mods will be downloaded.
2. Select a folder for downloads.
3. Login to your *Nexus Mods* account.<br>
After logging in, you will be taken to the *SkyUI* mod page.
Close the Nexus window once you are there.
4. **Optional:** Set the download limit and auto retry.<br>
*Download limit* is the maximum file size that will be downloaded. Default limit is **500MB**. Increase this number if you want the program to download large files.<br>
*Auto retry* automatically re-attempts the download upon failure. You may want to turn this **off** so that your account will not be flagged for excessive activities (see [disclaimer](#disclaimer)).
5. Click **Download** and wait. Mods are downloaded one by one until completed.<br>
Once finished, you can continue the setup process in the Wabbajack app.

*Note:* You can close the program at anytime. When you select the same wabbajack file and download folder, the program will attempt to resume downloads where it left off. However, since none of the settings are saved, you have to configure all the steps again.

# Installation
Download the latest [release](https://github.com/ent3m/WabbajackDownloader/releases).
Requires **Windows 10 x64** or newer.<br><br>
To build your own binaries, you will need to supply a *wabbajack.png*, *wabbajack.ico*, and *nexus.png* in */Assets*. You also need to specify a font in *App.axaml*. These were not included for copyright reasons.

# Disclaimer
This software is provided for educational, research, and personal use only. It is not officially affiliated, endorsed, or supported by Nexus Mods, Wabbajack, or any other third-party entities. The developer of this application does not endorse or promote any use that would violate the terms of service, policies, or legal rights of any third parties, including Nexus Mods or any other service providers.

#### Nexus Mods Policies:
The use of automation is explicitly prohibited by Nexus Mods, as stated in their [Terms of Service](https://help.nexusmods.com/article/18-terms-of-service). Any attempts to automate downloads or related actions may result in account suspension or other enforcement actions by the service provider.

#### Use at your own risks:
Users are solely responsible for ensuring that their use of this application complies with the terms of service of any third-party services involved, as well as with all applicable laws and regulations. By using this software, you expressly acknowledge and agree to assume all risks associated with its use, including the possibility of legal action from third parties, and release the developer from any and all liability arising from such use.
