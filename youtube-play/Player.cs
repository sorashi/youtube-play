using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace youtube_play
{
    public class Player
    {
        private readonly Queue<LinkResolver.Video> _playQueue = new Queue<LinkResolver.Video>();

        public Task StartPlaying(int volume = 20) {
            return Task.Factory.StartNew(async () => {
                while (_playQueue.Count > 0) {
                    var current = _playQueue.Dequeue();
                    var playerProcess = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "ffplay",
                            Arguments = $"-volume {volume} -nodisp -vn -autoexit \"{(await current.GetBestStreamableAudioFormat()).Url}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Now playing: {current.Title}");
                    Console.ResetColor();
                    playerProcess.Start();
                    ChildProcessTracker.AddProcess(playerProcess);
                    playerProcess.WaitForExit();
                }
            });
        }

        public async Task<int> AddToQueue(LinkResolver.Video video) {
            if (await video.GetBestStreamableAudioFormat() == null) {
                Console.WriteLine("Skipped " + video.Title);
                return _playQueue.Count;
            }
            Console.WriteLine("Link resolved: " + video.Title);
            _playQueue.Enqueue(video);
            return _playQueue.Count;
        }

        public int QueuedLinksCount => _playQueue.Count;
    }
}