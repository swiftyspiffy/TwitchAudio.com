using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;


namespace TwitchAudioRecognition.Models
{
    public class IdentificationResponse
    {
        public bool Successful { get; set; }
        public List<string> Artists { get; set; } = new List<string>();
        public string Label { get; set; }
        public string Album { get; set; }
        public string Title { get; set; }
        public string YouTubeId { get; set; }
        public SpotifyResp Spotify { get; set; } = new SpotifyResp();
        public DeezerResp Deezer { get; set; } = new DeezerResp();

        public IdentificationResponse(bool successful, List<string> artists, string label, string title, string album, string youtubeId)
        {
            Successful = successful;
            Artists = artists;
            Label = label;
            Title = title;
            YouTubeId = youtubeId;
            Album = album;
        }

        public IdentificationResponse(string jsonStr)
        {
            JObject json = JObject.Parse(jsonStr);
            var status = json.SelectToken("status");
            if (status.SelectToken("msg").ToString() != "Success" || status.SelectToken("code").ToString() != "0")
            {
                Successful = false;
            }
            else
            {
                Successful = true;
                var song = json.SelectToken("metadata").SelectToken("music")[0];
                foreach (var artist in song.SelectToken("artists"))
                {
                    Artists.Add(artist.SelectToken("name").ToString());
                }
                if (song.SelectToken("external_metadata").SelectToken("youtube") != null && song.SelectToken("external_metadata").SelectToken("youtube").SelectToken("vid") != null)
                {
                    YouTubeId = song.SelectToken("external_metadata").SelectToken("youtube").SelectToken("vid").ToString();
                }
                if (song.SelectToken("external_metadata").SelectToken("spotify") != null)
                {
                    Spotify = new SpotifyResp(song.SelectToken("external_metadata").SelectToken("spotify"));
                }
                if(song.SelectToken("external_metadata").SelectToken("deezer") != null)
                {
                    Deezer = new DeezerResp(song.SelectToken("external_metadata").SelectToken("deezer"));
                }
                Label = song.SelectToken("label")?.ToString();
                Title = song.SelectToken("title")?.ToString();
                Album = song.SelectToken("album")?.SelectToken("name")?.ToString();
            }
        }

        public override string ToString()
        {
            return $"successful: {Successful}, artists: {string.Join(",", Artists)}, label: {Label}, title: {Title}, youtube id: {YouTubeId}, album: {Album}";
        }

        public class SpotifyResp
        {
            public string TrackId { get; protected set; }
            public List<string> ArtistsIds { get; protected set; } = new List<string>();
            public string AlbumId { get; protected set; }

            public SpotifyResp() { }

            public SpotifyResp(JToken json)
            {
                TrackId = json.SelectToken("album")?.SelectToken("id").ToString();
                AlbumId = json.SelectToken("title")?.SelectToken("id").ToString();
                foreach (var token in json.SelectToken("artists"))
                {
                    ArtistsIds.Add(token.SelectToken("id").ToString());
                }
            }
        }

        public class DeezerResp
        {
            public string TrackId { get; protected set; }
            public List<string> ArtistsIds { get; protected set; } = new List<string>();
            public string AlbumId { get; protected set; }

            public DeezerResp() { }

            public DeezerResp(JToken json)
            {
                TrackId = json.SelectToken("album")?.SelectToken("id").ToString();
                AlbumId = json.SelectToken("title")?.SelectToken("id").ToString();
                foreach (var token in json.SelectToken("artists"))
                {
                    ArtistsIds.Add(token.SelectToken("id").ToString());
                }
            }
        }
    }
}
