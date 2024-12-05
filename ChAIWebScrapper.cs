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

namespace ChAIScrapper
{
    public static class ChAIWebScraper
    {
        private static WasapiLoopbackCapture loopbackCapture;
        private static WaveFileWriter writer;
        private static string outputFilePath = "tmpBotAudio.wav";
        private static DateTime lastSoundDetectedTime;
        private static bool isSilent = false;
        private static bool soundEverDetected = false;
        private static readonly TimeSpan SilenceThreshold = TimeSpan.FromSeconds(3);

        public static void RunWebScraper(CancellationTokenSource cancellationToken)
        {
            ChromeOptions chromeOptions = new ChromeOptions();

            chromeOptions.DebuggerAddress = Global.chromeDebuggerAddress;
            chromeOptions.BinaryLocation = Global.portableChromiumPath;

            bool flagFirstTime = true;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                driver.Navigate().GoToUrl(Global.characterAIChatURL);

                if (flagFirstTime && Global.initialBotBriefing != "")
                {
                    Thread.Sleep(3000);
                    SimulateInput(driver, Global.initialBotBriefing);
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
                                Global.lastDiscordAnswer = Global.lastFeasibleAnswer;
                            }
                            ChAIScrapperProgram.Write("Last Answer:");
                            ChAIScrapperProgram.Write(Global.lastFeasibleAnswer);
                        }
                        else
                        {
                            ChAIScrapperProgram.Write("No new answers!");
                        }
                        ChAIScrapperProgram.Write("Waiting for Discord Chat Buffer Input...");
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

                                }
                                if ((DateTime.Now - Global.lastPageRefresh).TotalMinutes >= 20)
                                {
                                    driver.Navigate().Refresh();
                                    Global.lastPageRefresh = DateTime.Now;
                                    Thread.Sleep(2000);
                                    ScrapeAnswers(driver, true);
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
                                                ChAIScrapperProgram.Write("Simulated Input: " + virtualWatchIntroString);
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
                                                    ChAIScrapperProgram.Write("Simulated Input: " + virtualWatchString + virtualCaptions + virtualPrompt);
                                                    Global.botYTVirtualWatchLocalTime = DateTime.Now;
                                                    Global.lastDiscordInteractionTime = DateTime.Now;
                                                }
                                                else
                                                {
                                                    string virtualWatchString = "";
                                                    virtualWatchString = @"[ YouTube video ended! ]"; // You check the comments and the most liked comment states: "" " + Global.botYTWatchData.YTMAINCOMMENT + @" "" ]";
                                                    prePrompt += virtualWatchString;
                                                    ChAIScrapperProgram.Write("Simulated Input: " + virtualWatchString);
                                                    Global.botYTVirtualWatchLocalTime = DateTime.Now;
                                                    Global.lastDiscordInteractionTime = DateTime.Now;
                                                    Global.botYTWatchData = null;
                                                    Global.botYTVirtualWatch = false;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (Global.discordChatBuffer != "")
                                {
                                    lock (Global.lockInternalData)
                                    {
                                        SimulateInput(driver, prePrompt + Global.discordChatBuffer);
                                        ChAIScrapperProgram.Write("Simulated Input: " + prePrompt + Global.discordChatBuffer);
                                        Global.discordChatBuffer = "";
                                        Global.lastDiscordInteractionTime = DateTime.Now;
                                    }
                                    break;
                                }
                                else if (prePrompt != "")
                                {
                                    SimulateInput(driver, prePrompt);
                                    break;
                                }
                                TimeSpan lastInteractionSpan = DateTime.Now - Global.lastDiscordInteractionTime;
                                if (Global.idleMin != 0 && Global.idleMax != 0)
                                {
                                    if (lastInteractionSpan.TotalMinutes >= Global.randomIdleInteraction)
                                    {
                                        lock (Global.lockInternalData)
                                        {
                                            Global.lastDiscordInteractionTime = DateTime.Now;
                                            Global.randomIdleInteraction = Global.mainRandom.Next(Global.idleMin, Global.idleMax);
                                            DateTime now = DateTime.Now;
                                            string formattedDateTime = now.ToString("HH ':' mm '24h format hour' dd 'Day' dddd 'Month' MMMM 'Year' yyyy");
                                            string idleString = @"( * LOCAL TIME IS " + formattedDateTime + @"* ) ( * NOBODY WROTE ON THE DISCORD CHAT FOR " + Math.Floor(lastInteractionSpan.TotalMinutes).ToString() + @" MINUTES. "
                                            + @"YOU MAY TRY TO PING SOMEONE OR EVERYONE AND ASK FOR CHAT OR ALTERNATIVELY ENGAGE YOURSELF INTO AN ACTIVITY MEANWHILE * )";
                                            ChAIScrapperProgram.Write("Simulated Input: " + idleString);
                                            SimulateInput(driver, idleString);
                                        }
                                        break;
                                    }
                                }
                                if (Global.discordFeedbackLevel > 0)
                                {
                                    SimulateFeedback(driver, Global.discordFeedbackLevel);
                                    Global.discordFeedbackLevel = 0;
                                }
                                if (Global.programSoftResetFlag)
                                {
                                    return;
                                }
                                lock (Global.lockInternalData)
                                {
                                    while (Global.botDisableFlag)
                                    {
                                        Thread.Sleep(1000);
                                    }
                                }
                                Thread.Sleep(50);
                            }
                            catch (Exception ex)
                            {
                                ChAIScrapperProgram.Write("Exception in DC Scrapper Buffer: " + ex.ToString());
                                Global.AppendToFile("ErrorLog.txt", ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        ChAIScrapperProgram.Write("No answers found on the webpage.");
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
                    Thread.Sleep(2000); // This was 1000 but it used to cut messages
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
                                ChAIScrapperProgram.Write("SCRAPED IA NAME: " + Global.discordBotScrapedIAName);
                            }
                            catch (NoSuchElementException)
                            {
                                ChAIScrapperProgram.Write("It was not possible to scrap the current text!");
                                Global.AppendToFile("ErrorLog.txt", "It was not possible to scrap the current text!");
                            }
                        }
                    }

                    try
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
                    catch (Exception ex)
                    {
                        ChAIScrapperProgram.Write("It was not possible to scrap the current AI Image!: " + ex.ToString());
                        Global.AppendToFile("ErrorLog.txt", ex.ToString());
                    }
                }
                
                var divElements = driver.FindElements(By.CssSelector(".mt-1.max-w-xl.rounded-2xl.px-3.min-h-12.flex.justify-center.py-3.bg-surface-elevation-2"));
                var divTexts = divElements.Select(div => div.Text).ToList();

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
                                        ChAIScrapperProgram.Write("Button for audio mode exists. Recording and clicking the button.");
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
                            ChAIScrapperProgram.Write("Exception occurred while finding the button: " + ex.ToString());
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
        static void SimulateInput(IWebDriver driver, string text)
        {
            if (text != null)
            {
                try
                {
                    var chatBox = driver.FindElement(By.TagName("textarea"));
                    chatBox.SendKeys(text);
                    chatBox.SendKeys(Keys.Enter);
                }
                catch (NoSuchElementException ex)
                {
                    ChAIScrapperProgram.Write("Chat textarea not found: " + ex.Message);
                    Global.AppendToFile("ErrorLog.txt", ex.ToString());
                }
                finally
                {
                    Global.simulatedInputsCounter++;
                }
            }
        }
        public static string RemoveEmojis(string text)
        {
            try
            {
                var EmojiPattern = @"[#*0-9]\uFE0F?\u20E3|©\uFE0F?|[®\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA]\uFE0F?|[\u231A\u231B]|[\u2328\u23CF]\uFE0F?|[\u23E9-\u23EC]|[\u23ED-\u23EF]\uFE0F?|\u23F0|[\u23F1\u23F2]\uFE0F?|\u23F3|[\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB\u25FC]\uFE0F?|[\u25FD\u25FE]|[\u2600-\u2604\u260E\u2611]\uFE0F?|[\u2614\u2615]|\u2618\uFE0F?|\u261D(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642]\uFE0F?|[\u2648-\u2653]|[\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E]\uFE0F?|\u267F|\u2692\uFE0F?|\u2693|[\u2694-\u2697\u2699\u269B\u269C\u26A0]\uFE0F?|\u26A1|\u26A7\uFE0F?|[\u26AA\u26AB]|[\u26B0\u26B1]\uFE0F?|[\u26BD\u26BE\u26C4\u26C5]|\u26C8\uFE0F?|\u26CE|[\u26CF\u26D1\u26D3]\uFE0F?|\u26D4|\u26E9\uFE0F?|\u26EA|[\u26F0\u26F1]\uFE0F?|[\u26F2\u26F3]|\u26F4\uFE0F?|\u26F5|[\u26F7\u26F8]\uFE0F?|\u26F9(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\u26FA\u26FD]|\u2702\uFE0F?|\u2705|[\u2708\u2709]\uFE0F?|[\u270A\u270B](?:\uD83C[\uDFFB-\uDFFF])?|[\u270C\u270D](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\u270F\uFE0F?|[\u2712\u2714\u2716\u271D\u2721]\uFE0F?|\u2728|[\u2733\u2734\u2744\u2747]\uFE0F?|[\u274C\u274E\u2753-\u2755\u2757]|\u2763\uFE0F?|\u2764(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79)|\uFE0F(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79))?)?|[\u2795-\u2797]|\u27A1\uFE0F?|[\u27B0\u27BF]|[\u2934\u2935\u2B05-\u2B07]\uFE0F?|[\u2B1B\u2B1C\u2B50\u2B55]|[\u3030\u303D\u3297\u3299]\uFE0F?|\uD83C(?:[\uDC04\uDCCF]|[\uDD70\uDD71\uDD7E\uDD7F]\uFE0F?|[\uDD8E\uDD91-\uDD9A]|\uDDE6\uD83C[\uDDE8-\uDDEC\uDDEE\uDDF1\uDDF2\uDDF4\uDDF6-\uDDFA\uDDFC\uDDFD\uDDFF]|\uDDE7\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEF\uDDF1-\uDDF4\uDDF6-\uDDF9\uDDFB\uDDFC\uDDFE\uDDFF]|\uDDE8\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDEE\uDDF0-\uDDF5\uDDF7\uDDFA-\uDDFF]|\uDDE9\uD83C[\uDDEA\uDDEC\uDDEF\uDDF0\uDDF2\uDDF4\uDDFF]|\uDDEA\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDED\uDDF7-\uDDFA]|\uDDEB\uD83C[\uDDEE-\uDDF0\uDDF2\uDDF4\uDDF7]|\uDDEC\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEE\uDDF1-\uDDF3\uDDF5-\uDDFA\uDDFC\uDDFE]|\uDDED\uD83C[\uDDF0\uDDF2\uDDF3\uDDF7\uDDF9\uDDFA]|\uDDEE\uD83C[\uDDE8-\uDDEA\uDDF1-\uDDF4\uDDF6-\uDDF9]|\uDDEF\uD83C[\uDDEA\uDDF2\uDDF4\uDDF5]|\uDDF0\uD83C[\uDDEA\uDDEC-\uDDEE\uDDF2\uDDF3\uDDF5\uDDF7\uDDFC\uDDFE\uDDFF]|\uDDF1\uD83C[\uDDE6-\uDDE8\uDDEE\uDDF0\uDDF7-\uDDFB\uDDFE]|\uDDF2\uD83C[\uDDE6\uDDE8-\uDDED\uDDF0-\uDDFF]|\uDDF3\uD83C[\uDDE6\uDDE8\uDDEA-\uDDEC\uDDEE\uDDF1\uDDF4\uDDF5\uDDF7\uDDFA\uDDFF]|\uDDF4\uD83C\uDDF2|\uDDF5\uD83C[\uDDE6\uDDEA-\uDDED\uDDF0-\uDDF3\uDDF7-\uDDF9\uDDFC\uDDFE]|\uDDF6\uD83C\uDDE6|\uDDF7\uD83C[\uDDEA\uDDF4\uDDF8\uDDFA\uDDFC]|\uDDF8\uD83C[\uDDE6-\uDDEA\uDDEC-\uDDF4\uDDF7-\uDDF9\uDDFB\uDDFD-\uDDFF]|\uDDF9\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDED\uDDEF-\uDDF4\uDDF7\uDDF9\uDDFB\uDDFC\uDDFF]|\uDDFA\uD83C[\uDDE6\uDDEC\uDDF2\uDDF3\uDDF8\uDDFE\uDDFF]|\uDDFB\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDEE\uDDF3\uDDFA]|\uDDFC\uD83C[\uDDEB\uDDF8]|\uDDFD\uD83C\uDDF0|\uDDFE\uD83C[\uDDEA\uDDF9]|\uDDFF\uD83C[\uDDE6\uDDF2\uDDFC]|\uDE01|\uDE02\uFE0F?|[\uDE1A\uDE2F\uDE32-\uDE36]|\uDE37\uFE0F?|[\uDE38-\uDE3A\uDE50\uDE51\uDF00-\uDF20]|[\uDF21\uDF24-\uDF2C]\uFE0F?|[\uDF2D-\uDF35]|\uDF36\uFE0F?|[\uDF37-\uDF7C]|\uDF7D\uFE0F?|[\uDF7E-\uDF84]|\uDF85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDF86-\uDF93]|[\uDF96\uDF97\uDF99-\uDF9B\uDF9E\uDF9F]\uFE0F?|[\uDFA0-\uDFC1]|\uDFC2(?:\uD83C[\uDFFB-\uDFFF])?|[\uDFC3\uDFC4](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFC5\uDFC6]|\uDFC7(?:\uD83C[\uDFFB-\uDFFF])?|[\uDFC8\uDFC9]|\uDFCA(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCB\uDFCC](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCD\uDFCE]\uFE0F?|[\uDFCF-\uDFD3]|[\uDFD4-\uDFDF]\uFE0F?|[\uDFE0-\uDFF0]|\uDFF3(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08)|\uFE0F(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08))?)?|\uDFF4(?:\u200D\u2620\uFE0F?|\uDB40\uDC67\uDB40\uDC62\uDB40(?:\uDC65\uDB40\uDC6E\uDB40\uDC67|\uDC73\uDB40\uDC63\uDB40\uDC74|\uDC77\uDB40\uDC6C\uDB40\uDC73)\uDB40\uDC7F)?|[\uDFF5\uDFF7]\uFE0F?|[\uDFF8-\uDFFF])|\uD83D(?:[\uDC00-\uDC07]|\uDC08(?:\u200D\u2B1B)?|[\uDC09-\uDC14]|\uDC15(?:\u200D\uD83E\uDDBA)?|[\uDC16-\uDC3A]|\uDC3B(?:\u200D\u2744\uFE0F?)?|[\uDC3C-\uDC3E]|\uDC3F\uFE0F?|\uDC40|\uDC41(?:\u200D\uD83D\uDDE8\uFE0F?|\uFE0F(?:\u200D\uD83D\uDDE8\uFE0F?)?)?|[\uDC42\uDC43](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC44\uDC45]|[\uDC46-\uDC50](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC51-\uDC65]|[\uDC66\uDC67](?:\uD83C[\uDFFB-\uDFFF])?|\uDC68(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|[\uDC68\uDC69]\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD])|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFC-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFD-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFD\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFE]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|\uDC69(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?[\uDC68\uDC69]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|\uDC69\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD])|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFC-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFD-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFD\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFE]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|\uDC6A|[\uDC6B-\uDC6D](?:\uD83C[\uDFFB-\uDFFF])?|\uDC6E(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC6F(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDC70\uDC71](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC72(?:\uD83C[\uDFFB-\uDFFF])?|\uDC73(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC74-\uDC76](?:\uD83C[\uDFFB-\uDFFF])?|\uDC77(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC78(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC79-\uDC7B]|\uDC7C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC7D-\uDC80]|[\uDC81\uDC82](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC83(?:\uD83C[\uDFFB-\uDFFF])?|\uDC84|\uDC85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC86\uDC87](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC88-\uDC8E]|\uDC8F(?:\uD83C[\uDFFB-\uDFFF])?|\uDC90|\uDC91(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC92-\uDCA9]|\uDCAA(?:\uD83C[\uDFFB-\uDFFF])?|[\uDCAB-\uDCFC]|\uDCFD\uFE0F?|[\uDCFF-\uDD3D]|[\uDD49\uDD4A]\uFE0F?|[\uDD4B-\uDD4E\uDD50-\uDD67]|[\uDD6F\uDD70\uDD73]\uFE0F?|\uDD74(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\uDD75(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD76-\uDD79]\uFE0F?|\uDD7A(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD87\uDD8A-\uDD8D]\uFE0F?|\uDD90(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\uDD95\uDD96](?:\uD83C[\uDFFB-\uDFFF])?|\uDDA4|[\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA]\uFE0F?|[\uDDFB-\uDE2D]|\uDE2E(?:\u200D\uD83D\uDCA8)?|[\uDE2F-\uDE34]|\uDE35(?:\u200D\uD83D\uDCAB)?|\uDE36(?:\u200D\uD83C\uDF2B\uFE0F?)?|[\uDE37-\uDE44]|[\uDE45-\uDE47](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDE48-\uDE4A]|\uDE4B(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE4D\uDE4E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE80-\uDEA2]|\uDEA3(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDEA4-\uDEB3]|[\uDEB4-\uDEB6](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDEB7-\uDEBF]|\uDEC0(?:\uD83C[\uDFFB-\uDFFF])?|[\uDEC1-\uDEC5]|\uDECB\uFE0F?|\uDECC(?:\uD83C[\uDFFB-\uDFFF])?|[\uDECD-\uDECF]\uFE0F?|[\uDED0-\uDED2\uDED5-\uDED7]|[\uDEE0-\uDEE5\uDEE9]\uFE0F?|[\uDEEB\uDEEC]|[\uDEF0\uDEF3]\uFE0F?|[\uDEF4-\uDEFC\uDFE0-\uDFEB])|\uD83E(?:\uDD0C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD0D\uDD0E]|\uDD0F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD10-\uDD17]|[\uDD18-\uDD1C](?:\uD83C[\uDFFB-\uDFFF])?|\uDD1D|[\uDD1E\uDD1F](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD20-\uDD25]|\uDD26(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD27-\uDD2F]|[\uDD30-\uDD34](?:\uD83C[\uDFFB-\uDFFF])?|\uDD35(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD36(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD37-\uDD39](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD3A|\uDD3C(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDD3D\uDD3E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD3F-\uDD45\uDD47-\uDD76]|\uDD77(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD78\uDD7A-\uDDB4]|[\uDDB5\uDDB6](?:\uD83C[\uDFFB-\uDFFF])?|\uDDB7|[\uDDB8\uDDB9](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDBA|\uDDBB(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDBC-\uDDCB]|[\uDDCD-\uDDCF](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD0|\uDDD1(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1|[\uDDAF-\uDDB3\uDDBC\uDDBD]))|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFC-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFD-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFD\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFE]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|[\uDDD2\uDDD3](?:\uD83C[\uDFFB-\uDFFF])?|\uDDD4(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD5(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDD6-\uDDDD](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDDDE\uDDDF](?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDDE0-\uDDFF\uDE70-\uDE74\uDE78-\uDE7A\uDE80-\uDE86\uDE90-\uDEA8\uDEB0-\uDEB6\uDEC0-\uDEC2\uDED0-\uDED6])";
                string result = Regex.Replace(text, EmojiPattern, string.Empty);

                return result;
            }
            catch
            {
                return "ChAIScrapper Error: Oops! There was a funny character on the string!";
            }
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
        public static void SimulateFeedback(IWebDriver driver, int feedbackLevel)
        {
            if (feedbackLevel < 1 || feedbackLevel > 4)
            {
                feedbackLevel = 4;
            }

            if (CheckButtonsExist(driver, out List<IWebElement> buttons))
            {
                IWebElement buttonToClick = buttons[Math.Min(feedbackLevel - 1, buttons.Count - 1)];
                buttonToClick.Click();

                ChAIScrapperProgram.Write($"Button {feedbackLevel} clicked successfully!");
            }
            else
            {
                ChAIScrapperProgram.Write("Required buttons are not present on the page.");
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
            ChAIScrapperProgram.Write("Recording stopped!");
        }
        private static void StartRecording()
        {
            ChAIScrapperProgram.Write("Recording started!");

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

                ChAIScrapperProgram.Write("WAV to OGG Conversion succesful!");
            }
            else
            {
                ChAIScrapperProgram.Write("WAV file not found!");
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
