using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchAudioRecognition.Helpers
{
    public class DBv2
    {
        public enum DDB_TABLE
        {
            IDENTIFICATION_STREAMS,
            IDENTIFICATION_CLIPS
        }

        private AmazonDynamoDBClient _client;

        public DBv2(Amazon.Runtime.BasicAWSCredentials creds, Amazon.RegionEndpoint region)
        {
            _client = new AmazonDynamoDBClient(creds, region);
        }

        public async void ListeningStart(DDB_TABLE tbl, string jobId, string twitchResource)
        {
            // any error will be shown in cloudwatch logs, and user will get job doesn't exist error
            var twitchColumn = tbl == DDB_TABLE.IDENTIFICATION_STREAMS ? "Channel" : "ClipId";
            var request = new PutItemRequest(getTable(tbl), new Dictionary<string, AttributeValue>()
            {
                { "Id", new AttributeValue(){ S = jobId } },
                { "Status", new AttributeValue(){ S = "started" } },
                { twitchColumn, new AttributeValue(){ S = twitchResource } },
                { "CreatedAt", new AttributeValue(){ S = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ssZ")} }
            });

            await _client.PutItemAsync(request);
        }

        public async void ListeningFinished(DDB_TABLE tbl, bool successful, string jobId, List<string> artists,
            string label, string title, string album, string youtubeId,
            string spotifyTrackId, string spotifyAlbumId, List<string> spotifyArtistsIds,
            string deezerTrackId, string deezerAlbumId, List<string> deezerArtistsIds)
        {
            List<DynamoField> fields = new List<DynamoField>()
            {
                new DynamoField("Status", successful ? "successful" : "failed"),
                new DynamoField("Artists", artists),
                new DynamoField("Label", label),
                new DynamoField("Title", title),
                new DynamoField("Album", album),
                new DynamoField("YouTubeId", youtubeId),
                new DynamoField("SpotifyTrackId", spotifyTrackId),
                new DynamoField("SpotifyAlbumId", spotifyAlbumId),
                new DynamoField("SpotifyArtistsId", spotifyArtistsIds),
                new DynamoField("DeezerTrackId", deezerTrackId),
                new DynamoField("DeezerAlbumId", deezerAlbumId),
                new DynamoField("DeezerArtistsIds", deezerArtistsIds)
            };

            // Our request can NOT contain fields that are empty.
            var expressionAttributeNames = new Dictionary<string, string>() { };
            var expressionAttributeValues = new Dictionary<string, AttributeValue>();
            var updateExpression = "";
            foreach (var field in fields)
            {
                if((field.Value != null && field.Value.Length > 0) ||
                    (field.Values != null && field.Values.Count > 0))
                {
                    expressionAttributeNames.Add($"#{field.Name}", field.Name);
                    if(field.Value != null)
                    {
                        expressionAttributeValues.Add($":{field.Name.ToLower()}", new AttributeValue() { S = field.Value });
                    } else
                    {
                        expressionAttributeValues.Add($":{field.Name.ToLower()}", new AttributeValue() { SS = field.Values });
                    }
                    if(updateExpression == "") {
                        updateExpression = $"SET #{field.Name} = :{field.Name.ToLower()}";
                    } else
                    {
                        updateExpression += $", #{field.Name} = :{field.Name.ToLower()}";
                    }
                }
            }

            var request = new UpdateItemRequest
            {
                TableName = getTable(tbl),
                Key = new Dictionary<string, AttributeValue>() { { "Id", new AttributeValue { S = jobId } } },
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues,
                UpdateExpression = updateExpression
            };

            await _client.UpdateItemAsync(request);
        }

        private string getTable(DDB_TABLE table)
        {
            switch(table)
            {
                case DDB_TABLE.IDENTIFICATION_STREAMS:
                    return Config.TableStreamIdentifications;
                case DDB_TABLE.IDENTIFICATION_CLIPS:
                    return Config.TableClipIdentifications;
                default:
                    throw new Exception($"unknown table: {table}");
            }
        }

        private class DynamoField
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public List<string> Values { get; set; }

            public DynamoField(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public DynamoField(string name, List<string> values)
            {
                Name = name;
                Values = values;
            }
        }
    }
}
