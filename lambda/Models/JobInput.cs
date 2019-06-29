using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchAudioRecognition.Models
{
    public class JobInput
    {
        public string JobId { get; set; }
        public string Type { get; set; }
        public string Identifier { get; set; }
        public string Extra { get; set; }
    }
}
