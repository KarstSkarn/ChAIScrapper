# ChAIScrapper Chromium Version 4.1
*By KarstSkarn - https://karstskarn.carrd.co*

*If you liked it you can support me on https://ko-fi.com/karstskarn*

*It motivates me for this and many other projects!*

Demo/Assistance Discord: https://discord.com/invite/d9rNwkerZw

<img width="802" height="482" alt="maincapture" src="https://github.com/KarstSkarn/ChAIScrapper/blob/main/maincapture.png?raw=true" />

### Disclaimer
This program has been created for experimental and demonstrative purposes regarding the functionalities that AI applications, such as CharacterAI, could have in an environment like Discord.

Under no circumstances am I related to or responsible for the misuse of this program or any consequences that may arise from it.

The program respects and does not interfere in any way with the NSFW content filters that CharacterAI has in place.

The use of this program and any potential liabilities are the sole responsibility of the user.

### Readme Index
- [Setup Guide](#setup-guide)
- [Starting the Service](#common-faq)
- [Common FAQ](#file-security-check)
- [Bot Commands](#bot-commands)

### 4.1 Features
* The bot now reacts to Discord message reactions!  
* The mentions (@username) function now always works.  
* YouTube Virtual Videos AI Watch can now watch videos in virtually any language and even use YouTube’s automatic subtitles.  
* You can set a favorite language for YouTube videos, so it will prioritize watching them in that language.  
* You can now define how often the bot will react to a video.  
* Fixed bugs and crashes.  
* Major optimizations.  

**Note:** The setup steps are exactly the same as in the older 4.0 version!

#### 4.0 Features
* An easy-to-setup and brand-new GUI!  
* Possibility to talk to the bots using DMs.  
* YouTube Virtual Videos AI Watch fully functional.  
* It now keeps the original CharacterAI text format emphasis (italic, bold...)  
* Possibility to have a list of ignored users (useful for trolling or to make two bots stay together in the same channel without causing an endless loop of replies)  
* Made easy to host multiple bots on the same computer!  
* Bots now detect if they are on the wrong Discord servers and automatically leave them!  
* Major optimizations and bug fixes.  

# Setup Guide
The setup of ChAIScrapper requires the creation of a Discord bot, which is the one that will be used by your executable.  
The customization of parameters has been made super easy in version 4.0, so it's intuitive and allows you to save and switch between different configurations or bots.  
Note: **ChAIScrapper Chromium 4.0 no longer requires Chrome installed** — it uses its own standalone Chromium executable.

### Create a Discord Bot  
Due to its operation, ChAIScrapper requires a Discord bot to be created **exclusively** for its use. This is a simple task that **any Discord user** can perform through the Discord Developer Portal.  

https://discord.com/developers/applications  

You don't need to think too much about what profile image or username to give the bot, because ChAIScrapper automatically changes the bot's nickname on the server and its profile image depending on the Character AI character that is running.  

Once you've created your Discord bot, get the Application/Bot Token and store it for later use in a safe file.  

Note: **Never share your Discord Bot token with anyone**  

### Add permissions to the Discord Bot  
In the Discord Developer Portal, under the "Installation" category and the "Guild Install" tab, click and choose "Bot". Then add the following permissions:  
`Manage Messages, Read Message History, Send Messages, Attach Files, View Channels`  

Finally, go to the "Bot" tab in the Discord Developer Portal and scroll all the way down. Under the "Privileged Gateway Intents" category, enable the following intent options:  
`PRESENCE INTENT - SERVER MEMBERS INTENT - MESSAGE CONTENT INTENT`  

### Invite the Bot to your Discord  
Now you can invite your newly created Discord bot to the server where you want to use it. Due to how ChAIScrapper works, the bot cannot run on more than one server at a time, so make sure it is the Discord server where you really want to have it.  

Also, make sure that the bot, either through roles or direct assignment, has sufficient permissions to read and write messages in channels and to see all channels and users of that server (this is managed as if it were a normal user).

### Configure the program
In this version, this step is made super easy!

Open the program `ChAIScrapperV4.0.exe`. Go to the 'Config' tab.  
There you have all the options and customizations.

<img width="802" height="482" alt="maincapture" src="https://github.com/KarstSkarn/ChAIScrapper/blob/main/setupguide.png?raw=true" />

1. First, copy and paste your Discord bot token into the **Discord Bot Token** textbox.  
2. By right-clicking on the Discord text channel, get the **Channel ID** and paste it into the **Channel ID** textbox.  
   (You may have to enable **Developer Mode** in your Discord settings for this option to appear!)  
3. By right-clicking on the Discord server name, copy the **Discord Server ID**  
   (This helps prevent others from accidentally or intentionally adding your bot to their servers and causing a mess...)  
   and paste it into the **Discord Server ID** textbox.  
4. Paste your desired **CharacterAI Chat URL** into the **ChAI URL** textbox.  
   (This is the URL of the chat you want to use.)  
5. Save your configuration!  
   This way, every time the program starts, it will automatically load the last configuration used,  
   so you won’t have to repeat these steps anymore!  
   Also, you’ll be able to switch between configurations or bots really quickly!

## Starting the Service
1. Go to the 'Control' tab and always click **"Launch Chromium"** first.  
   Wait until Chromium has launched, and log in to your CharacterAI / Google account if you want to use a specific account.  
   (Especially useful if you have **ChAI+**!)  
2. Click **"Run"** and wait a few seconds until everything sets up automatically!  
3. Enjoy!

<img width="802" height="482" alt="maincapture" src="https://github.com/KarstSkarn/ChAIScrapper/blob/main/launchguide.png?raw=true" />

## Hosting multiple bots in the same computer
This version is specially designed to make it easy to host multiple bots on the same computer.  
Just take note that each bot will require a different token, so you will have to create multiple Discord bots for that.

In order to host multiple bots, just copy the entire ChAIScrapper folder (including Chromium and all its contents).  
Open the executable and change the URL, Channel ID (all the stuff you want to be different) to your desired values and,  
more importantly, set a different Chromium port in the config tab. By default, Chromium uses **9222**, as stated in the  
executable. By switching to **9223**, **9224**... and so on, it lets you host multiple bot instances.

Then launch each bot as normal (each one will have its own Chromium window) and keep the executables open!

## Adding Administrative Privileges  
To add users with administrative privileges, simply copy their Discord ID (not their name or the "@", just the long numerical ID) by right-clicking on their profile.  
Then, add that ID in the **Privileges** tab of the executable.

This list is saved automatically whenever you save your configuration!

## Adding Users to the Ignore List  
By default, the bot ignores itself and this behavior cannot be changed (for obvious reasons—otherwise it would create an endless loop).

You can add other users or bots for the bot to ignore by adding their Discord IDs to the **Ignore List** within the **Privileges** tab.

This list is also saved automatically when you save your configuration!

## Changing How the Bot Changes Its Name and Profile Picture  
You can disable the automatic name and/or profile picture changes in the **Config** tab of the executable.  
This setting only applies when the bot starts, so you may need to stop and restart the bot for it to take effect!

## Editing the Initial Bot Briefing  
You can edit the initial bot briefing in the executable’s **Config** tab.  
This briefing is the message sent to the bot at startup if desired.  
It helps set up the environment and context for the bot’s interactions.  

This is also saved in your configuration for your convenience!

## DMs Mechanics  
The bot gives absolute priority to DMs. When it is answering DMs, it won’t respond in the main text channel, so it’s not possible for the bot to answer 100% on both sides at the same time  
(We humans can’t do that either, anyway).

The only time the bot won’t be able to answer DMs is when it is watching YouTube videos.

**NOTE: Due to Discord’s policies, the bot won’t respond to your DMs until you’ve sent a message in the same text channel where the bot is currently active.**  
This is to prevent bots from massively spamming users.  
Once you have messaged the bot once in the text channel, it will answer your DMs.

### Hint  
It works better with the "Meow" AI model since it gives faster and shorter answers, making the conversation feel more realistic.

## Changing preferred YouTube Videos Virtual Watch language
By default, the bot will prefer to watch videos in the language defined in the **Code Language** textbox of the Config section. By default, this is `"en"` for English.

If you want to change it to your preferred language, you can check the two-letter code associated with that language in the following standardization table:

[ISO 639 Language Codes](https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes)

## Common FAQ

* **Why do I need to keep Chromium open while the bot is running?**

The program uses web-scraping techniques to automate and enhance certain functions of the Character AI page. For this, you need to open Chromium in a special debug mode that allows the program to run.

* **Do I have to keep the Chromium window active?**

Not anymore! You can minimize it or move it to another screen / hide it somewhere on your desktop.  
The only important thing is **not to close it**.  

Version 4.0 launches Chromium in a special way that disables the automatic resource starvation it used to suffer when minimized, which previously caused shortened answers and other malfunctions!

* **Which Character AI account will the bot use?**

Since it uses the standalone Chromium executable, it will use whichever account you are currently logged into in that browser and page.  
This includes accounts with ChAI+ (which may greatly benefit from the enhanced answer speed!).

Take this into consideration before starting the service, since it may use a bot where you already have a chat and could leak some answers to your Discord channel.

* **How many people can speak to the bot at once?**

It has virtually no limitations other than the amount of data that the Character AI bot system itself can handle in a single message.

* **Does the bot recognize different users while using ChAIScrapper?**

Yes, the bot receives specially designed prompts that make it very easy for it to recognize different Discord users and differentiate the various things they send to the bot.

* **Do I need to create a special Character AI character for it?**

Strictly no; many vanilla characters (such as Ame by @renai) will easily follow the Discord flow and can even natively tag users or ping @everyone.

That said, it is highly recommended that once you decide you enjoy this program, you create your own Character AI character and detail how you want it to behave in your Discord channel (things like pings, tags, style of answers, etc.).

* **How does the voice notes feature work?**

The voice notes trick the CharacterAI page into playing just once the last answer the bot gave you, and automatically record and trim it to send via Discord.  
Keep in mind that unless you set up custom Virtual Cables on your computer, the voice note will play through your computer's default output device.

If you’re playing other sounds through that device, they will be recorded too and may interfere with the voice note.  
By default, ChAIScrapper captures audio from the default output device, but this is a feature you can change relatively easily in the program’s code.

* **How does the YouTube Virtual Watch feature work?**

This feature "allows" the bot to "watch and enjoy" YouTube videos. In this version, ChAIScrapper attempts to fetch and scrape the YouTube URL (without needing Chrome for this) and retrieves its subtitles and all available metadata such as YouTube username, video description, etc.

Then a sequence of "watching" the video begins while you can still speak to the bot, and it will answer each of you separately as normal.  
The bot breaks the video into 15-second segments and uses the subtitles of those segments as a very detailed and structured prompt to keep track of the video it’s currently watching.

In a private experimental version, I even implemented code that grabs some frames of the video every 15 seconds and sends them via an API to an image recognition service, which returns detailed descriptions of those frames to complement the subtitles.  
This way, the bot reacts very realistically to the submitted video. Sadly, these image detection services have limited requests and require me to share my private API tokens.  
But I leave the concept here in case any of you want to implement it yourselves.

Note: **Very, very long videos can disorient your bot.** For example, hours-long movies or videos with abstract subtitles or audio may confuse your bot.

Note #2: **If the video has no subtitles or enough metadata, the bot will refuse to "watch" it.**

* **How does the automatic idle function work?**

ChAIScrapper has a function that enables the bot to detect when the Discord text channel goes silent and automatically feed itself by voluntarily writing.

This can result in a wide range of messages from the bot. We even experienced "needy bots" that started pinging @everyone and plainly asking for attention because they got bored.

Idle interactions are random but have a timing range that you can change in the `ChAISData.xml` file.  
By default, the minimum time between idle interactions is 15 minutes, and the maximum is 75 minutes.

Note: **If you set both numbers to 0 in the executable’s Config tab, this function will be entirely disabled and the bot won’t be able to automatically write when it’s idle.**

## Bot Commands  
All bot commands start with the **"!"** character.

* **!help**  
  Displays the list of commands.

* **!ping**  
  The bot responds with "Pong!" for debugging and online testing purposes.

* **!refresh** *Administrative Lock*  
  Forces the webpage to refresh (very useful when Character AI gets stuck while generating an answer!).

* **!reset** *Administrative Lock*  
  Resets most of the ChAIScrapper service from the Discord text channel if the bot malfunctions.

  **Note:** The !reset command has a 5-minute cooldown. Since the bot also scraps the Character AI character profile image and uses it as its own, Discord may reject connections if this happens too often!

* **!audio** *Administrative Lock*  
  Enables or disables the voice notes system.

* **!ytwatch [URL]**  
  Starts the bot watching any YouTube video.  
  Example: `!ytwatch https://www.youtube.com/watch?v=dQw4w9WgXcQ`

* **!ytstop**  
  Completely stops and ends the playback of any YouTube video the bot is watching.

* **!ytpause**  
  Pauses the current video playback without ending it, so the bot can resume later.

* **!ytresume**  
  Resumes playback of any paused video.

* **!lock**  
  Enables or disables the lock on commands marked with *Administrative Lock*.  
  When enabled, only administrative users can use those commands.

* **!dm**  
  Enables or disables the Direct Messages feature.  
  This can only be changed by users with administrative privileges.

* **//** Ignore / Exclusive  
  This prefix is defined in the **Config** tab and determines which messages the bot ignores or listens to exclusively.  
  By default, messages starting with "//" are ignored, but you can change this behavior and prefix to whatever you want (e.g., the bot only answering messages starting with "/").
------------

## Repository Disclaimer
Due to the size of Chromium's dll files they are not included in the repository. Download a standalone Chromium version and put it into the Chromium folder if you are going to edit the code.

------------
By KarstSkarn 2025
[![CC Licence](https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png "CC Licence")](https://creativecommons.org/licenses/by-nc-sa/4.0/ "CC Licence")
