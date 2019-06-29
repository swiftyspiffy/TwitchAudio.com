using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public static class DownloadClip
    {
        public static string Run(string mp4Path)
        {
            var wc = new System.Net.WebClient();
            wc.DownloadFile(mp4Path, "/tmp/audio.mp3");

            return "/tmp/audio.mp3";
        }

        private static string decodeMp4Path(string encodedMp4Path)
        {
            Console.WriteLine($"DECODING: '{encodedMp4Path}'");
            byte[] data = Convert.FromBase64String(encodedMp4Path);
            return Encoding.UTF8.GetString(data);
        }
    }
}
