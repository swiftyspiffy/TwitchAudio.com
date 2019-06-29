using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchAudioRecognition.Models;

using Amazon.Lambda.Core;
using TwitchAudioRecognition.Helpers;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TwitchAudioRecognition
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public void FunctionHandler(string input, ILambdaContext context)
        {
            var req = JsonConvert.DeserializeObject<JobInput>(input);
            Contact.swiftyspiffy(req.JobId, req.Type, req.Identifier);

            Console.WriteLine($"JOB ID: {req.JobId}");
            Console.WriteLine($"JOB TYPE: {req.Type}");
            Console.WriteLine($"JOB IDENTIFIER: {req.Identifier}");

            //ddb client
            var ddbClient = new DBv2(new Amazon.Runtime.BasicAWSCredentials(Config.AWSAccessKey, Config.AWSSecretKey), Amazon.RegionEndpoint.USWest2);

            switch(req.Type) {
                case "stream":
                    req.Identifier = req.Identifier.ToLower();

                    // update database - started
                    Console.WriteLine("Updating database that we're starting...");
                    ddbClient.ListeningStart(DBv2.DDB_TABLE.IDENTIFICATION_STREAMS, req.JobId, req.Identifier);

                    // run job
                    Console.WriteLine("Running job...");
                    var result = streamJob(req);

                    // update database - finished
                    Console.WriteLine("Updating database with results...");
                    ddbClient.ListeningFinished(DBv2.DDB_TABLE.IDENTIFICATION_STREAMS, result.Successful, req.JobId, result.Artists, result.Label, result.Title, result.Album, result.YouTubeId,
                        result.Spotify.TrackId, result.Spotify.AlbumId, result.Spotify.ArtistsIds, result.Deezer.TrackId, result.Deezer.AlbumId, result.Deezer.ArtistsIds);
                    break;
                case "clip":
                    // update database - started
                    Console.WriteLine("Updating database that we're starting...");
                    ddbClient.ListeningStart(DBv2.DDB_TABLE.IDENTIFICATION_CLIPS, req.JobId, req.Identifier);

                    // run job
                    Console.WriteLine("Running job...");
                    result = clipJob(req);

                    // update database - finished
                    Console.WriteLine("Updating database with results...");
                    ddbClient.ListeningFinished(DBv2.DDB_TABLE.IDENTIFICATION_CLIPS, result.Successful, req.JobId, result.Artists, result.Label, result.Title, result.Album, result.YouTubeId,
                        result.Spotify.TrackId, result.Spotify.AlbumId, result.Spotify.ArtistsIds, result.Deezer.TrackId, result.Deezer.AlbumId, result.Deezer.ArtistsIds);
                    break;
                default:
                    Console.WriteLine($"FAILED: INVALID REQUEST TYPE '{req.Type}'");
                    break;
            }
        }

        private IdentificationResponse streamJob(JobInput input)
        {
            // perform 
            Console.WriteLine("Installing ffmpeg...");
            var ffmpegPath = InstallDependencies.Run();
            Console.WriteLine("Getting m3u8 link...");
            var m3u8Link = M3U8.GetAudioOnlyM3U8(input.Identifier);
            Console.WriteLine("Listening to channel...");
            var mp3 = ListenToChannel.Listen(m3u8Link);
            Console.WriteLine("Running recognition...");
            var result = Recognition.Run(mp3);
            Console.WriteLine(result);
            Console.WriteLine("Finished...");

            // cleanup
            Console.WriteLine("Cleaning up...");
            System.IO.File.Delete("/tmp/audio.mp3");
            System.IO.File.Delete("/tmp/audio.mp3.cli.lo");

            return result;
        }

        private IdentificationResponse clipJob(JobInput input)
        {
            // perform 
            Console.WriteLine("Installing ffmpeg...");
            var ffmpegPath = InstallDependencies.Run();
            var mp4 = DownloadClip.Run(input.Identifier);
            var result = Recognition.Run(mp4);
            Console.WriteLine(result);
            Console.WriteLine("Finished...");
            // cleanup
            Console.WriteLine("Cleaning up...");
            System.IO.File.Delete("/tmp/audio.mp4");
            System.IO.File.Delete("/tmp/audio.mp4.cli.lo");

            return result;
        }
    }
}
