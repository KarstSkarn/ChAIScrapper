# ChAIScrapper 1.0
*By KarstSkarn - https://karstskarn.carrd.co*

### Disclaimer
This program has been created for experimental and demonstrative purposes of the functionalities that the application of AI's such as CharacterAI's could have in an environment such as Discord.

Under no circumstances I am related or responsible for the misuse of this program or the consequences that could be derived from it.

The program respects and does not interfere in any way with the “NSFW” content filters that Character AI has in place.

The use of this program and its possible liabilities are the sole responsibility of the user.

### Readme Index
- [Setup Guide](#setup-guide)
- [Starting the Service](#common-faq)
- [Common FAQ](#file-security-check)
- [Bot Commands](#bot-commands)

# Setup Guide
The setup of ChAIScrapper requires the user to customize some parameters for its operation. These steps are simple and require no prior knowledge of any kind.

Note: **ChAIScrapper requires Chrome Installed**

### Create a Discord Bot
Due to its operation ChAIScrapper requires a Discord bot to be created **exclusively** for its operation. This is a simple task that **any Discord user** can perform through the Discord developer portal.

https://discord.com/developers/applications

You don't need to think too much about what profile image and username to give to the bot because ChAIScrapper automatically changes the bot's nickname on the server and its profile image depending on the Character AI character that is running.

Once you created your Discord bot get the Application/Bot Token and store it for later usage in a safe file.

Note: **Never share your Discord Bot token with anyone**

### Invite the Bot to your Discord
Now you can invite your newly created Discord bot to the server where you want to use it. Due to how ChAIScrapper works the bot cannot run on more than one server at a time so make sure it is the Discord server where you really want to have it.

Also make sure that the bot either through roles or personally has sufficient permissions to read and write messages in channels and see all channels and users of that server (This is managed as if it were a normal user).

### Configure the file "ChAISData.xml"
Now the only thing left to do is to open with notepad the ChAISData.xml file.

Put the Discord bot token in the markdown
`<DISCORDTOKEN>YOUR_DISCORD_TOKEN_HERE</DISCORDTOKEN>` section.

Then in your Discord right click on the text channel you want the bot to read and write on. In the drop-down menu select the “Copy Channel ID” option.
Paste the Discord Channel ID in the “Copy Channel ID” section.
`<DISCORDCHANNELID>0</DISCORDCHANNELID>`

Finally right click on the bot in the list of users on the right and copy its user ID. Note that it has to be a number and not the username or nickname. Once you have done this paste the Discord bot's User ID into the
`<DISCORDBOTUID>0</DISCORDBOTUID>`

Finally close that file saving changes!

Note: **You maybe need to enable Discord Developer Features in your Discord Options menu to be able to copy the channel or user ID**

Note #2: **For if some reason you somehow messed badly with that file and causes crashes or any sort of error you can just delete it and open ChAIScrapper.exe and will automatically create a new blank file for you to fill.**

### Set your Chrome user data path
Open with notepad the file "ChAIScrapperLauncher.bat" and modify the path "C:\Users\YOUR_USER_NAME\AppData\Local\Google\Chrome\User Data" with your Windows username.
Also ensure that the Chrome path corresponds with where your local Chrome installation is located. Finally save changes and close.

Note: **Ensure that "ChAIScrapperLauncher.bat" is in the same directory than "ChAIScrapper.exe"!**

## Starting the Service

To start the bot, follow a simple procedure.

* **Close all the Chrome tabs you have open** ( Don't worry, once the bot is started you can open them again! )
* Execute the file **ChAIScrapperLauncher.bat** not the .exe directly! 
 * A Chrome tab should open and automatically redirect to the CharacterAI URL listed in the “ChAISData.xml” file.
* At the same time, the ChAIScrapper service window should open in console mode.
* It will take a few seconds to start the service in Discord and wait until the URL has stabilized enough to capture and send answers.
* Once the bot has responded to the “initial briefing” that the service makes it is ready to be used!

## Common FAQ

* **Why I need to keep Chrome open while the bot is running?**

The program uses web-scrapping techniques to automate and enhance certain functions of the Character AI page. For this you need to open Chrome in a special debug mode in which you can run the program. This debug mode also requires Chrome to be re-started that's why its very important to close all tabs before starting the service.

In my case I have a small computer with which I host all kinds of programs in which I have a few tricks done so that Chrome in conjunction with multiple libraries can be opened in “headless” mode in a separate session without having to interfere with the user's main Chrome.

Unfortunately that requires severe extra steps in its installation and I didn't see it convenient to add it in this version in a definitive way.

* **I must keep the Chrome window active?**

Sadly as I commented in the previous answer the alternative is too intensive for the average user to be deployed in this version. **It is recommended** to keep the bot Chrome Tab opened despite it can work too with the tab not being visible. The only consideration is that Chrome reduces the resources and refresh rate of the tabs that aren't active and this may result in a noticeable reduction of the bot's answer speed.

A little trick is to keep that Chrome tab into another screen so you can use the rest of the screens freely since in that way Chrome will not reduce the resources that the bot's tab is using.

* **Which Character AI account the bot will use?**

Since it uses your installed version of Chrome it will use any account you have currently logged in in that page. This includes those with ChAI+ (Which may benefit greatly of the enhanced answer speed!).

Take this into consideration before starting the service since it may use a bot where you already have a chat and leak some answers to your Discord Channel.

* **How many people can speak to the bot at once?**

It has virtually no limitation other than the amount of data that the Character AI bot system itself can handle in a single message.

* **The bot recognizes different users while using ChAIScrapper?**

Yes, the bot receives specially designed prompts that make it very easy for it to recognize different Discord users and differentiate the different things that different users may send to the bot.

* **I must do a special Character AI character for it?**

Strictly no; a lot of vanilla characters (Such as Ame by @renai) will easily follow the Discord flow and be able to even natively tag users or ping @everyone. 

Despite that it is heavily recommended to once you decided you enjoy this program to create your own Character AI character and in a detailed manner expose how you want it to behave in your Discord channel (Things such pings, tags, style of answers...)

* **How does the voice notes feature works?**

The voice notes just trick the CharacterAI page to play just once the last answer the bot gave you and automatically record and trim it in order to send it using Discord. Keep in mind that this means that unless you do some Virtual Cables customized setup on your computer it will automatically play the voice note in your computer's default output device.

If you are playing other sounds through that device they will be recorded too and may interfer onto the voice note. By default ChAIScrapper captures the audio from the default output device but this is a feature you can change relatively easily in the program's code.

* **How does the YT Virtual Watch feature works?**

This feature "allows" the bot to "watch and enjoy" YouTube videos. In this version the program ChAIScrapper attempts to fetch and scrap the YouTube URL (Without needing Chrome for this) and fetches its subtitles and all available metadata such as YouTube user name, Video description...

Then a sequence of "watching" the video begins while you all can still speak to the bot and it will answer to you all separately as normal. The bot breaks the video in 15 seconds segments and uses the subtitles of those 15 seconds as a very detailed and structurated prompt in order for it to be able to keep track of the video its currently watching.

In the private experimental version I even implemented some code that grabs some frames of the video every 15 seconds and sends them using an API to a Image Recognition service and returns the detailed description of those frames to complement the video subtitles. In this way the bot reacts very realistically to the submitted video but sadly those Image Detection servicions have limited requests and require me to share my private API Tokens. But I leave here the concept just in case any of you want to implement it yourselves.

Note: **Very very long videos can disorient your bot.** For example hours long movies or videos with abstract subtitles or audio can just disorient your bot.

Note #2: **If the video has not subtitles or enough metadata the bot will refuse to "see" it**.

* **How the automatic idle function works?**

The program ChAIScrapper has a function that enables the bot to detect when the Discord text channel goes silent and automatically feed itself and voluntarily write.

This may result in a very wide range of messages from the bot. We even experienced "needy bots" that started pinging @everyone and plainly asking for attention because they got bored. 

The idle interactions are random but have a timing range which you can change in the file ChAISData.xml. By default the minimum time between idle interactions will be 15 minutes and the maximum will be 75 minutes. 

Note: **If you set both numbers to 0 in the file ChAISData.xml this function will be entirely disabled and the bot won't be able to automatically write when its idle.**

## Bot Commands
All bot commands start by the **"!"** character. The only exception is starting a Discord message with **"//"** which results in the bot ignoring that message.

Example:
*// The bot can't read this and won't react to it at all* 

* **!ping**
This command results in the Discord Bot service answering "Pong!" just for debugging and online testing purposes.

* **!character [URL]**
This command allows you to change the ChAIScrapper current character. Once its accepted it will take approximately one minute to completely restart working with that character without any need from the host to do anything.
Example: *!character https://character.ai/chat/xuHtpVIs5Pl6XGvIjOKx-8jsIOCm5xi_iG4H19gxOxA*

* **!reset**
For if some reason the bot seems to malfunction you can reset most of the ChAIScrapper service from the Discord text channel itself using this command.

Note: **Both the !reset and the !character commands have a 5 minute cooldown. Since the bot scraps also the Character AI character profile image and uses it as its own this makes Discord prone to rejecting connection if this were to happen too often!**

* **!fb / !feedback [1-4]**
This allows the Discord channel users to give feedback to the bot as its naturally done from the Character AI's webpage. This is very helpful to train it into more accurate answers.
Example: *!fb 4*
This will set a 4 star rating into the bot last answer.

* **!audio**
This enables/disables the voice notes system.

* **!watch [URL]**
This makes the bot start watching any YouTube video.
Example: *!watch https://www.youtube.com/watch?v=dQw4w9WgXcQ*

* **!stop**
This completely stops and ends the reproduction of any YouTube video the bot is currently watching.

* **!pause**
This pauses the current reproduction of the video but doesn't end it so the bot can resume it later.

* **!resume**
This resumes the reproduction of any video that has been paused.

------------
By KarstSkarn 2024
[![CC Licence](https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png "CC Licence")](https://creativecommons.org/licenses/by-nc-sa/4.0/ "CC Licence")