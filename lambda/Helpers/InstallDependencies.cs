using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public static class InstallDependencies
    {
        private static string baseLocation = @"/tmp/";

        public static string Run()
        {
            if (!File.Exists($"{baseLocation}ffmpeg"))
            {
                extractResource("ffmpeg");
                var s2 = Util.ExecuteCommand(@"ls -l /tmp/");
                var s3 = Util.ExecuteCommand(@"chmod +x /tmp/ffmpeg");
                var s4 = Util.ExecuteCommand(@"/tmp/ffmpeg");
            }
            if (!File.Exists($"{baseLocation}acrcloud_extr_linux"))
            {
                extractResource("acrcloud", "/tmp/acrcloud_extr_linux");
                Util.ExecuteCommand(@"chmod +x /tmp/acrcloud_extr_linux");
            }

            Console.WriteLine("Dependencies:");
            var res = Util.ExecuteCommand("ls -l /tmp");
            Console.WriteLine(res);

            return $"{baseLocation}ffmpeg";
        }



        private static void extractResource(string filename, string toPath = null)
        {
            var path = $"TwitchAudioRecognition.{filename}";
            if (toPath == null)
                toPath = $@"{baseLocation}{filename}";
            Assembly assembly = Assembly.GetCallingAssembly();
            Console.WriteLine($"Extracting: {path} to {toPath}");
            using (Stream s = assembly.GetManifestResourceStream(path))
            using (BinaryReader r = new BinaryReader(s))
            using (FileStream fs = new FileStream(toPath, FileMode.OpenOrCreate))
            using (BinaryWriter w = new BinaryWriter(fs))
                w.Write(r.ReadBytes((int)s.Length));

            File.SetAttributes(toPath, FileAttributes.Normal);
        }
    }
}
