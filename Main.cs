using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Discord;
using Discord.WebSocket;
using System.Threading.Channels;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ChAIScrapper
{
    public class ChAIScrapperProgram
    {
        public static object saveLock = new object();
        public static object loadLock = new object();
        public class SavedData
        {
            public string DISCORDTOKEN = "";
            public ulong DISCORDCHANNELID = 0;
            public ulong DISCORDBOTUID = 0;
            public string CHAIURL = "https://character.ai/chat/VhfYMgO4Agqz_ZI5tHkQ9DyDFgEoMVK3JkrM-1QDlz8";
            public bool ALLOWAUDIOS = true;
            public bool ALLOWYTVIDEOS = true;
            public bool HEADLESS = true;
            public int IDLEMIN = 15;
            public int IDLEMAX = 75;
        }
        public static async Task Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("ChAIScrapper Discord Bot Service");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("    ");
                    Console.WriteLine("https://karstskarn.carrd.co");

                    try
                    {
                        if (File.Exists("ChAISData.xml"))
                        {
                            SavedData loadedData = LoadObjectFromFile<SavedData>("ChAISData.xml");
                            Global.characterAIChatURL = loadedData.CHAIURL;
                            Global.allowBotAudios = loadedData.ALLOWAUDIOS;
                            Global.allowYTVirtualWatch = loadedData.ALLOWYTVIDEOS;
                            Global.chromeHeadlessMode = false; // loadedData.HEADLESS;
                            Global.discordBotToken = loadedData.DISCORDTOKEN;
                            Global.discordBotUserID = loadedData.DISCORDBOTUID;
                            Global.discordChannelID = loadedData.DISCORDCHANNELID;
                            Global.idleMin = loadedData.IDLEMIN;
                            Global.idleMax = loadedData.IDLEMAX;
                            Global.loadedData = loadedData;
                        }
                        else
                        {
                            SavedData newData = new SavedData();
                            newData.CHAIURL = Global.characterAIChatURL;
                            newData.ALLOWAUDIOS = Global.allowBotAudios;
                            newData.ALLOWYTVIDEOS = Global.allowYTVirtualWatch;
                            newData.HEADLESS = Global.chromeHeadlessMode;
                            newData.DISCORDTOKEN = "YOUR_DISCORD_TOKEN_HERE";
                            newData.DISCORDBOTUID = 0;
                            newData.DISCORDCHANNELID = 0;
                            newData.IDLEMIN = 15;
                            newData.IDLEMAX = 75;
                            SaveObjectToFile<SavedData>(newData, "ChAISData.xml");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while loading ChAISData.xml on init!: " + ex.ToString());
                    }

                    var monitoringTask = MonitorTasks(cancellationTokenSource);

                    // Check for program soft reset flag
                    while (!Global.programSoftResetFlag)
                    {
                        await Task.Delay(500); // Check every second
                    }

                    // Set the flag to cancel tasks
                    cancellationTokenSource.Cancel();

                    await monitoringTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("General Error: " + ex.ToString());
                }

                ProgramSoftReset(0, ref cancellationTokenSource);

                Console.Clear();
            }
        }
        private static async Task MonitorTasks(CancellationTokenSource cancellationTokenSource)
        {
            var scrapingTask = Task.Run(() => ChAIWebScraper.RunWebScraper(cancellationTokenSource));
            var discordBotTask = ChAIDiscordBot.RunDiscordBotAsync(cancellationTokenSource);

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (scrapingTask.IsCompleted)
                {
                    if (scrapingTask.IsFaulted)
                    {
                        Write($"Scraping task faulted: {scrapingTask.Exception}");
                    }

                    // Restart the scraping task
                    scrapingTask = Task.Run(() => ChAIWebScraper.RunWebScraper(cancellationTokenSource));
                    Write("Scraping task restarted.");
                }

                if (discordBotTask.IsCompleted)
                {
                    if (discordBotTask.IsFaulted)
                    {
                        Write($"Discord bot task faulted: {discordBotTask.Exception}");
                    }

                    // Restart the discord bot task
                    discordBotTask = ChAIDiscordBot.RunDiscordBotAsync(cancellationTokenSource);
                    Write("Discord bot task restarted.");
                }

                // Wait a bit before checking again
                await Task.Delay(500);
            }

            // Wait for both tasks to complete after cancellation
            await Task.WhenAll(scrapingTask, discordBotTask);
        }
        public static void ProgramSoftReset(byte resetLevel, ref CancellationTokenSource cancellationTokenSource)
        {
            Global.chromeDebuggerAddress = "localhost:9222";

            try
            {
                if (File.Exists("ChAISData.xml"))
                {
                    SavedData loadedData = LoadObjectFromFile<SavedData>("ChAISData.xml");
                    Global.characterAIChatURL = loadedData.CHAIURL;
                    Global.allowBotAudios = loadedData.ALLOWAUDIOS;
                    Global.allowYTVirtualWatch = loadedData.ALLOWYTVIDEOS;
                    Global.chromeHeadlessMode = false; // loadedData.HEADLESS;
                    Global.discordBotToken = loadedData.DISCORDTOKEN;
                    Global.discordBotUserID = loadedData.DISCORDBOTUID;
                    Global.discordChannelID = loadedData.DISCORDCHANNELID;
                    Global.idleMin = loadedData.IDLEMIN;
                    Global.idleMax = loadedData.IDLEMAX;
                }
            }
            catch (Exception ex)
            {
                Write("Error Loading ChAISData.xml while soft reset!: " + ex.ToString());
            }

            Global.lastFeasibleAnswer = "";
            Global.lastDiscordAnswer = "";
            Global.previousDiscordAnswer = "";
            Global.discordChatBuffer = "";
            Global.discordImageBytes = null;
            Global.discordImageFlag = false;
            Global.discordImageUpdateFlag = false;
            Global.discordBotIAName = "Unknown";
            Global.discordBotScrapedIAName = "Unknown";
            Global.discordFeedbackLevel = 0;

            Global.botAudioMode = false;
            Global.botYTVirtualWatch = false;
            Global.botYTWatchData = null;
            Global.botYTIntro = false;
            Global.botYTPause = false;
            Global.botYTVirtualWatchTime = TimeSpan.Zero;

            Global.lastCharacterChangeTime = DateTime.Now;
            Global.randomIdleInteraction = Global.mainRandom.Next(Global.idleMin, Global.idleMax);
            Global.lastDiscordMessageTime = DateTime.Now;
            Global.lastDiscordInteractionTime = DateTime.Now;

            cancellationTokenSource = new CancellationTokenSource();
            Global.programSoftResetFlag = false;
        }
        public static void Write(string text)
        {
            lock (Global.lockWrite)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    DateTime currentTime = DateTime.Now;
                    string formattedTimeString = $"[ {currentTime:dd/MM/yyyy} - {currentTime:HH:mm:ss} ]";
                    Console.Write(formattedTimeString);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" " + text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while attempting to use the Write() function: " + ex.ToString());
                }
            }
        }
        public static void SaveObjectToFile<T>(T objectToSave, string filePath)
        {
            const int maxRetries = 100;
            const int minDelayMilliseconds = 20;
            const int maxDelayMilliseconds = 40;

            int retries = 0;
            Random random = new Random();

            while (retries < maxRetries)
            {
                try
                {
                    lock (saveLock)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (TextWriter writer = new StreamWriter(filePath))
                        {
                            serializer.Serialize(writer, objectToSave);
                        }
                    }

                    // Save successful, exit the loop
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    retries++;
                    int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                    System.Threading.Thread.Sleep(delayMilliseconds);
                }
            }
        }
        public static T LoadObjectFromFile<T>(string filePath)
        {
            const int maxRetries = 100;
            const int minDelayMilliseconds = 20;
            const int maxDelayMilliseconds = 40;

            int retries = 0;
            Random random = new Random();

            while (retries < maxRetries)
            {
                try
                {
                    lock (loadLock)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (TextReader reader = new StreamReader(filePath))
                        {
                            return (T)serializer.Deserialize(reader);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    retries++;
                    int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                    System.Threading.Thread.Sleep(delayMilliseconds);
                }
            }
            return default(T);
        }
    }
}
