using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Diagnostics;

namespace ChAIScrapper
{
    public static class Global
    {
        public static bool chromeHeadlessMode = false;
        public static string chromeDebuggerAddress = "localhost:9222";
        public static string characterAIChatURL = "https://character.ai/chat/xuHtpVIs5Pl6XGvIjOKx-8jsIOCm5xi_iG4H19gxOxA";
        public static string discordBotToken = "";
        public static ulong discordChannelID = 0;
        public static ulong discordBotUserID = 0;

        public static ChAIScrapperProgram.SavedData loadedData = new ChAIScrapperProgram.SavedData();

        public static int idleMin = 15;
        public static int idleMax = 75;
        public static string lastFeasibleAnswer = "";
        public static string lastDiscordAnswer = "";
        public static string previousDiscordAnswer = "";
        public static string discordChatBuffer = "";
        public static byte[] discordImageBytes = null;
        public static bool discordImageFlag = false;
        public static bool discordImageUpdateFlag = false;
        public static bool programSoftResetFlag = false;
        public static string discordBotIAName = "Unknown";
        public static string discordBotScrapedIAName = "Unknown";
        public static DateTime lastCharacterChangeTime = DateTime.Now;
        public static DateTime lastDiscordMessageTime = DateTime.Now;
        public static DateTime lastDiscordInteractionTime = DateTime.Now;
        public static DateTime lastPageRefresh = DateTime.Now;
        public static Random mainRandom = new Random();
        public static bool flagIdleSequence = false;
        public static int idleSequenceRepeat = mainRandom.Next(1, 10);
        public static int randomIdleInteraction = mainRandom.Next(15, 75);

        public static int discordFeedbackLevel = 0;

        public static bool botWrittingState = false;
        public static bool botAudioMode = false;
        public static bool botAudioGenerated = false;
        public static bool allowBotAudios = true;

        public static bool botYTVirtualWatch = false;
        public static bool allowYTVirtualWatch = true;
        public static bool botYTPause = false;
        public static bool botYTIntro = false;
        public static YTVirtualWatch botYTWatchData = null;
        public static TimeSpan botYTVirtualWatchPace = TimeSpan.FromSeconds(15);
        public static TimeSpan botYTVirtualWatchTime = TimeSpan.Zero;
        public static DateTime botYTVirtualWatchLocalTime = DateTime.Now;

        public static string tagTextbox = "textarea";

        public static object lockWrite = new object();
        public static object lockInternalData = new object();

        public static string initialBotBriefing = @"[ * DON'T ANSWER TO THIS MESSAGE. " +
            @"You are connected to a Discord channel where it can be multiple people. " +
            @"You can tag anyone and answer personally by using @username so using the name " +
            @"you see from each one in the chat you can tag them. You can also tag @everyone to ping them all! * ]" +
            @" [ * NOW PLEASE PROCEED TO SALUTE AND PRESENT YOURSELF TO THE CHANNEL * ]";
    }
}
