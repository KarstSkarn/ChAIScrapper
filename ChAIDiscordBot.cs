using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Discord.Rest;
using System.Reflection.Metadata;
using Discord.Audio;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Discord.Interactions;
using System.Reflection;
using ChAIScrapperWF;
using ImageMagick;
using EmojiOne;

namespace ChAIScrapperWF
{
    public static class ChAIDiscordBot
    {
        private static readonly SemaphoreSlim RateLimitSemaphore = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentQueue<(ulong, string)> MessageQueue = new ConcurrentQueue<(ulong, string)>();
        private static readonly ConcurrentQueue<(ulong, string)> DirectMessageQueue = new ConcurrentQueue<(ulong, string)>();
        private static ConcurrentQueue<(ulong, string)> FileQueue = new ConcurrentQueue<(ulong, string)>();
        private static readonly TimeSpan RateLimitDelay = TimeSpan.FromMilliseconds(1000 / 5); // 5 messages per second
        public static DiscordSocketClient discordClient = null;
        //public static InteractionService _interactionService;
        //public static IServiceProvider _services;
        private static Process ffmpeg;

        public static PictureBox botPanelImage;
        public static Label DMCounterLabel;
        public static Label AIStatusLabel;
        public static Label ResponsesCounterLabel;
        public static ListBox ignoreListBox;
        public static TextBox characterURLTextBox;
        public static ChAIWF mainForm;

        public static async Task RunDiscordBotAsync(CancellationTokenSource cancellationToken)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            discordClient = new DiscordSocketClient(config);
            //_interactionService = new InteractionService(discordClient);

            discordClient.Log += LogAsync;
            discordClient.MessageReceived += MessageReceivedAsync;
            discordClient.ReactionAdded += ReactionAddedAsync;

            //await _interactionService.AddModuleAsync<SlashCommandsModule>(null);

            /*
            discordClient.Ready += async () =>
            {
                await _interactionService.RegisterCommandsGloballyAsync();
            };
            */

            discordClient.Ready += async () =>
            {
                Global.discordBotUserID = discordClient.CurrentUser.Id;

                lock (Global.lockInternalData)
                {
                    if (!Global.internalIgnoreList.Contains(Global.discordBotUserID))
                    {
                        Global.internalIgnoreList.Add(Global.discordBotUserID);
                        if (mainForm.InvokeRequired)
                        {
                            mainForm.Invoke(new Action(() =>
                            {
                                mainForm.ForceSaveCurrentData();
                            }));
                        }
                        else
                        {
                            mainForm.ForceSaveCurrentData();
                        }
                    }
                    if (!ignoreListBox.Items.Contains(Global.discordBotUserID + " (Bot Itself)"))
                    {
                        ignoreListBox.Items.Add(Global.discordBotUserID + " (Bot Itself)");
                    }
                }

                foreach (var guild in discordClient.Guilds)
                {
                    if (guild.Id != Global.discordServerID)
                    {
                        try
                        {
                            await guild.LeaveAsync();
                            ChAIO.WriteDiscord("Bot did leave the guild [ " + guild.Id + " / " + guild.Name + " ] because is not the main one!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error exiting from a server {guild.Name}: {ex.Message}");
                        }
                    }
                }
            };

            await discordClient.LoginAsync(TokenType.Bot, Global.discordBotToken);
            await discordClient.StartAsync();

            try
            {
                var processFileQueueTask = Task.Run(() => ProcessFileQueueAsync(discordClient, cancellationToken.Token), cancellationToken.Token);
                var processMessageQueueTask = Task.Run(() => ProcessMessageQueueAsync(discordClient, cancellationToken.Token), cancellationToken.Token);
                var processDirectMessageQueueTask = Task.Run(() => ProcessDirectMessagesQueueAsync(discordClient, cancellationToken.Token), cancellationToken.Token);
                var checkAndSendFeasibleAnswerTask = Task.Run(() => CheckAndSendFeasibleAnswerAsync(discordClient, cancellationToken.Token), cancellationToken.Token);

                while (!cancellationToken.Token.IsCancellationRequested)
                {
                    bool writtingState = false;
                    lock (Global.lockInternalData)
                    {
                        writtingState = Global.botWrittingState;
                    }
                    if (writtingState)
                    {
                        // Simulate typing in the default channel
                        if (Global.currentDMBuffer == null)
                        {
                            var defaultChannel = discordClient.GetChannel(Global.discordChannelID) as SocketTextChannel;
                            if (defaultChannel != null)
                            {
                                await defaultChannel.TriggerTypingAsync();
                                if (Global.botYTVirtualWatch && !Global.botYTPause)
                                {
                                    //SetBotStatus("Answering While Watching a YT Video...");
                                    SetBotStatus("Answering Discord Channel... (YT Video)");
                                }
                                else
                                {
                                    SetBotStatus("Answering Discord Channel...");
                                }
                            }
                        }
                        else // Simulate typing in the proper DM Chat
                        {
                            var user = discordClient.GetUser(Global.currentDMBuffer.ID);

                            if (user != null)
                            {
                                var dmChannel = await user.CreateDMChannelAsync();

                                if (dmChannel != null)
                                {
                                    await dmChannel.TriggerTypingAsync();
                                    SetBotStatus("Answering DM...");
                                }
                            }
                        }
                    }

                    await Task.Delay(500);
                }

                await Task.WhenAll(processDirectMessageQueueTask, processMessageQueueTask, checkAndSendFeasibleAnswerTask, processFileQueueTask);
            }
            catch (OperationCanceledException)
            {
                //
            }
            finally
            {
                await discordClient.StopAsync();
                await discordClient.LogoutAsync();
                //discordClient.Dispose();
            }
        }

