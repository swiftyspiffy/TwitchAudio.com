using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public static class Util
    {
        public static ExecutionResult ExecuteCommand(string command, string directory = null, bool listenForOutputs = true)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "/bin/bash";
            var args = "-c \" " + command + " \"";
            proc.StartInfo.Arguments = args;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = listenForOutputs;
            proc.StartInfo.RedirectStandardError = listenForOutputs;
            if (directory != null)
            {
                proc.StartInfo.WorkingDirectory = directory;
            }
            proc.Start();
            if (listenForOutputs)
            {
                string result = proc.StandardOutput.ReadToEnd();
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                return new ExecutionResult(result, err);
            }
            return null;
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public class ExecutionResult
        {
            public string Result { get; protected set; }
            public string Error { get; protected set; }

            public ExecutionResult(string result, string error)
            {
                Result = result;
                Error = error;
            }

            public override string ToString()
            {
                return $"[execution result] result: {Result}, error: {Error}";
            }
        }
    }
}
