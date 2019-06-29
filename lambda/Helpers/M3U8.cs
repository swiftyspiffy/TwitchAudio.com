using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public static class M3U8
    {
        public static string GetAudioOnlyM3U8(string channelName)
        {
            var accessToken = GetAccessToken(channelName);
            var body = GetM3U8Body(channelName, accessToken);
            var link = ParseAudioOnlyLink(body);
            return link;
        }

        private static string ParseAudioOnlyLink(List<string> body)
        {
            bool nextLine = false;
            foreach (var line in body)
            {
                if (nextLine)
                {
                    return line;
                }
                if (line.Contains("VIDEO=\"audio_only\""))
                {
                    nextLine = true;
                }
            }
            return "";
        }

        private static List<string> GetM3U8Body(string channelName, AccessToken token)
        {
            var playlist = getRequest($"http://usher.twitch.tv/api/channel/hls/{channelName}.m3u8?player=twitchweb&&token={token.Token}&sig={token.Sig}&allow_audio_only=true&allow_source=true&type=any&p={123212}&player_backend=html5");
            List<string> lines = new List<string>();
            foreach (var line in playlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                lines.Add(line);
            return lines;
        }

        private static AccessToken GetAccessToken(string channelName)
        {
            var json = JObject.Parse(getRequest($"https://api.twitch.tv/api/channels/{channelName}/access_token"));
            return new AccessToken(json["token"].ToString(), json["sig"].ToString());
        }

        private static string getRequest(string url)
        {
            Console.WriteLine($"Requesting: {url}");
            var webRequest = System.Net.WebRequest.Create(url);
            if (webRequest != null)
            {
                webRequest.Method = "GET";
                webRequest.Timeout = 12000;
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Client-Id", Config.TwitchClientId);

                using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                    {
                        var jsonResponse = sr.ReadToEnd();
                        return jsonResponse;
                    }
                }
            }
            return "";
        }

        public class AccessToken
        {
            public string Token { get; protected set; }
            public string Sig { get; protected set; }

            public AccessToken(string token, string sig)
            {
                Token = token;
                Sig = sig;
            }
        }
    }
}