        public static Task LogAsync(LogMessage log)
        {
            ChAIO.WriteDiscord(log.ToString());
            return Task.CompletedTask;
        }

        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            try
            {
                if (Global.internalIgnoreList.Contains(message.Author.Id))
                {
                    return;
                }

                string messageContent = RemoveNonBmpCharacters(message.Content);
                if (messageContent == null) { return; }

                // Check if the message is from a DM channel
                if (message.Channel is SocketDMChannel dmChannel)
                {
                    ChAIO.WriteDiscord("DM Received: " + messageContent);
                    ChAIO.WriteDiscord("DM From: " + message.Author.ToString());

                    if (Global.loadedData.ALLOWDMS || Global.internalAdminList.Contains(message.Author.Id))
                    {

                        if (messageContent.StartsWith("!"))
                        {
                            EnqueueDirectMessage(message.Author.Id, "> *You can't use commands on DMs!*");
                            return;
                        }

                        if (Global.botAudioMode)
                        {
                            EnqueueDirectMessage(message.Author.Id, "> *You can't use DMs while AI has the !audio function enabled!*");
                            return;
                        }

                        lock (Global.lockInternalData)
                        {
                            ChAIDataStructures.DMBuffer newDM = new ChAIDataStructures.DMBuffer();
                            newDM.AUTHOR = message.Author.ToString();

                            newDM.CONTENT = messageContent;

                            newDM.ID = message.Author.Id;

                            newDM.TIME = DateTime.Now;

                            Global.DMBufferList.Add(newDM);

                            if (DMCounterLabel.InvokeRequired)
                            {
                                DMCounterLabel.Invoke(new Action(() =>
                                {
                                    DMCounterLabel.Text = Global.DMBufferList.Count.ToString();
                                }));
                            }
                            else
                            {
                                DMCounterLabel.Text = Global.DMBufferList.Count.ToString();
                            }
                        }

                        if (Global.botYTVirtualWatch)
                        {
                            EnqueueDirectMessage(message.Author.Id, "> *" + Global.discordBotIAName + " is currently watching YT and won't answer until the current video ends...");
                        }
                    }
                    else
                    {
                        EnqueueDirectMessage(message.Author.Id, "> Sorry, *" + Global.discordBotIAName + " won't accept DMs right now! *(Closed by administrator)*");
                    }
                    return;
                }

                // Only process messages from the designated channel and ignore messages from the bot itself
                // This allows more than one bot in the same text channel.
                if (message.Channel.Id != Global.discordChannelID)
                {
                    return;
                }

                messageContent = await ReplaceUserMentionsWithUsernames(messageContent);

                if (messageContent == "!lock")
                {
                    lock (Global.lockInternalData)
                    {
                        if (Global.internalAdminList.Contains(message.Author.Id))
                        {
                            if (Global.adminLockFlag == false)
                            {
                                Global.adminLockFlag = true;
                                Global.loadedData.ADMINISTRATIVELOCK = Global.adminLockFlag;
                                EnqueueMessage(message.Channel.Id, "> Administrative commands are now locked!");
                            }
                            else
                            {
                                Global.adminLockFlag = false;
                                Global.loadedData.ADMINISTRATIVELOCK = Global.adminLockFlag;
                                EnqueueMessage(message.Channel.Id, "> Administrative commands are now unlocked!");
                            }
                        }
                        else
                        {
                            EnqueueMessage(message.Channel.Id, "> You don't have enough privileges to execute this command!");
                        }
                        return;
                    }
                }

                if (messageContent.StartsWith("!dm"))
                {
                    lock (Global.lockInternalData)
                    {
                        if (Global.internalAdminList.Contains(message.Author.Id))
                        {
                            if (Global.loadedData.ALLOWDMS)
                            {
                                Global.loadedData.ALLOWDMS = false;
                                EnqueueMessage(message.Channel.Id, "> DMs are now closed!");
                            }
                            else
                            {
                                Global.loadedData.ALLOWDMS = true;
                                EnqueueMessage(message.Channel.Id, "> DMs are now open!");
                            }
                        }
                        return;
                    }
                }

                if (messageContent == "!ping")
                {
                    EnqueueMessage(message.Channel.Id, "> Pong!");
                    return;
                }

                if (messageContent == "!help")
                {
                    EnqueueMessage(message.Channel.Id, "> **!reset** Resets ChaAIScrapper");
                    EnqueueMessage(message.Channel.Id, "> **!ytwatch URL** Forces the AI to watch a YT video");
                    EnqueueMessage(message.Channel.Id, "> **!refresh** Refreshes Character AI webpage");
                    EnqueueMessage(message.Channel.Id, "> **!audio** Enables/Disables the voice notes function");
                    EnqueueMessage(message.Channel.Id, "> **!dm** Opens/Closes the DMs");
                    EnqueueMessage(message.Channel.Id, "> **!lock** Locks/Unlocks bot commands to non-administrative users");
                    return;
                }

                if (!Global.adminLockFlag || Global.internalAdminList.Contains(message.Author.Id))
                {
                    if (messageContent == "!refresh")
                    {
                        SetBotStatus("Refreshing ChAI Page...");
                        EnqueueMessage(message.Channel.Id, "> Attempting page refresh...");
                        Global.refreshFlag = true;
                        return;
                    }

                    /*
                    if (messageContent.StartsWith("!character "))
                    {
                        lock (Global.lockInternalData)
                        {
                            SetBotStatus("Switching Character...");
                            TimeSpan timeDifference = DateTime.Now - Global.lastCharacterChangeTime;
                            if (timeDifference.TotalMinutes >= 5)
                            {
                                Global.characterAIChatURL = messageContent.Substring("!character ".Length);
                                Global.loadedData.CHAIURL = Global.characterAIChatURL;
                                if (characterURLTextBox.InvokeRequired)
                                {
                                    characterURLTextBox.Invoke(new Action(() =>
                                    {
                                        characterURLTextBox.Text = Global.characterAIChatURL;
                                    }));
                                }
                                else
                                {
                                    characterURLTextBox.Text = Global.characterAIChatURL;
                                }
                                if (mainForm.InvokeRequired)
                                {
                                    mainForm.Invoke(new Action(() =>
                                    {
                                        mainForm.ForceSaveCurrentData();
                                    }));
                                }
                                else
                                {
                                    mainForm.ForceSaveCurrentData();
                                }
                                Global.lastCharacterChangeTime = DateTime.Now;
                                Global.programSoftResetFlag = true;
                                return;
                            }
                            else
                            {
                                double remainingSeconds = (5 * 60) - timeDifference.TotalSeconds;
                                EnqueueMessage(message.Channel.Id, "> You cannot change character that soon! Remaining Seconds: " + remainingSeconds.ToString("F0"));
                                return;
                            }
                        }
                    }
                    */

                    if (messageContent.StartsWith("!reset"))
                    {
                        lock (Global.lockInternalData)
                        {
                            TimeSpan timeDifference = DateTime.Now - Global.lastCharacterChangeTime;
                            if (timeDifference.TotalMinutes >= 5)
                            {
                                Global.lastCharacterChangeTime = DateTime.Now;
                                Global.programSoftResetFlag = true;
                                return;
                            }
                            else
                            {
                                double remainingSeconds = (5 * 60) - timeDifference.TotalSeconds;
                                EnqueueMessage(message.Channel.Id, "> You cannot reset the bot that soon! Remaining Seconds: " + remainingSeconds.ToString("F0"));
                                return;
                            }
                        }
                    }
                }
                else
                {
                    EnqueueMessage(message.Channel.Id, "> You don't have enough privileges to execute this command!");
                    return;
                }

                if (messageContent.StartsWith("!ytstop"))
                {
                    lock (Global.lockInternalData)
                    {
                        if (!Global.botYTVirtualWatch)
                        {

                        }
                        else
                        {
                            EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** stopped watching YouTube...");
                            Global.botYTVirtualWatch = false;
                            Global.botYTWatchData = null;
                            SetBotStatus("Stopped YT Video...");
                        }
                    }
                }

                if (messageContent.StartsWith("!ytresume"))
                {
                    lock (Global.lockInternalData)
                    {
                        if (Global.botYTVirtualWatch)
                        {
                            if (Global.botYTPause)
                            {
                                EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** resumed YouTube...");
                                Global.botYTPause = false;
                                SetBotStatus("Resumed YT Video...");
                            }
                        }
                        else
                        {
                            EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** is not currently watching YouTube!");
                        }
                    }
                }

                if (messageContent.StartsWith("!ytpause"))
                {
                    lock (Global.lockInternalData)
                    {
                        if (Global.botYTVirtualWatch)
                        {
                            if (!Global.botYTPause)
                            {
                                EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** paused YouTube...");
                                Global.botYTPause = true;
                                SetBotStatus("Paused YT Video...");
                            }
                        }
                        else
                        {
                            EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** is not currently watching YouTube!");
                        }
                    }
                }

                if (messageContent.StartsWith("!ytwatch "))
                {
                    if (Global.allowYTVirtualWatch)
                    {
                        try
                        {
                            bool virtualWatchEnabled = false;
                            lock (Global.lockInternalData)
                            {
                                if (!Global.botYTVirtualWatch)
                                {
                                    EnqueueMessage(message.Channel.Id, "> ⏯ **" + Global.discordBotIAName + "** starts watching YouTube...");
                                    Global.botYTVirtualWatch = true;
                                    virtualWatchEnabled = true;
                                }
                                else
                                {
                                    EnqueueMessage(message.Channel.Id, "> ⏸ **" + Global.discordBotIAName + "** stopped watching YouTube...");
                                    Global.botYTVirtualWatch = false;
                                    Global.botYTWatchData = null;
                                }
                            }
                            if (virtualWatchEnabled)
                            {
                                try
                                {
                                    string videoUrl = messageContent.Substring("!ytwatch ".Length).Trim();
                                    YTVirtualWatch temporaryYTVirtualWatchData = await ChAIExternal.GetYouTubeSubtitlesAndDetailsAsync(videoUrl);
                                    if (temporaryYTVirtualWatchData.LENGTH == TimeSpan.Zero)
                                    {
                                        EnqueueMessage(message.Channel.Id, "> Error: Not a valid YT link! (Length is zero!)");
                                    }
                                    else
                                    {
                                        if (temporaryYTVirtualWatchData.YTCAPTIONS.Count > 0 && temporaryYTVirtualWatchData.LENGTH.TotalSeconds > 0)
                                        {
                                            lock (Global.lockInternalData)
                                            {
                                                Global.botYTVirtualWatchTime = TimeSpan.Zero;
                                                Global.botYTWatchData = temporaryYTVirtualWatchData;
                                                Global.botYTVirtualWatchLocalTime = DateTime.Now;
                                                Global.botYTPause = false;
                                                Global.botYTIntro = true;
                                            }
                                            EnqueueMessage(message.Channel.Id, "> Video Selected is **" + Global.botYTWatchData.YTTITLE + "** by **" + Global.botYTWatchData.YTUPLOADER + "**");
                                            EnqueueMessage(message.Channel.Id, "> To stop the video use !ytstop.");
                                            EnqueueMessage(message.Channel.Id, "> Use !ytpause and !ytresume to halt the video playback.");
                                            if (Global.botAudioMode)
                                            {
                                                EnqueueMessage(message.Channel.Id, "> Warning: Audio voice notes been disabled automatically to improve the AI's answer time while watching videos. You can forcefully enable them again using !audio");
                                                Global.botAudioMode = false;
                                            }
                                        }
                                        else
                                        {
                                            EnqueueMessage(message.Channel.Id, "> Error: Video has not enough data!");
                                            lock (Global.lockInternalData)
                                            {
                                                Global.botYTWatchData = null;
                                                Global.botYTVirtualWatch = false;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    EnqueueMessage(message.Channel.Id, "> Error: Not a valid YT link!");
                                    ChAIO.WriteGeneral(ex.ToString());
                                    lock (Global.lockInternalData)
                                    {
                                        Global.botYTWatchData = null;
                                        Global.botYTVirtualWatch = false;
                                    }
                                }
                            }
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else
                    {
                        EnqueueMessage(message.Channel.Id, "> **YT Virtual Watch been disabled by the administrator!**");
                        lock (Global.lockInternalData)
                        {
                            Global.botYTWatchData = null;
                            Global.botYTVirtualWatch = false;
                        }
                    }
                }

                if (messageContent.StartsWith("!audio"))
                {
                    try
                    {
                        lock (Global.lockInternalData)
                        {
                            if (Global.allowBotAudios)
                            {
                                if (!Global.botAudioMode)
                                {
                                    Global.botAudioMode = true;
                                    EnqueueMessage(message.Channel.Id, "> AI Audio Messages enabled!");
                                }
                                else
                                {
                                    Global.botAudioMode = false;
                                    EnqueueMessage(message.Channel.Id, "> AI Audio Messages disabled!");
                                }
                            }
                            else
                            {
                                Global.botAudioMode = false;
                                EnqueueMessage(message.Channel.Id, "> **AI Audios been disabled by the administrator!**");
                            }
                        }
                        return;
                    }
                    catch
                    {
                        return;
                    }
                }

                lock (Global.lockInternalData)
                {
                    if (!Global.discriminatoryExclusive)
                    {
                        if (messageContent.StartsWith(Global.discriminatoryString))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!messageContent.StartsWith(Global.discriminatoryString))
                        {
                            return;
                        }
                    }
                }

                // Log the message details
                ChAIO.WriteDiscord($"Received message from {message.Author.Username} (ID: {message.Author.Id}): {messageContent}");
                ChAIO.WriteDiscord($"Message ID: {message.Id}, Channel ID: {message.Channel.Id}, Timestamp: {message.Timestamp}");

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    ChAIO.WriteDiscord("The message content is empty or whitespace.");
                    return;
                }

                messageContent = SanitizeString(messageContent);

                if (!messageContent.StartsWith("!"))
                {
                    string timeMetadata = "";
                    if (Global.discordChatBuffer == "" && messageContent.Length > 0 && messageContent != "")
                    {
                        TimeSpan elapsedTime = DateTime.Now - Global.lastDiscordMessageTime;
                        if (elapsedTime.TotalSeconds > 0)
                        {
                            string elapsedTimeString = FormatElapsedTime(elapsedTime);
                            DateTime now = DateTime.Now;
                            string formattedDateTime = now.ToString("HH ':' mm '(24h format hour)' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                            timeMetadata += @"[DISCORD MAIN PUBLIC TEXT CHANNEL]( LOCAL TIME IS " + formattedDateTime + @" )";
                            timeMetadata += @"( " + elapsedTimeString + @" AFTER. TO TAG SOMEONE USE @username )";
                            Global.lastDiscordMessageTime = DateTime.Now;
                        }
                    }
                    if (message.Reference != null)
                    {
                        ulong referencedMessageId = message.Reference.MessageId.Value;
                        var referencedMessage = await message.Channel.GetMessageAsync(referencedMessageId);
                        // Check if the referenced message was sent by the bot
                        if (referencedMessage.Author.Id == Global.discordBotUserID)
                        {
                            // Get the ID of the replied message
                            ulong repliedMessageId = message.Reference.MessageId.Value;
                            var repliedMessage = await message.Channel.GetMessageAsync(repliedMessageId);
                            if (repliedMessage != null)
                            {
                                // Store the content of the replied message
                                string replyMessageContent = repliedMessage.Content;
                                replyMessageContent = SanitizeString(replyMessageContent);
                                replyMessageContent = await ReplaceUserMentionsWithUsernames(replyMessageContent);
                                ChAIO.WriteDiscord($"Message is a reference to another message sent by the bot: {replyMessageContent}");

                                string sanitizedMessageContent = SanitizeString(message.Content);

                                // Now you can use replyMessageContent as needed
                                lock (Global.lockInternalData)
                                {
                                    Global.discordChatBuffer += timeMetadata + $"( {message.Author.Username} has quoted what you said earlier : \"{replyMessageContent}\" and said: \"{sanitizedMessageContent}\" )";
                                }
                            }
                        }
                        else
                        {
                            ulong repliedMessageId = message.Reference.MessageId.Value;
                            var repliedMessage = await message.Channel.GetMessageAsync(repliedMessageId);
                            if (repliedMessage != null)
                            {
                                // Store the content of the replied message
                                string replyMessageContent = repliedMessage.Content;
                                replyMessageContent = SanitizeString(replyMessageContent);
                                replyMessageContent = RemoveNonBmpCharacters(replyMessageContent);
                                replyMessageContent = await ReplaceUserMentionsWithUsernames(replyMessageContent);
                                ChAIO.WriteDiscord($"Message is a reference to another message sent by other user: {replyMessageContent}");

                                string sanitizedMessageContent = SanitizeString(message.Content);

                                // Now you can use replyMessageContent as needed
                                lock (Global.lockInternalData)
                                {
                                    Global.discordChatBuffer += timeMetadata + $"( {message.Author.Username} has quoted what {referencedMessage.Author.Username} said earlier: \"{replyMessageContent}\" and said \"{sanitizedMessageContent}\" )";
                                }
                            }
                        }
                    }
                    else
                    {
                        lock (Global.lockInternalData)
                        {
                            if (messageContent.Contains("@" + Global.discordBotUserID))
                            {
                                messageContent = messageContent.Replace("@" + Global.discordBotUserID, "");
                                if (Global.discordChatBuffer == "")
                                {
                                    Global.discordChatBuffer += timeMetadata + $"( {message.Author.Username} has directly tagged you and said : \"{messageContent}\" you must tag your answer by using \"@{message.Author.Username} \" )";
                                }
                                else
                                {
                                    Global.discordChatBuffer += timeMetadata + $" ( And {message.Author.Username} has directly tagged you and said: \"{messageContent}\" you must tag your answer by using \"@{message.Author.Username} \" )";
                                }
                            }
                            else
                            {
                                if (Global.discordChatBuffer == "")
                                {
                                    Global.discordChatBuffer += timeMetadata + $"* {message.Author.Username} told you: \"{messageContent}\" *";
                                }
                                else
                                {
                                    Global.discordChatBuffer += timeMetadata + $" * And also {message.Author.Username} told you: \"{messageContent}\" *";
                                }
                            }
                        }
                    }
                }
                if (Global.programSoftResetFlag)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                ChAIO.WriteDiscord($"Discord String Compiler Error: " + ex.ToString());
                Global.AppendToFile("ErrorLog.txt", ex.ToString());
            }
        }
        // Enqueue the DM message for a user by their Discord user ID
        public static void EnqueueDirectMessage(ulong userId, string message)
        {
            // Enqueue the direct message with user ID
            DirectMessageQueue.Enqueue((userId, message));

            // Skip processing if the program soft reset flag is set
            if (Global.programSoftResetFlag)
            {
                return;
            }
        }

        // Process the Direct Message Queue asynchronously
        public static async Task ProcessDirectMessagesQueueAsync(DiscordSocketClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Dequeue a message if available
                if (DirectMessageQueue.TryDequeue(out var queuedMessage))
                {
                    // Wait for rate limiting
                    await RateLimitSemaphore.WaitAsync();

                    try
                    {
                        // Find the user by their ID
                        var user = client.GetUser(queuedMessage.Item1);
                        if (user != null)
                        {
                            // Send the direct message to the user
                            await user.SendMessageAsync(queuedMessage.Item2);
                        }

                        // Apply rate limit delay after sending the message
                        await Task.Delay(RateLimitDelay);
                    }
                    finally
                    {
                        RateLimitSemaphore.Release();
                    }
                }
                else
                {
                    // If no messages in the queue, wait before checking again
                    await Task.Delay(100);
                }

                // Check for soft reset flag to exit
                if (Global.programSoftResetFlag)
                {
                    return;
                }
            }
            return;
        }
        public static void EnqueueMessage(ulong channelId, string message)
        {
            MessageQueue.Enqueue((channelId, message));
            if (Global.programSoftResetFlag)
            {
                return;
            }
        }

        public static async Task ProcessMessageQueueAsync(DiscordSocketClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (MessageQueue.TryDequeue(out var queuedMessage))
                {
                    await RateLimitSemaphore.WaitAsync();
                    try
                    {
                        await SendMessageAsync(client, queuedMessage.Item1, queuedMessage.Item2);
                        await Task.Delay(RateLimitDelay);
                    }
                    finally
                    {
                        RateLimitSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
                if (Global.programSoftResetFlag)
                {
                    return;
                }
            }
            return;
        }
        public static void EnqueueFileToSend(ulong channelId, string filePath)
        {
            FileQueue.Enqueue((channelId, filePath));
        }
        public static async Task ProcessFileQueueAsync(DiscordSocketClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (FileQueue.TryDequeue(out var queuedFile))
                {
                    await RateLimitSemaphore.WaitAsync();
                    try
                    {
                        var channel = client.GetChannel(queuedFile.Item1) as IMessageChannel;
                        if (channel != null)
                        {
                            try
                            {
                                if (queuedFile.Item2.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                                {
                                    await channel.SendFileAsync(queuedFile.Item2, "", false);
                                    //await channel.SendFileAsync(queuedFile.Item2, "", false, null, null, false, Discord.AllowedMentions.All, null, null, null, null, MessageFlags.VoiceMessage);
                                }
                                else
                                {
                                    await channel.SendFileAsync(queuedFile.Item2, "", false);
                                }
                                ChAIO.WriteDiscord("File sent successfully.");
                            }
                            catch (Exception ex)
                            {
                                ChAIO.WriteDiscord("Failed to send file: " + ex.ToString());
                                Global.AppendToFile("ErrorLog.txt", ex.ToString());
                            }
                        }
                        else
                        {
                            ChAIO.WriteDiscord($"Channel with ID {queuedFile.Item1} not found.");
                        }
                    }
                    finally
                    {
                        RateLimitSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(100); // Wait a bit before checking the queue again
                }
            }
        }
        public static async Task CheckAndSendFeasibleAnswerAsync(DiscordSocketClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(250); // Check every 250ms

                lock (Global.lockInternalData)
                {
                    if (Global.lastDiscordAnswer != "" && Global.previousDiscordAnswer != Global.lastDiscordAnswer)
                    {
                        Global.lastDiscordAnswer = DuplicateLineBreaks(Global.lastDiscordAnswer);
                        if (Global.botYTVirtualWatch)
                        {
                            if (!Global.botYTPause)
                            {
                                SetBotStatus("Watching a YT Video...");
                                EnqueueMessage(Global.discordChannelID, "> * " + Global.discordBotIAName + " is now watching **" + Global.botYTWatchData.YTTITLE + "** " + Global.botYTVirtualWatchTime.ToString() + " / " + Global.botYTWatchData.LENGTH.ToString());
                            }
                        }
                        EnqueueMessage(Global.discordChannelID, Global.lastDiscordAnswer);
                        Global.responsesCounter++;
                        if (ResponsesCounterLabel.InvokeRequired)
                        {
                            ResponsesCounterLabel.Invoke(new Action(() =>
                            {
                                ResponsesCounterLabel.Text = Global.responsesCounter.ToString();
                            }));
                        }
                        else
                        {
                            ResponsesCounterLabel.Text = Global.responsesCounter.ToString();
                        }
                        Global.previousDiscordAnswer = Global.lastDiscordAnswer;
                        Global.lastDiscordAnswer = "";
                    }
                    try
                    {
                        if (Global.loadedData.CHANGEPROFILEPICTURE)
                        {
                            if (Global.discordImageBytes != null && Global.discordImageFlag && !Global.discordImageUpdateFlag)
                            {
                                // HERE
                                try
                                {
                                    using (Stream stream = new MemoryStream(Global.discordImageBytes))
                                    {
                                        client.CurrentUser.ModifyAsync(properties => properties.Avatar = new Discord.Image(stream)).Wait();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ChAIO.WriteDiscord(ex.ToString());
                                }
                                try
                                {
                                    string heicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "botAvatar.heic");
                                    string jpgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "botAvatar_converted.jpg");
                                    File.WriteAllBytes(heicPath, Global.discordImageBytes);

                                    try
                                    {
                                        using (var image = new MagickImage(heicPath))
                                        {
                                            image.Format = MagickFormat.Jpeg;
                                            image.Write(jpgPath);
                                        }

                                        if (botPanelImage.Image != null)
                                        {
                                            botPanelImage.Image.Dispose();
                                            botPanelImage.Image = null;
                                        }
                                        botPanelImage.ImageLocation = jpgPath;
                                    }
                                    catch (Exception ex)
                                    {
                                        ChAIO.WriteGeneral("Error converting HEIC to JPG: " + ex.Message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ChAIO.WriteGeneral("Error loading image into PictureBox: " + ex.Message);
                                }
                                Global.discordImageUpdateFlag = true;
                            }
                        }
                        else
                        {
                            Global.discordImageUpdateFlag = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ChAIO.WriteDiscord("It was not possible to upload the current AI Image!: " + ex.ToString());
                        Global.AppendToFile("ErrorLog.txt", ex.ToString());
                    }

                    if (Global.discordBotScrapedIAName != Global.discordBotIAName)
                    {
                        if (Global.loadedData.CHANGEUSERNAME)
                        {
                            foreach (var guild in client.Guilds)
                            {
                                var botGuildUser = guild.GetUser(client.CurrentUser.Id);
                                string newBotUsername = Global.loadedData.USERNAMESTENCIL.Replace("USERNAME", Global.discordBotScrapedIAName);
                                botGuildUser.ModifyAsync(properties => properties.Nickname = newBotUsername).Wait();
                            }
                        }

                        Global.discordBotIAName = Global.discordBotScrapedIAName;
                    }

                    if (Global.botAudioGenerated && File.Exists("AIVoiceMessage.ogg"))
                    {
                        EnqueueFileToSend(Global.discordChannelID, "AIVoiceMessage.ogg");
                        Global.botAudioGenerated = false;
                    }
                }
            }
            return;
        }
        public static async Task SendMessageAsync(DiscordSocketClient client, ulong channelId, string message)
        {
            var channel = client.GetChannel(channelId) as IMessageChannel;
            if (channel != null)
            {
                var guildChannel = channel as SocketGuildChannel;
                if (guildChannel != null)
                {
                    var guild = guildChannel.Guild;

                    var regex = new Regex(@"@([\w.-]+)", RegexOptions.IgnoreCase);
                    var matches = regex.Matches(message);

                    var userCache = new Dictionary<string, SocketGuildUser>();

                    foreach (Match match in matches)
                    {
                        if (!match.Success) continue;

                        string username = match.Groups[1].Value;

                        if (!userCache.TryGetValue(username.ToLower(), out var user))
                        {
                            user = await GetUserByUsernameAsync(guild, username);

                            if (user == null)
                            {
                                for (int i = username.Length - 1; i > 0; i--)
                                {
                                    string partialUsername = username.Substring(0, i);
                                    user = await GetUserByUsernameAsync(guild, partialUsername);
                                    if (user != null)
                                        break;
                                }
                            }

                            userCache[username.ToLower()] = user;
                        }

                        if (user != null)
                        {
                            message = Regex.Replace(message, $@"@{Regex.Escape(username)}\b", user.Mention, RegexOptions.IgnoreCase);
                        }
                    }
                }

                await channel.SendMessageAsync(message);
            }
        }

        public static Task<SocketGuildUser> GetUserByUsernameAsync(SocketGuild guild, string username)
        {
            return Task.FromResult(guild.Users.FirstOrDefault(user => user.Username == username));
        }
        public static string SanitizeString(string input)
        {
            string pattern = @"[\n\r\t\b\a]";
            string sanitized = Regex.Replace(input, pattern, string.Empty);

            return sanitized;
        }
        public static void SetBotStatus(string status)
        {
            if (AIStatusLabel.InvokeRequired)
            {
                AIStatusLabel.Invoke(new Action(() =>
                {
                    AIStatusLabel.Text = status;
                }));
            }
            else
            {
                AIStatusLabel.Text = status;
            }
        }
        public static string RemoveNonBmpCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new System.Text.StringBuilder(input.Length);

            foreach (var ch in input.Normalize(NormalizationForm.FormC))
            {
                if (ch <= 0xFFFF && !char.IsSurrogate(ch) && !char.IsControl(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
        public static string DuplicateLineBreaks(string input)
        {
            if (input == null) return null;

            return input.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine);
        }
        private static string FormatElapsedTime(TimeSpan elapsedTime)
        {
            if (elapsedTime.TotalSeconds < 60)
            {
                return Math.Round((double)elapsedTime.Seconds).ToString() + " SECONDS";
            }
            else if (elapsedTime.TotalMinutes < 60)
            {
                return Math.Round((double)elapsedTime.Minutes).ToString() + " MINUTES";
            }
            else if (elapsedTime.TotalHours < 24)
            {
                return Math.Round((double)elapsedTime.Hours).ToString() + " HOURS";
            }
            else if (elapsedTime.TotalDays < 7)
            {
                return Math.Round((double)elapsedTime.Days).ToString() + " DAYS";
            }
            else
            {
                int weeks = (int)Math.Floor((double)(elapsedTime.TotalDays / 7));
                return weeks + " WEEKS";
            }
        }
        private static async Task ReactionAddedAsync(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> cachedChannel,
        SocketReaction reaction)
        {
            if (!Global.discordReactions) { return; }

            var message = await cachedMessage.GetOrDownloadAsync();
            if (message == null || message.Author.Id != Global.discordBotUserID)
                return;

            var channel = await cachedChannel.GetOrDownloadAsync();

            if (channel.Id != Global.discordChannelID)
                return;

            string emojiUsed = "";

            if (reaction.Emote is Emote customEmoji)
            {
                emojiUsed = customEmoji.Name;
            }
            else if (reaction.Emote is Emoji standardEmoji)
            {
                emojiUsed = EmojiOne.EmojiOne.ToShort(standardEmoji.Name);
            }

            emojiUsed = RemoveNonBmpCharacters(emojiUsed);
            string reactedMessage = RemoveNonBmpCharacters(message.Content);
            var reactor = reaction.User.IsSpecified ? reaction.User.Value : null;
            string reactorName = reactor?.Username ?? "Unknown";

            if (reactor != null && !Global.internalIgnoreList.Contains(reactor.Id))
            {
                bool alreadyOnBuffer = false;
                string newInput = "";
                TimeSpan timeSincePosted = DateTimeOffset.UtcNow - message.Timestamp;
                string howLongAgo = timeSincePosted.Days > 0
                ? $"{timeSincePosted.Days} day{(timeSincePosted.Days == 1 ? "" : "s")}, {timeSincePosted.Hours} hour{(timeSincePosted.Hours == 1 ? "" : "s")}, {timeSincePosted.Minutes} minute{(timeSincePosted.Minutes == 1 ? "" : "s")} and {timeSincePosted.Seconds} second{(timeSincePosted.Seconds == 1 ? "" : "s")}"
                : timeSincePosted.Hours > 0
                    ? $"{timeSincePosted.Hours} hour{(timeSincePosted.Hours == 1 ? "" : "s")}, {timeSincePosted.Minutes} minute{(timeSincePosted.Minutes == 1 ? "" : "s")} and {timeSincePosted.Seconds} second{(timeSincePosted.Seconds == 1 ? "" : "s")}"
                    : timeSincePosted.Minutes > 0
                        ? $"{timeSincePosted.Minutes} minute{(timeSincePosted.Minutes == 1 ? "" : "s")} and {timeSincePosted.Seconds} second{(timeSincePosted.Seconds == 1 ? "" : "s")}"
                        : $"{timeSincePosted.Seconds} second{(timeSincePosted.Seconds == 1 ? "" : "s")}";

                ChAIO.WriteDiscord("User " + reactorName + " reacted with " + emojiUsed + " on the AI's message <" + reactedMessage + ">");

                lock (Global.lockInternalData)
                {
                    foreach (string input in Global.reactionsBufferList)
                    {
                        if (input.Contains(reactedMessage))
                        {
                            alreadyOnBuffer = true;
                            break;
                        }
                    }
                    if (!alreadyOnBuffer)
                    {
                        newInput = "[ * The user <" + reactorName + "> reacted with <" + emojiUsed + "> to your message from " + howLongAgo + " ago '" + reactedMessage + "' * ]";
                    }
                    else
                    {
                        string tenFirst = reactedMessage.Length > 10
                        ? reactedMessage.Substring(0, 10)
                        : reactedMessage;

                        newInput = "[ * The user <" + reactorName + "> also reacted with <" + emojiUsed + "> to your message that starts with '" + tenFirst + "...' * ]";
                    }
                    Global.reactionsBufferList.Add(newInput);
                }
            }
        }

        private static async Task<string> ReplaceUserMentionsWithUsernames(string messageContent)
        {
            var regex = new Regex(@"<@(\d+)>");
            var matches = regex.Matches(messageContent);

            foreach (Match match in matches)
            {
                ulong userId = ulong.Parse(match.Groups[1].Value);
                var user = await discordClient.GetUserAsync(userId);

                if (user != null)
                {
                    string username = user.Username;
                    messageContent = messageContent.Replace(match.Value, "@" + username);
                }
            }

            return messageContent;
        }
    }
}
