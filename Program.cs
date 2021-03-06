using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReplayScraper
{
    class Program
    {
        private static readonly string ReplayFolder = @"C:\Users\msjog\Downloads\Replays";

        static void Main(string[] args)
        {
            if (!Directory.Exists(ReplayFolder))
            {
                Directory.CreateDirectory(ReplayFolder);
            }

            foreach (var comp in args)
            {
                ProcessComp(comp);
            }
        }

        private static void ProcessComp(string comp)
        {
            var compResults = DownloadResults(comp);
            var matchIdCol = compResults["cols"]["idmatch"].Value<int>();
            var startedCol = compResults["cols"]["started"].Value<int>();
            var finishedCol = compResults["cols"]["finished"].Value<int>();

            bool IsNotAdminMatch(JToken r)
            {
                return r[startedCol].Value<string>() != r[finishedCol].Value<string>();
            }

            var rows = (JArray) compResults["rows"];
            foreach (var matchId in rows.Where(IsNotAdminMatch).Select(r => r[matchIdCol].Value<string>()))
            {
                DownloadReplay(matchId);
            }
        }

        private static JToken DownloadResults(string comp)
        {
            // {"compResults":{"id":"compResults","idmap":{"idcompetition":"41886"}}}
            var idmap = new Dictionary<string, string> {{"idcompetition", comp}};
            var compResults = new Dictionary<string, object> {{"id", "compResults"}, {"idmap", idmap}};
            var req = new Dictionary<string, object> {{"compResults", compResults}};
            var request = WebRequest.Create($"https://www.mordrek.com:666/api/v1/queries?req={JsonConvert.SerializeObject(req)}");
            using var response = request.GetResponse();
            using var stream = response.GetResponseStream();
            var foo = Deserialize<JObject>(stream);
            return foo["response"]["compResults"]["result"];
        }

        private static string DownloadReplay(string matchId)
        {
            var replayUrl = GetReplayUri(matchId);
            if (replayUrl == null)
            {
                return null;
            }

            var fname = Path.Combine(ReplayFolder, replayUrl.Substring(replayUrl.LastIndexOf('/') + 1));
            if (File.Exists(fname))
            {
                Console.WriteLine($"{fname} already exists, skipping.");
            }
            else
            {
                Console.WriteLine($"Downloading {replayUrl} to {fname}");
                var request = WebRequest.Create(replayUrl);
                using var response = request.GetResponse();
                using var stream = response.GetResponseStream();
                using var localFile = File.Create(fname);
                stream.CopyTo(localFile);
            }

            return fname;
        }

        private static string GetReplayUri(string matchId)
        {
            var request = WebRequest.Create($"https://www.mordrek.com:666/api/v1/match/{matchId}/url");
            using var response = request.GetResponse();
            using var stream = response.GetResponseStream();
            return Deserialize<string>(stream);
        }

        private static T Deserialize<T>(Stream s)
        {
            var ds = new JsonSerializer();
            return ds.Deserialize<T>(new JsonTextReader(new StreamReader(s)));
        }
    }
}
