using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace youtube_play
{
    internal class Program
    {
        private static void Main(string[] args) {
            var version = new Semver.SemVersion(Assembly.GetExecutingAssembly().GetName().Version);
            version = version.Change(prerelease: "beta");
            Console.WriteLine("youtube-play " + version.ToString());
            try {
                MainAsync().Wait();
            }
            catch (Exception e) {
                while (e is AggregateException) e = e.InnerException;
                throw e;
            }
        }

        private static IEnumerable<string> ArgumentEnumerator(IEnumerable<string> arguments) {
            // TODO: allow random iteration with -r flag
            using (var enumerator = arguments.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    if (enumerator.Current == "-f") {
                        if (!enumerator.MoveNext()) {
                            Console.WriteLine("No file specified after -f.");
                            continue;
                        }
                        if (!File.Exists(enumerator.Current)) {
                            Console.WriteLine("Specified file doesn't exist: " + enumerator.Current);
                        }
                        using (var sr = new StreamReader(enumerator.Current)) {
                            // TODO: return only if it's a valid YT link
                            while (!sr.EndOfStream) {
                                yield return sr.ReadLine();
                            }
                        }
                    }
                    yield return enumerator.Current;
                }
            }
        }

        private static async Task MainAsync() {
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();
            if (!args.Any()) {
                Console.WriteLine("\nUsage:");
                Console.WriteLine("youtube-play <video/playlist/channel link> (one or more in a row)");
                Console.WriteLine(
                    "Instead of a link, you can also pass at any time a '-f <filename>' option with a link on each line of the file");
                return;
            }
            var arguments = ArgumentEnumerator(args);
            var player = new Player();
            var links = arguments.SelectMany(x => LinkResolver.ResolveLink(x)).GetEnumerator();
            bool first = true;
            Task playingTask = null;
            Console.WriteLine("Entering loop");
            while (links.MoveNext()) {
#if DEBUG
                Console.WriteLine("[Debug] Current link: " + links.Current.Id);
#endif
                var count = await player.AddToQueue(links.Current);
                if (count <= 0) continue;
                if (first) {
                    first = false;
                    Console.WriteLine("Starting to play");
                    playingTask = player.StartPlaying();
                }
                while (player.QueuedLinksCount > 10) {
                    // leave 10 links in queue at max, we don't want to resolve links infinitely
                    await Task.Delay(3000);
                }
            }
            links.Dispose();
            Console.WriteLine("End of the list, waiting for the player to stop playing");
            await playingTask;
        }
    }
}