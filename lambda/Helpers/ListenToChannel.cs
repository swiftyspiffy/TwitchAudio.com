using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public static class ListenToChannel
    {
        public static string Listen(string m3u8Link)
        {
            StartListenProcess(m3u8Link);
            System.Threading.Thread.Sleep(6000);
            var processId = GetProcessId();
            KillProcess(processId);
            return "/tmp/audio.mp3";
        }

        private static void StartListenProcess(string m3u8Link)
        {
            var cmd = $"./ffmpeg -i \"{m3u8Link}\" -acodec mp3 -ab 257k audio.mp3 &";
            Console.WriteLine($"cmd: {cmd}");
            Util.ExecuteCommand(cmd, "/tmp", false);
        }

        private static string GetProcessId()
        {
            var res = Util.ExecuteCommand("ps");
            Console.WriteLine(res);
            foreach (var line in res.Result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("ffmpeg"))
                {
                    var parts = line.Split(" ");
                    foreach (var part in parts)
                    {
                        if (part.Length > 0 && part.All(char.IsDigit))
                        {
                            Console.WriteLine($"SUCCESS: '{part}'");
                            return part;
                        }
                        else
                        {
                            Console.WriteLine($"NOT: {part}");
                        }
                    }
                }
            }
            return "";
        }

        private static void KillProcess(string processId)
        {
            Console.WriteLine($"Killing: {processId}");
            var result = Util.ExecuteCommand($"kill -9 {processId}");
            Console.WriteLine(result);
        }
    }
}
