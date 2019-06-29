using System;
using System.Collections.Generic;
using System.Text;
using TwitchAudioRecognition.Models;

namespace TwitchAudioRecognition.Helpers
{
    public class Recognition
    {
        public static Models.IdentificationResponse Run(string mp3Location)
        {
            var bytes = GetFingerprint(mp3Location);
            var result = Recognize(bytes);
            return result;
        }

        private static byte[] GetFingerprint(string mp3Location)
        {
            var resp = Util.ExecuteCommand(@"./acrcloud_extr_linux -cli -i audio.mp3 --debug", "/tmp");
            Console.WriteLine(resp);
            return System.IO.File.ReadAllBytes("/tmp/audio.mp3.cli.lo");
        }

        private static IdentificationResponse Recognize(byte[] bytes)
        {
            Dictionary<string, object> config = new Dictionary<string, object>();
            config.Add("host", Config.ACRHost);
            config.Add("access_key", Config.ACRCloudAccessKey);
            config.Add("access_secret", Config.ACRCloudSecretKey);
            config.Add("timeout", 10);


            Console.WriteLine("-------------------------");
            ACRCloudRecognizer re = new ACRCloudRecognizer(config);

            string result = re.RecognizeByFile(bytes);
            Console.WriteLine(result);
            Console.WriteLine("-------------------------");
            return new IdentificationResponse(result);
        }
    }
}
