using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
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
using static ChAIScrapper.ChAIScrapperProgram;

namespace ChAIScrapper
{
    public static class ChAIDiscordBot
    {
        private static readonly SemaphoreSlim RateLimitSemaphore = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentQueue<(ulong, string)> MessageQueue = new ConcurrentQueue<(ulong, string)>();
        private static ConcurrentQueue<(ulong, string)> FileQueue = new ConcurrentQueue<(ulong, string)>();
        private static readonly TimeSpan RateLimitDelay = TimeSpan.FromMilliseconds(1000 / 5); // 5 messages per second
        public static DiscordSocketClient discordClient = null;
        private static Process ffmpeg;

        public static async Task RunDiscordBotAsync(CancellationTokenSource cancellationToken)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            discordClient = new DiscordSocketClient(config);

            discordClient.Log += LogAsync;
            discordClient.MessageReceived += MessageReceivedAsync;

            await discordClient.LoginAsync(TokenType.Bot, Global.discordBotToken);
            await discordClient.StartAsync();

            try
            {
                var processFileQueueTask = Task.Run(() => ProcessFileQueueAsync(discordClient, cancellationToken.Token), cancellationToken.Token);
                var processMessageQueueTask = Task.Run(() => ProcessMessageQueueAsync(discordClient, cancellationToken.Token), cancellationToken.Token);
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
                        var defaultChannel = discordClient.GetChannel(Global.discordChannelID) as SocketTextChannel;
                        if (defaultChannel != null)
                        {
                            await defaultChannel.TriggerTypingAsync();
                        }
                    }

                    await Task.Delay(500);
                }

