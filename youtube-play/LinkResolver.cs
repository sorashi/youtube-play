using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace youtube_play
{
    public static class LinkResolver
    {
        public static IEnumerable<Video> ResolveLink(string rawLink) {
            var iteratorProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "youtube-dl",
                    Arguments = $"-j --flat-playlist \"{rawLink}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            iteratorProcess.Start();
            while (!iteratorProcess.StandardOutput.EndOfStream) {
                var line = iteratorProcess.StandardOutput.ReadLine();
                var jo = JObject.Parse(line);
                if (jo.Value<string>("_type") == "url") // we are dealing with a playlist
                    yield return new Video(jo["id"].ToString(), jo["title"].ToString());
                else yield return Video.FromJson(line); // a single video
            }
        }

        public class VideoFormat
        {
            public string FormatCode { get; set; }
            public string Extension { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
            public string Note { get; set; }
            public string Url { get; set; }
            public float? Bitrate { get; set; }
        }

        public class Video
        {
            private VideoFormat bestStreamableAudioFormat;

            public Video(string id, string title = null) {
                Id = id;
                Title = title;
            }

            public string Title { get; }
            public string Id { get; }
            private Task<IEnumerable<VideoFormat>> Formats { get; set; }

            public async Task<VideoFormat> GetBestStreamableAudioFormat() {
                if (bestStreamableAudioFormat != null) return bestStreamableAudioFormat;
                var videoFormats = await GetFormats();
                if (videoFormats == null) return null;
                // at first try to find audio only
                var audioOnly = videoFormats.Where(x => x.Note == "DASH audio" &&
                                                   (x.Extension == "webm" || x.Extension == "m4a"));
                if (audioOnly.Any()) {
                    bestStreamableAudioFormat = audioOnly.OrderByDescending(x => x.Bitrate).First();
                    return bestStreamableAudioFormat;
                }
                // else return the best streamable video format
                bestStreamableAudioFormat = videoFormats.Where(x => x.Extension == "webm" || x.Extension == "mp4")
                    .Where(x => x.Width != null && x.Height != null || x.Bitrate != null)
                    .OrderByDescending(x => x.Bitrate ?? x.Width * x.Height)
                    .First();
                return bestStreamableAudioFormat;
            }

            public static Video FromJson(string json) {
                var jo = JObject.Parse(json);
                var vid = new Video(jo["id"].ToString(), jo["title"].ToString()) {
                    Formats = Task.FromResult(ResolveFormats(jo["formats"].ToString()))
                };
                return vid;
            }

            private static IEnumerable<VideoFormat> ResolveFormats(string jsonArray) {
                var ja = JArray.Parse(jsonArray);
                return ja
                    .Select(x =>
                        new VideoFormat {
                            FormatCode = x["format_id"].ToString(),
                            Extension = x["ext"].ToString(),
                            Width = x.Value<int?>("width"),
                            Height = x.Value<int?>("height"),
                            Note = x["format_note"]?.ToString(),
                            Bitrate = x.Value<float?>("abr") ?? x.Value<float?>("tbr"),
                            Url = x["url"].ToString()
                        }
                    );
            }

            public Task<IEnumerable<VideoFormat>> GetFormats() {
                if (Formats != null) return Formats;
                var t = Task.Factory.StartNew(() => {
                    var iteratorProcess = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "youtube-dl",
                            Arguments = "-j " + Id,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    iteratorProcess.Start();
                    var stdout = iteratorProcess.StandardOutput.ReadToEnd();
                    var stderr = iteratorProcess.StandardError.ReadToEnd();
                    iteratorProcess.WaitForExit();
                    var code = iteratorProcess.ExitCode;
                    if (code != 0) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("An error occurred while resolving the video link:\n" + stderr);
                        Console.ResetColor();
                        return null;
                    }
                    var jo = JObject.Parse(stdout);
                    return ResolveFormats(jo["formats"].ToString());
                });
                Formats = t;
                return t;
            }
        }
    }
}