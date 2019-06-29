using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public class ACRCloudStatusCode
    {
        public static string HTTP_ERROR = "{\"status\":{\"msg\":\"Http Error\", \"code\":3000}}";
        public static string NO_RESULT = "{\"status\":{\"msg\":\"No Result\", \"code\":1001}}";
        public static string GEN_FP_ERROR = "{\"status\":{\"msg\":\"Gen Fingerprint Error\", \"code\":2004}}";
        public static string DECODE_AUDIO_ERROR = "{\"status\":{\"msg\":\"Can not decode audio data\", \"code\":2005}}";
        public static string RECORD_ERROR = "{\"status\":{\"msg\":\"Record Error\", \"code\":2000}}";
        public static string JSON_ERROR = "{\"status\":{\"msg\":\"json error\", \"code\":2002}}";
    }

    class ACRCloudRecognizer
    {
        public enum RECOGNIZER_TYPE
        {
            acr_rec_type_audio, acr_rec_type_humming, acr_rec_type_both
        };
        private string host = "";
        private string accessKey = "";
        private string accessSecret = "";
        private int timeout = 5 * 1000; // ms
        private RECOGNIZER_TYPE rec_type = RECOGNIZER_TYPE.acr_rec_type_audio;

        public ACRCloudRecognizer(IDictionary<string, Object> config)
        {
            if (config.ContainsKey("host"))
            {
                this.host = (string)config["host"];
            }
            if (config.ContainsKey("access_key"))
            {
                this.accessKey = (string)config["access_key"];
            }
            if (config.ContainsKey("access_secret"))
            {
                this.accessSecret = (string)config["access_secret"];
            }
            if (config.ContainsKey("timeout"))
            {
                this.timeout = 1000 * (int)config["timeout"];
            }
            if (config.ContainsKey("rec_type"))
            {
                this.rec_type = (RECOGNIZER_TYPE)config["rec_type"];
            }
        }

        /**
          *
          *  recognize by file path of (Audio/Video file)
          *          Audio: mp3, wav, m4a, flac, aac, amr, ape, ogg ...
          *          Video: mp4, mkv, wmv, flv, ts, avi ...
          *
          *  @param filePath query file path
          *  @param startSeconds skip (startSeconds) seconds from from the beginning of (filePath)
          *  
          *  @return result 
          *
          **/
        public String RecognizeByFile(byte[] ext_fp)
        {
            Trace.TraceInformation("RecognizeByFile");
            IDictionary<string, Object> query_data = new Dictionary<string, Object>();
            query_data.Add("ext_fp", ext_fp);

            if (ext_fp == null)
            {
                Trace.TraceInformation("null null");
                return ACRCloudStatusCode.NO_RESULT;
            }
            return this.DoRecognize(query_data);
        }

        private string PostHttp(string url, IDictionary<string, Object> postParams)
        {
            Trace.TraceInformation("PostHttp");
            string result = "";

            string BOUNDARYSTR = "acrcloud***copyright***2015***" + DateTime.Now.Ticks.ToString("x");
            string BOUNDARY = "--" + BOUNDARYSTR + "\r\n";
            var ENDBOUNDARY = Encoding.ASCII.GetBytes("--" + BOUNDARYSTR + "--\r\n\r\n");

            var stringKeyHeader = BOUNDARY +
                           "Content-Disposition: form-data; name=\"{0}\"" +
                           "\r\n\r\n{1}\r\n";
            var filePartHeader = BOUNDARY +
                            "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
                            "Content-Type: application/octet-stream\r\n\r\n";

            var memStream = new MemoryStream();
            foreach (var item in postParams)
            {
                if (item.Value is string)
                {
                    string tmpStr = string.Format(stringKeyHeader, item.Key, item.Value);
                    byte[] tmpBytes = Encoding.UTF8.GetBytes(tmpStr);
                    memStream.Write(tmpBytes, 0, tmpBytes.Length);
                }
                else if (item.Value is byte[])
                {
                    var header = string.Format(filePartHeader, item.Key, item.Key);
                    var headerbytes = Encoding.UTF8.GetBytes(header);
                    memStream.Write(headerbytes, 0, headerbytes.Length);
                    byte[] sample = (byte[])item.Value;
                    memStream.Write(sample, 0, sample.Length);
                    memStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, 2);
                }
            }
            memStream.Write(ENDBOUNDARY, 0, ENDBOUNDARY.Length);

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream writer = null;
            StreamReader myReader = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = this.timeout;
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + BOUNDARYSTR;

                memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);

                writer = request.GetRequestStream();
                writer.Write(tempBuffer, 0, tempBuffer.Length);
                writer.Flush();
                writer.Close();
                writer = null;

                response = (HttpWebResponse)request.GetResponse();
                myReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                result = myReader.ReadToEnd();
            }
            catch (WebException e)
            {
                Trace.TraceInformation("webex: " + e.Message);
                Console.WriteLine("timeout:\n" + e.ToString());
                result = ACRCloudStatusCode.HTTP_ERROR;
            }
            catch (Exception e)
            {
                Trace.TraceInformation("ex2: " + e.Message);
                Console.WriteLine("other excption:" + e.ToString());
                result = ACRCloudStatusCode.HTTP_ERROR;
            }
            finally
            {
                if (memStream != null)
                {
                    memStream.Close();
                    memStream = null;
                }
                if (writer != null)
                {
                    writer.Close();
                    writer = null;
                }
                if (myReader != null)
                {
                    myReader.Close();
                    myReader = null;
                }
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
            Trace.TraceInformation("post finished");
            return result;
        }

        private string EncryptByHMACSHA1(string input, string key)
        {
            HMACSHA1 hmac = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(key));
            byte[] stringBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashedValue = hmac.ComputeHash(stringBytes);
            return EncodeToBase64(hashedValue);
        }

        private string EncodeToBase64(byte[] input)
        {
            string res = Convert.ToBase64String(input, 0, input.Length);
            return res;
        }

        private string DoRecognize(IDictionary<string, Object> query_data)
        {
            Trace.TraceInformation("DoRecognize");
            byte[] ext_fp = null;
            byte[] hum_fp = null;
            string method = "POST";
            string httpURL = "/v1/identify";
            string dataType = "fingerprint";
            string sigVersion = "1";
            string timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();

            string reqURL = "http://" + host + httpURL;

            string sigStr = method + "\n" + httpURL + "\n" + accessKey + "\n" + dataType + "\n" + sigVersion + "\n" + timestamp;
            string signature = EncryptByHMACSHA1(sigStr, this.accessSecret);

            var dict = new Dictionary<string, object>();
            dict.Add("access_key", this.accessKey);
            if (query_data.ContainsKey("ext_fp"))
            {
                ext_fp = (byte[])query_data["ext_fp"];
                if (ext_fp != null)
                {
                    dict.Add("sample_bytes", ext_fp.Length.ToString());
                    dict.Add("sample", ext_fp);
                }
            }
            if (query_data.ContainsKey("hum_fp"))
            {
                hum_fp = (byte[])query_data["hum_fp"];
                if (hum_fp != null)
                {
                    dict.Add("sample_hum_bytes", hum_fp.Length.ToString());
                    dict.Add("sample_hum", hum_fp);
                }
            }
            if (ext_fp == null && hum_fp == null)
            {
                return ACRCloudStatusCode.NO_RESULT;
            }
            dict.Add("timestamp", timestamp);
            dict.Add("signature", signature);
            dict.Add("data_type", dataType);
            dict.Add("signature_version", sigVersion);
            Trace.TraceInformation("posting");
            string res = PostHttp(reqURL, dict);

            return res;
        }
    }
}