                await Task.WhenAll(processMessageQueueTask, checkAndSendFeasibleAnswerTask, processFileQueueTask);
            }
            catch (OperationCanceledException)
            {
                //
            }
            finally
            {
                await discordClient.StopAsync();
                await discordClient.LogoutAsync();
                discordClient.Dispose();
            }
        }

        public static Task LogAsync(LogMessage log)
        {
            ChAIScrapperProgram.Write(log.ToString());
            return Task.CompletedTask;
        }

        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            try
            {
                // Only process messages from the designated channel and ignore messages from the bot itself
                // This allows more than one bot in the same text channel.
                if (message.Channel.Id != Global.discordChannelID || message.Author.Id == Global.discordBotUserID)
                {
                    return;
                }

                string messageContent = RemoveNonBmpCharacters(message.Content);
                messageContent = await ReplaceUserMentionsWithUsernames(messageContent);

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
                    EnqueueMessage(message.Channel.Id, "> **!fb 1-5** Sets feedback to the last message the AI sent");
                    EnqueueMessage(message.Channel.Id, "> **!character URL** Changes the current chat used by the AI");
                    return;
                }

                if (messageContent == "!refresh")
                {
                    Global.refreshFlag = true;
                    return;
                }

                if (messageContent.StartsWith("!character "))
                {
                    lock (Global.lockInternalData)
                    {
                        TimeSpan timeDifference = DateTime.Now - Global.lastCharacterChangeTime;
                        if (timeDifference.TotalMinutes >= 5)
                        {
                            Global.characterAIChatURL = messageContent.Substring("!character ".Length);
                            Global.loadedData.CHAIURL = Global.characterAIChatURL;
                            ChAIScrapperProgram.SaveObjectToFile<SavedData>(Global.loadedData, "ChAISData.xml");
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
                                    string videoUrl = messageContent.Substring("!watch ".Length).Trim();
                                    YTVirtualWatch temporaryYTVirtualWatchData = await ChAIExternal.GetYouTubeSubtitlesAndDetailsAsync(videoUrl);
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
                                        EnqueueMessage(message.Channel.Id, "> Use !ytpause and !resume to halt the video playback.");
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
                                catch
                                {
                                    EnqueueMessage(message.Channel.Id, "> Error: Not a valid YT link!");
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

                if (messageContent.StartsWith("!fb ") || messageContent.ToLower().StartsWith("!feedback "))
                {
                    try
                    {
                        string feedbackLevelString = messageContent.Split(' ')[1];
                        if (int.TryParse(feedbackLevelString, out int feedbackLevel))
                        {
                            if (feedbackLevel <= 0)
                            {
                                EnqueueMessage(message.Channel.Id, "> Feedback must be greater than 0 and equal or smaller to 4!");
                                return;
                            }
                            if (feedbackLevel > 4)
                            {
                                EnqueueMessage(message.Channel.Id, "> Feedback must be greater than 0 and equal or smaller to 4!");
                                feedbackLevel = 4;
                            }

                            string feedbackString = "";
                            for (int i = 0; i < feedbackLevel; i++)
                            {
                                feedbackString += "⭐";
                            }
                            EnqueueMessage(message.Channel.Id, "> Feedback sent! " + feedbackString);
                            Global.discordFeedbackLevel = feedbackLevel;
                            return;
                        }
                    }
                    catch
                    {
                        return;
                    }
                }

                if (messageContent.StartsWith("//"))
                {
                    return;
                }

                // Log the message details
                ChAIScrapperProgram.Write($"Received message from {message.Author.Username} (ID: {message.Author.Id}): {messageContent}");
                ChAIScrapperProgram.Write($"Message ID: {message.Id}, Channel ID: {message.Channel.Id}, Timestamp: {message.Timestamp}");

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    ChAIScrapperProgram.Write("The message content is empty or whitespace.");
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
                            string formattedDateTime = now.ToString("HH ':' mm '24h format hour' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                            timeMetadata += @"( LOCAL TIME IS " + formattedDateTime + @" )";
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
                                ChAIScrapperProgram.Write($"Message is a reference to another message sent by the bot: {replyMessageContent}");

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
                                replyMessageContent = await ReplaceUserMentionsWithUsernames(replyMessageContent);
                                ChAIScrapperProgram.Write($"Message is a reference to another message sent by other user: {replyMessageContent}");

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
                ChAIScrapperProgram.Write($"Discord String Compiler Error: " + ex.ToString());
                Global.AppendToFile("ErrorLog.txt", ex.ToString());
            }
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
                                ChAIScrapperProgram.Write("File sent successfully.");
                            }
                            catch (Exception ex)
                            {
                                ChAIScrapperProgram.Write("Failed to send file: " + ex.ToString());
                                Global.AppendToFile("ErrorLog.txt", ex.ToString());
                            }
                        }
                        else
                        {
                            ChAIScrapperProgram.Write($"Channel with ID {queuedFile.Item1} not found.");
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
                                EnqueueMessage(Global.discordChannelID, "> * " + Global.discordBotIAName + " is now watching **" + Global.botYTWatchData.YTTITLE + "** " + Global.botYTVirtualWatchTime.ToString() + " / " + Global.botYTWatchData.LENGTH.ToString());
                            }
                        }
                        EnqueueMessage(Global.discordChannelID, Global.lastDiscordAnswer);
                        Global.previousDiscordAnswer = Global.lastDiscordAnswer;
                        Global.lastDiscordAnswer = "";
                    }
                    try
                    {
                        if (Global.discordImageBytes != null && Global.discordImageFlag && !Global.discordImageUpdateFlag)
                        {
                            using (Stream stream = new MemoryStream(Global.discordImageBytes))
                            {
                                client.CurrentUser.ModifyAsync(properties => properties.Avatar = new Image(stream)).Wait();
                            }
                            Global.discordImageUpdateFlag = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ChAIScrapperProgram.Write("It was not possible to upload the current AI Image!: " + ex.ToString());
                        Global.AppendToFile("ErrorLog.txt", ex.ToString());
                    }

                    if (Global.discordBotScrapedIAName != Global.discordBotIAName)
                    {
                        foreach (var guild in client.Guilds)
                        {
                            var botGuildUser = guild.GetUser(client.CurrentUser.Id);
                            botGuildUser.ModifyAsync(properties => properties.Nickname = "ChAI.S (AI: " + Global.discordBotScrapedIAName + ")").Wait();
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

                    // Check for @username pattern in the message
                    // This part sadly keeps having some mistakes on its RegEX expressions...
                    var matches = System.Text.RegularExpressions.Regex.Matches(message, @"@(\w[\w.-]*)(?=\s|,|\n|$|\)|\]|!|\?)");
                    if (matches.Count == 0) { matches = System.Text.RegularExpressions.Regex.Matches(message, @"@([\w.-]+)"); }
                    if (matches.Count == 0) { matches = System.Text.RegularExpressions.Regex.Matches(message, @"@(\w[\w]*)(?=\s|,|\n|$|\)|\]|!|\?|,)"); }
                    if (matches.Count == 0) { matches = System.Text.RegularExpressions.Regex.Matches(message, @"@(\w[\w.-]*)(?=\s|,|\n|$)"); }
                    if (matches.Count == 0) { matches = System.Text.RegularExpressions.Regex.Matches(message, @"@[\w.-] +"); };
                    foreach (Match match in matches)
                    {
                        var username = match.Groups[1].Value;
                        var user = await GetUserByUsernameAsync(guild, username);
                        if (user != null)
                        {
                            // Replace @username with the proper mention format for each match
                            message = message.Replace($"@{username}", user.Mention);
                        }
                        else
                        {
                            // If the user does not exist, progressively remove characters from the end of the string and check each one
                            for (int i = username.Length - 1; i >= 0; i--)
                            {
                                var partialUsername = username.Substring(0, i);
                                var partialUser = await GetUserByUsernameAsync(guild, partialUsername);
                                if (partialUser != null)
                                {
                                    // If a user with the partial username is found, replace @username with the proper mention format
                                    message = message.Replace($"@{username}", partialUser.Mention);
                                    break;
                                }
                            }
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
        public static string RemoveNonBmpCharacters(string input)
        {
            try
            {
                if (input == null) return null;

                StringBuilder stringBuilder = new StringBuilder();
                foreach (var rune in input.EnumerateRunes())
                {
                    if (rune.IsBmp)
                    {
                        stringBuilder.Append(rune.ToString());
                    }
                }

                return stringBuilder.ToString();
                }
            catch (Exception ex)
            {
                ChAIScrapperProgram.Write("Error removing Non-BMP Characters: " + ex.Message);
                Global.AppendToFile("ErrorLog.txt", ex.ToString());
                return input;
            }
        }
        private struct StringRuneEnumerator
        {
            private readonly string _string;
            private int _index;

            public StringRuneEnumerator(string @string)
            {
                _string = @string;
                _index = 0;
                Current = default;
            }

            public Rune Current { get; private set; }

            public bool MoveNext()
            {
                if (_index >= _string.Length) return false;

                if (Rune.TryGetRuneAt(_string, _index, out Rune rune))
                {
                    Current = rune;
                    _index += rune.Utf16SequenceLength;
                    return true;
                }

                _index++;
                return false;
            }
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
