using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace ChAIScrapper
{
    public class YTVirtualWatch
    {
        public string YTUPLOADER { get; set; }
        public string YTTITLE { get; set; }
        public string YTDESCRIPTION { get; set; }
        public string YTMAINCOMMENT { get; set; }
        public TimeSpan LENGTH { get; set; }
        public List<YTCaptions> YTCAPTIONS { get; set; }
    }

    public class YTCaptions
    {
        public TimeSpan TIME { get; set; }
        public string CAPTION { get; set; }
    }

    public static class ChAIExternal
    {
        public static async Task<YTVirtualWatch> GetYouTubeSubtitlesAndDetailsAsync(string videoUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                string pageSource = await client.GetStringAsync(videoUrl);
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageSource);
                var scriptTags = htmlDoc.DocumentNode.SelectNodes("//script");
                string playerResponse = null;

                foreach (var script in scriptTags)
                {
                    if (script.InnerText.Contains("ytInitialPlayerResponse"))
                    {
                        playerResponse = script.InnerText;
                        break;
                    }
                }

                if (playerResponse == null)
                    return null;

                int jsonStartIndex = playerResponse.IndexOf('{');
                playerResponse = playerResponse.Substring(jsonStartIndex);
                playerResponse = playerResponse.Substring(0, playerResponse.LastIndexOf('}') + 1);

                JObject json = JObject.Parse(playerResponse);

                // Get uploader name
                string uploader = json["videoDetails"]?["author"]?.ToString() ?? "Unknown";

                // Get video title
                string title = json["videoDetails"]?["title"]?.ToString() ?? "Unknown";

                // Get video length
                string lengthSeconds = json["videoDetails"]?["lengthSeconds"]?.ToString() ?? "0";
                TimeSpan length = TimeSpan.FromSeconds(double.Parse(lengthSeconds));

                // Get video description
                string description = json["videoDetails"]?["shortDescription"]?.ToString() ?? "No description available";

                var captionTracks = json.SelectToken("..captionTracks");

                // Get most liked comment
                string mostLikedComment = "No comments available";
                var commentsToken = json.SelectToken("..topLevelComment");

                if (commentsToken != null)
                {
                    mostLikedComment = commentsToken["snippet"]?["textDisplay"]?.ToString() ?? "No comments available";
                }

                if (captionTracks == null)
                    return new YTVirtualWatch { YTUPLOADER = uploader, YTTITLE = title, YTCAPTIONS = new List<YTCaptions>(), LENGTH = length, YTDESCRIPTION = description, YTMAINCOMMENT = mostLikedComment };

                string subtitleUrl = captionTracks[0]["baseUrl"].ToString();

                // Download subtitles
                string subtitlesXml = await client.GetStringAsync(subtitleUrl);
                List<YTCaptions> subtitlesList = ParseSubtitles(subtitlesXml);

                return new YTVirtualWatch
                {
                    YTUPLOADER = uploader,
                    YTTITLE = title,
                    YTCAPTIONS = subtitlesList,
                    YTDESCRIPTION = description,
                    YTMAINCOMMENT = mostLikedComment,
                    LENGTH = length
                };
            }
        }

        public static List<YTCaptions> ParseSubtitles(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var texts = doc.GetElementsByTagName("text");
            var subtitlesList = new List<YTCaptions>();

            foreach (XmlNode text in texts)
            {
                if (text.Attributes["start"] != null)
                {
                    double startTime = double.Parse(text.Attributes["start"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    TimeSpan time = TimeSpan.FromSeconds(startTime);
                    string caption = System.Net.WebUtility.HtmlDecode(text.InnerText);

                    subtitlesList.Add(new YTCaptions
                    {
                        TIME = time,
                        CAPTION = caption
                    });
                }
            }

            return subtitlesList;
        }
        public static string YTGetCaptions(YTVirtualWatch ytWatchData, TimeSpan start, TimeSpan end)
        {
            if (end > ytWatchData.LENGTH)
            {
                end = ytWatchData.LENGTH;
            }

            var filteredCaptions = ytWatchData.YTCAPTIONS
                .FindAll(caption => caption.TIME >= start && caption.TIME <= end);

            string result = string.Join(" ", filteredCaptions.ConvertAll(caption => caption.CAPTION));
            return result;
        }
    }
}
