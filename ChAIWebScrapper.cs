using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;
using NAudio.Lame;
using System.Reactive;
using HtmlAgilityPack;
using System.Net;
using ChAIScrapperWF;

namespace ChAIScrapperWF
{
    public static class ChAIWebScrapper
    {
        private static WasapiLoopbackCapture loopbackCapture;
        private static WaveFileWriter writer;
        private static string outputFilePath = "tmpBotAudio.wav";
        private static DateTime lastSoundDetectedTime;
        private static bool isSilent = false;
        private static bool soundEverDetected = false;
        private static readonly TimeSpan SilenceThreshold = TimeSpan.FromSeconds(3);

        public static Label AINameLabel;
        public static Label AIStatusLabel;
        public static Label ResponsesCounterLabel;
        public static Label DMCounterLabel;

        public static void RunWebScraper(CancellationTokenSource cancellationToken)
        {
            ChromeOptions chromeOptions = new ChromeOptions();

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            chromeOptions.DebuggerAddress = Global.chromeDebuggerAddress;
            chromeOptions.BinaryLocation = Global.portableChromiumPath;

            bool flagFirstTime = true;

            using (var driver = new ChromeDriver(service, chromeOptions))
            {
                driver.Navigate().GoToUrl(Global.characterAIChatURL);

                if (flagFirstTime && Global.initialBotBriefing != "" && Global.briefTheBot)
                {
                    Thread.Sleep(3000);
                    SetBotStatus("Sending Initial Briefing...");
                    SimulateInput(driver, RemoveNonBmpCharacters(Global.initialBotBriefing));
                    Thread.Sleep(10000);
                }
                else
                {
                    flagFirstTime = false;
                }

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                wait.Until(d => d.FindElements(By.CssSelector(".mt-1.max-w-xl.rounded-2xl.px-3.min-h-12.flex.justify-center.py-3.bg-surface-elevation-2")).Count > 0);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var answers = ScrapeAnswers(driver, false);

                    if (answers.Length > 0)
                    {
                        if (answers[0] != Global.lastFeasibleAnswer)
                        {
                            lock (Global.lockInternalData)
                            {
                                Global.lastFeasibleAnswer = answers[0];
                                if (Global.currentDMBuffer == null)
                                {
                                    Global.lastDiscordAnswer = Global.lastFeasibleAnswer;
                                }
                                else
                                {
                                    ulong authorID = new ulong();
                                    authorID = Global.currentDMBuffer.ID;
                                    ChAIDiscordBot.EnqueueDirectMessage(authorID, Global.lastFeasibleAnswer);
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
                                    Global.currentDMBuffer = null;
                                }
                            }
                            ChAIO.WriteScrapper("Last Answer: ");
                            ChAIO.WriteScrapper(Global.lastFeasibleAnswer);
                        }
                        else
                        {
                            ChAIO.WriteScrapper("No new answers!");
                        }
                        SetBotStatus("Idle...");
                        ChAIO.WriteScrapper("Waiting for Discord Chat Buffer Input...");
                        while (true)
                        {
                            try
                            {
                                string prePrompt = "";
                                if (Global.simulatedInputsCounter >= 30 || Global.refreshFlag)
                                {
                                    Global.simulatedInputsCounter = 0;
                                    Global.refreshFlag = false;
                                    driver.Navigate().Refresh();
                                    Global.lastPageRefresh = DateTime.Now;
                                    Thread.Sleep(2000);
                                    ScrapeAnswers(driver, true);
                                    SetBotStatus("Refreshing ChAI Page...");
                                }
                                if ((DateTime.Now - Global.lastPageRefresh).TotalMinutes >= 20)
                                {
                                    driver.Navigate().Refresh();
                                    Global.lastPageRefresh = DateTime.Now;
                                    Thread.Sleep(2000);
                                    ScrapeAnswers(driver, true);
                                    SetBotStatus("Refreshing ChAI Page...");
                                }
                                lock (Global.lockInternalData)
                                {
                                    if (Global.DMBufferList.Count == 0 && Global.currentDMBuffer == null && !Global.botYTVirtualWatch)
                                    {
                                        SetBotStatus("Idle...");
                                    }
                                }
                                if (Global.botYTVirtualWatch && Global.botYTWatchData != null)
                                {
                                    lock (Global.lockInternalData)
                                    {
                                        if (!Global.botYTPause)
                                        {
                                            if (Global.botYTIntro)
                                            {
                                                Thread.Sleep(250); // Little wait to ensure the YT Data has been fetched properly.
                                                string virtualWatchIntroString = "[ Currently Starting to Watch in YouTube: " + Global.botYTWatchData.YTTITLE + " by " + Global.botYTWatchData.YTUPLOADER + ", video length " + Global.botYTWatchData.LENGTH + @", Video Description: " + Global.botYTWatchData.YTDESCRIPTION + @" ]";
                                                prePrompt += virtualWatchIntroString;
                                                Global.botYTIntro = false;
                                                Global.lastDiscordInteractionTime = DateTime.Now;
                                                ChAIO.WriteScrapper("Simulated Input: " + virtualWatchIntroString);
                                            }
                                        }
                                    }
                                    if ((DateTime.Now - Global.botYTVirtualWatchLocalTime).TotalSeconds >= Global.botYTVirtualWatchPace.TotalSeconds)
                                    {
                                        lock (Global.lockInternalData)
                                        {
                                            if (!Global.botYTPause)
                                            {
                                                if (Global.botYTVirtualWatchTime < Global.botYTWatchData.LENGTH)
                                                {
                                                    string virtualWatchString = "";
                                                    virtualWatchString = "[ Currently Watching in YouTube: " + Global.botYTWatchData.YTTITLE + " by " + Global.botYTWatchData.YTUPLOADER + ", playtime " + Global.botYTVirtualWatchTime + " / " + Global.botYTWatchData.LENGTH + " ]";
                                                    string virtualCaptions = ChAIExternal.YTGetCaptions(Global.botYTWatchData, Global.botYTVirtualWatchTime, Global.botYTVirtualWatchTime + Global.botYTVirtualWatchPace);
                                                    virtualCaptions = @"[ The YouTube video says "" " + virtualCaptions + @" "" ]";
                                                    string virtualPrompt = " [ You may react to the video as you wish! ]";
                                                    Global.botYTVirtualWatchTime += Global.botYTVirtualWatchPace;
                                                    if (Global.botYTVirtualWatchTime > Global.botYTWatchData.LENGTH)
                                                    {
                                                        Global.botYTVirtualWatchTime = Global.botYTWatchData.LENGTH;
                                                    }
                                                    prePrompt += virtualWatchString + virtualCaptions + virtualPrompt;
                                                    ChAIO.WriteScrapper("Simulated Input: " + virtualWatchString + virtualCaptions + virtualPrompt);
                                                    Global.botYTVirtualWatchLocalTime = DateTime.Now;
                                                    Global.lastDiscordInteractionTime = DateTime.Now;
                                                }
                                                else
                                                {
                                                    string virtualWatchString = "";
                                                    virtualWatchString = @"[ YouTube video ended! ]"; // You check the comments and the most liked comment states: "" " + Global.botYTWatchData.YTMAINCOMMENT + @" "" ]";
                                                    prePrompt += virtualWatchString;
                                                    ChAIO.WriteScrapper("Simulated Input: " + virtualWatchString);
                                                    Global.botYTVirtualWatchLocalTime = DateTime.Now;
                                                    Global.lastDiscordInteractionTime = DateTime.Now;
                                                    Global.botYTWatchData = null;
                                                    Global.botYTVirtualWatch = false;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (Global.DMBufferList.Count > 0 && !Global.botYTVirtualWatch)
                                {
                                    lock (Global.lockInternalData)
                                    {
                                        if (Global.currentDMBuffer == null)
                                        {
                                            // Get the oldest message from the DM buffer
                                            ChAIDataStructures.DMBuffer oldestDM = Global.DMBufferList[0];

                                            ChAIDataStructures.DMBuffer clonedDMBuffer = new ChAIDataStructures.DMBuffer
                                            {
                                                AUTHOR = oldestDM.AUTHOR,
                                                CONTENT = oldestDM.CONTENT,
                                                ID = oldestDM.ID,
                                                TIME = oldestDM.TIME
                                            };

                                            Global.currentDMBuffer = clonedDMBuffer;

                                            // Format the current date and time
                                            DateTime now = DateTime.Now;
                                            string formattedDateTime = now.ToString("HH ':' mm '(24h format hour)' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                                            prePrompt += @"[YOUR DISCORD DIRECT MESSAGE PERSONAL TRAY]( * LOCAL TIME IS NOW " + formattedDateTime + @"* ) ";

                                            // Merge messages from the same author if there are more messages
                                            var authorMessages = Global.DMBufferList
                                                .Where(msg => msg.AUTHOR == oldestDM.AUTHOR)
                                                .OrderBy(msg => msg.TIME) // Ensure messages are ordered by time
                                                .ToList();

                                            // Initialize a string to hold the merged messages
                                            string mergedMessagesString = "";

                                            formattedDateTime = Global.currentDMBuffer.TIME.ToString("HH ':' mm '(24h format hour)' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                                            string contextString = @"( * THIS DIRECT MESSAGE TIME WAS " + formattedDateTime + "* ) [ * User \"" + Global.currentDMBuffer.AUTHOR
                                                + "\" sent you a Discord Direct Message. Your next answer will be sent to the user as your answer to the Discord Direct Message. The message content is: ";


                                            if (authorMessages.Count > 1)
                                            {
                                                contextString = @"( * THESE DIRECT MESSAGES TIME STARTED AT " + formattedDateTime + "* ) [ * User \"" + Global.currentDMBuffer.AUTHOR
                                                + "\" sent you some Discord Direct Messages. Your next answer will be sent to the user as your answer to these Discord Direct Messages. The message contents are: ";

                                                byte messageIndex = 0;
                                                foreach (var message in authorMessages)
                                                {
                                                    if (messageIndex == 0)
                                                    {
                                                        mergedMessagesString += " \" " + message.CONTENT + " \" ";
                                                    }
                                                    else
                                                    {
                                                        mergedMessagesString += " and then said \" " + message.CONTENT + " \" ";
                                                    }
                                                    messageIndex++;
                                                }

                                                mergedMessagesString = mergedMessagesString + "* ]";
                                            }
                                            else
                                            {
                                                mergedMessagesString += " \" " + authorMessages[0].CONTENT + " \" * ] ";
                                            }

                                            // Now, simulate the input with the merged message content
                                            SimulateInput(driver, prePrompt + contextString + mergedMessagesString);

                                            // Write to the log
                                            ChAIO.WriteScrapper("(DM) Simulated Input: " + prePrompt + Global.discordChatBuffer);

                                            // Remove all messages from the same author in the DM buffer list
                                            Global.DMBufferList.RemoveAll(msg => msg.AUTHOR == oldestDM.AUTHOR);
                                        }
                                    }

                                    break;
                                }
                                lock (Global.lockInternalData)
                                {
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
                                if (Global.discordChatBuffer != "" && (Global.DMBufferList.Count == 0 || Global.botYTVirtualWatch))
                                {
                                    lock (Global.lockInternalData)
                                    {
                                        SimulateInput(driver, prePrompt + Global.discordChatBuffer);
                                        ChAIO.WriteScrapper("(MC) Simulated Input: " + prePrompt + Global.discordChatBuffer);
                                        Global.discordChatBuffer = "";
                                        Global.lastDiscordInteractionTime = DateTime.Now;
                                    }
                                    break;
                                }
                                else if (prePrompt != "")
                                {
                                    SimulateInput(driver, prePrompt);
                                    ChAIO.WriteScrapper("(OT) Simulated Input: " + prePrompt);
                                    break;
                                }
                                TimeSpan lastInteractionSpan = DateTime.Now - Global.lastDiscordInteractionTime;
                                if (Global.idleMin != 0 && Global.idleMax != 0)
                                {
                                    if (lastInteractionSpan.TotalMinutes >= Global.randomIdleInteraction)
                                    {
                                        lock (Global.lockInternalData)
                                        {
                                            SetBotStatus("Generating Idle Activity...");
                                            Global.lastDiscordInteractionTime = DateTime.Now;
                                            Global.randomIdleInteraction = Global.mainRandom.Next(Global.idleMin, Global.idleMax);
                                            DateTime now = DateTime.Now;
                                            string formattedDateTime = now.ToString("HH ':' mm '(24h format hour)' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                                            string idleString = @"[DISCORD MAIN PUBLIC TEXT CHANNEL]( * LOCAL TIME IS " + formattedDateTime + @"* ) ( * NOBODY WROTE ON THE DISCORD CHAT FOR " + Math.Floor(lastInteractionSpan.TotalMinutes).ToString() + @" MINUTES. "
                                            + @"YOU MAY TRY TO PING SOMEONE OR EVERYONE AND ASK FOR CHAT OR ALTERNATIVELY ENGAGE YOURSELF INTO AN ACTIVITY MEANWHILE * )";
                                            ChAIO.WriteScrapper("Simulated Input: " + idleString);
                                            SimulateInput(driver, idleString);
                                        }
                                        break;
                                    }
                                }
                                if (Global.programSoftResetFlag)
                                {
                                    return;
                                }
                                Thread.Sleep(50);
                            }
                            catch (Exception ex)
                            {
                                ChAIO.WriteScrapper("Exception in DC Scrapper Buffer: " + ex.ToString());
                                Global.AppendToFile("ErrorLog.txt", ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        ChAIO.WriteScrapper("No answers found on the webpage.");
                    }
                    Thread.Sleep(5000);
                }
            }
            return;
        }
        public static string[] ScrapeAnswers(IWebDriver driver, bool refresh)
        {
            try
            {
                if (!refresh)
                {
                    lock (Global.lockInternalData)
                    {
                        Global.botWrittingState = true;
                    }
                }

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                Func<IWebDriver, bool> domStable = new Func<IWebDriver, bool>((IWebDriver d) =>
                {
                    string initialPageSource = d.PageSource;
                    Thread.Sleep(Global.timeout); // This was 1000 but it used to cut messages
                    return initialPageSource == d.PageSource;
                });

                wait.Until(domStable);

                lock (Global.lockInternalData)
                {
                    Global.botWrittingState = false;
                }

                if (!refresh)
                {
                    lock (Global.lockInternalData)
                    {
                        if (Global.discordBotIAName == "Unknown")
                        {
                            try
                            {
                                var pElement = driver.FindElement(By.CssSelector("p.font-semi-bold.line-clamp-1.text-ellipsis.break-anywhere.overflow-hidden.whitespace-normal"));
                                Global.discordBotScrapedIAName = pElement.Text;
                                ChAIO.WriteScrapper("SCRAPED IA NAME: " + Global.discordBotScrapedIAName);
                                if (AINameLabel.InvokeRequired)
                                {
                                    AINameLabel.Invoke(new Action(() =>
                                    {
                                        AINameLabel.Text = Global.discordBotScrapedIAName;
                                    }));
                                }
                                else
                                {
                                    AINameLabel.Text = Global.discordBotScrapedIAName;
                                }
                            }
                            catch (NoSuchElementException)
                            {
                                ChAIO.WriteScrapper("It was not possible to scrap the current text!");
                                Global.AppendToFile("ErrorLog.txt", "It was not possible to scrap the current text!");
                            }
                        }
                    }

                    try
                    {
                        if (Global.loadedData.CHANGEPROFILEPICTURE)
                        {
                            if (!Global.discordImageFlag)
                            {
                                var imgElement = driver.FindElement(By.CssSelector("span img"));
                                string imageUrl = imgElement.GetAttribute("src");
                                var httpClient = new HttpClient();
                                lock (Global.lockInternalData)
                                {
                                    Global.discordImageBytes = httpClient.GetByteArrayAsync(imageUrl).Result;
                                    Global.discordImageFlag = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ChAIO.WriteScrapper("It was not possible to scrap the current AI Image!: " + ex.ToString());
                        Global.AppendToFile("ErrorLog.txt", ex.ToString());
                    }
                }

                var divElements = driver.FindElements(By.CssSelector(".mt-1.max-w-xl.rounded-2xl.px-3.min-h-12.flex.justify-center.py-3.bg-surface-elevation-2"));
                //var divTexts = divElements.Select(div => div.Text).ToList();
                var divTexts = divElements.Select(div => RestoreHtmlCharacters(FormatDiscordText(div.GetAttribute("innerHTML")))).ToList();

                if (!refresh)
                {
                    if (Global.botAudioMode)
                    {
                        try
                        {
                            var buttons = driver.FindElements(By.CssSelector("button"));
                            foreach (var button in buttons)
                            {
                                try
                                {
                                    var svgElement = button.FindElement(By.CssSelector("svg.size-4.text-voice-blue"));
                                    if (svgElement != null)
                                    {
                                        ChAIO.WriteScrapper("Button for audio mode exists. Recording and clicking the button.");
                                        button.Click();
                                        RecordBotAudio();
                                        ConvertToOgg(outputFilePath, "AIVoiceMessage.ogg");
                                        break;
                                    }
                                }
                                catch (NoSuchElementException)
                                {
                                    // Continue if the current button does not contain the specified SVG
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ChAIO.WriteScrapper("Exception occurred while finding the button: " + ex.ToString());
                            Global.AppendToFile("ErrorLog.txt", ex.ToString());
                        }
                    }
                }

                return divTexts.ToArray();
            }
            catch (NoSuchElementException)
            {
                return new string[0];
            }
        }
        public static string RestoreHtmlCharacters(string input)
        {
            return WebUtility.HtmlDecode(input);
        }
        public static string FormatDiscordText(string htmlContent)
        {
            // Define a regex pattern to remove non-formatting tags
            string pattern = @"<(?!\/?(em|b|i|u|code|strike|pre|br)\b)[^>]+>";

            // Remove all non-formatting HTML tags using regex
            string cleanContent = Regex.Replace(htmlContent, pattern, "");

            // Now handle the allowed tags and convert them to Discord markdown format
            cleanContent = cleanContent.Replace("<em>", "*").Replace("</em>", "*");
            cleanContent = cleanContent.Replace("<b>", "**").Replace("</b>", "**");
            cleanContent = cleanContent.Replace("<i>", "*").Replace("</i>", "*");
            cleanContent = cleanContent.Replace("<u>", "_").Replace("</u>", "_");
            cleanContent = cleanContent.Replace("<strike>", "~~").Replace("</strike>", "~~");
            cleanContent = cleanContent.Replace("<code>", "`").Replace("</code>", "`");
            cleanContent = cleanContent.Replace("<pre>", "`").Replace("</pre>", "`");

            // Convert <br> tags to newlines
            cleanContent = cleanContent.Replace("<br>", "\n");

            return cleanContent;
        }
        static void SimulateInput(IWebDriver driver, string text)
        {
            if (text != null)
            {
                try
                {
                    var chatBox = driver.FindElement(By.TagName("textarea"));
                    chatBox.SendKeys(text);
                    chatBox.SendKeys(OpenQA.Selenium.Keys.Enter);
                }
                catch (NoSuchElementException ex)
                {
                    ChAIO.WriteScrapper("Chat textarea not found: " + ex.Message);
                    Global.AppendToFile("ErrorLog.txt", ex.ToString());
                }
                finally
                {
                    Global.simulatedInputsCounter++;
                }
            }
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
        public static bool CheckButtonsExist(IWebDriver driver, out List<IWebElement> buttons)
        {
            buttons = null;

            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(driver =>
                {
                    return ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete");
                });

                wait.Until(driver =>
                {
                    var elements = driver.FindElements(By.CssSelector("button.inline-flex.items-center.justify-center.h-6")).ToList();
                    return elements.Count > 0;
                });

                buttons = driver.FindElements(By.CssSelector("button.inline-flex.items-center.justify-center.h-6")).ToList();

                return buttons.Count > 0;
            }
            catch (WebDriverTimeoutException)
            {
                buttons = null;
                return false;
            }
            catch (NoSuchElementException)
            {
                buttons = null;
                return false;
            }
        }
        public static void RecordBotAudio()
        {
            isSilent = false;
            StartRecording();

            ChAIDiscordBot.EnqueueMessage(Global.discordChannelID, "> * " + Global.discordBotIAName + " is recording a voice note...");

            // Wait for the recording to stop due to silence
            while (!isSilent)
            {
                System.Threading.Thread.Sleep(100); // Check every 100ms if silence has been detected
            }

            StopRecording();
            ChAIO.WriteScrapper("Recording stopped!");
        }
        private static void StartRecording()
        {
            ChAIO.WriteScrapper("Recording started!");

            loopbackCapture = new WasapiLoopbackCapture();
            loopbackCapture.DataAvailable += OnDataAvailable;
            loopbackCapture.RecordingStopped += OnRecordingStopped;

            writer = new WaveFileWriter(outputFilePath, loopbackCapture.WaveFormat);
            loopbackCapture.StartRecording();
            lastSoundDetectedTime = DateTime.Now;
            isSilent = false;
            soundEverDetected = false;
        }
        private static void StopRecording()
        {
            loopbackCapture.StopRecording();
            loopbackCapture.Dispose();
        }
        private static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);

            // Check for silence
            bool soundDetected = false;
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                var sample = BitConverter.ToInt16(e.Buffer, index);

                int absSample = (sample == short.MinValue) ? short.MaxValue : Math.Abs(sample);

                if (Math.Abs(absSample) > 300) // Adjusted threshold for detecting sound
                {
                    soundEverDetected = true;
                    soundDetected = true;
                    break;
                }
            }

            if (soundDetected)
            {
                lastSoundDetectedTime = DateTime.Now;
                isSilent = false;
            }
            else if (soundEverDetected)
            {
                if (DateTime.Now - lastSoundDetectedTime > TimeSpan.FromSeconds(1))
                {
                    isSilent = true;
                }
            }
            else if (DateTime.Now - lastSoundDetectedTime > SilenceThreshold)
            {
                isSilent = true;
            }
        }
        private static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer?.Dispose();
            writer = null;
            loopbackCapture.Dispose();
        }
        public static void ConvertToOgg(string wavFilePath, string oggFilePath)
        {
            if (File.Exists(wavFilePath))
            {
                using (var reader = new WaveFileReader(wavFilePath))
                {
                    using (var writer = new LameMP3FileWriter(oggFilePath, reader.WaveFormat, 128))
                    {
                        reader.CopyTo(writer);
                    }
                }

                lock (Global.lockInternalData)
                {
                    Global.botAudioGenerated = true;
                }

                ChAIO.WriteScrapper("WAV to OGG Conversion succesful!");
            }
            else
            {
                ChAIO.WriteScrapper("WAV file not found!");
            }
        }
        public static void OnProcessExit(ChromeDriver driver)
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }
}
