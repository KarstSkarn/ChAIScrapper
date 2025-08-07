using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChAIScrapperWF
{
    public static class ChAIDataStructures
    {
        public class ConfigData
        {
            public string DISCORDTOKEN = "";
            public ulong DISCORDCHANNELID = 0;
            public string CHAIURL = "https://character.ai/chat/VhfYMgO4Agqz_ZI5tHkQ9DyDFgEoMVK3JkrM-1QDlz8";
            public bool ALLOWAUDIOS = true;
            public bool ALLOWYTVIDEOS = true;
            public int IDLEMIN = 15;
            public int IDLEMAX = 75;
            public string CHROMIUMPORT = "9222";
            public List<string> USERSIGNORELIST = new List<string>();
            public List<string> ADMINISTRATIVEUSERSLIST = new List<string>();
            public bool ADMINISTRATIVELOCK = false;
            public bool ALLOWDMS = true;
            public bool CHANGEPROFILEPICTURE = true;
            public bool CHANGEUSERNAME = true;
            public string DISCRIMINATORYSTRING = "//";
            public bool DISCRIMINATORYEXCLUSIVE = false;
            public string USERNAMESTENCIL = "ChAIS(AI: USERNAME)";
            public string INITIALBOTBRIEFING = "";
            public ulong DISCORDSERVERID = 0;
            public int TIMEOUT = 5000;
        }
        public class DMBuffer
        {
            public string AUTHOR = "";
            public string CONTENT = "";
            public ulong ID = 0;
            public DateTime TIME = DateTime.MinValue;
        }
    }
}
