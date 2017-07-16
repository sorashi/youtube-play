youtube-play
===

youtube-play is a command-line utility written in C#, which uses [youtube-dl](http://rg3.github.io/youtube-dl/) and [ffmpeg](https://ffmpeg.org/) to **stream** music from YouTube.

I needed a way to play music from YouTube without having Chrome open with a video playing in the background. That's why youtube-play is very lightweight. A quick analysis with [Process Explorer](https://technet.microsoft.com/sysinternals/processexplorer) shows 0.00…-0.01… % CPU usage and ~20 MB of RAM usage (including all sub-processes).

# Installation
youtube-play needs `youtube-dl` and `ffplay` to be in your PATH.
The fastest and easiest way to do this is using [Chocolatey](https://chocolatey.org/install).
To install Chocolatey, open `cmd` as Administrator, and run the following command:

```batch
@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
```

Then execute the following command, to install `youtube-dl` and the `ffmpeg` package. By installing you accept licenses for the packages.

```batch
choco install youtube-dl ffmpeg -y
```

After that, you are ready to download youtube-play. Recent release with binaries can be found [here](https://github.com/Sorashi/youtube-play/releases/latest).

# Usage
Run `youtube-play` with any number of arguments, where an argument can be either:
- link to a YouTube video, playlist or channel
- `-f` flag followed by a filename, with a link to a YouTube video, playlist or channel on each line

### Examples
- `youtube-play https://www.youtube.com/watch?v=ePX5qgDe9s4`
	- plays the video
- `youtube-play https://www.youtube.com/playlist?list=PLZHQObOWTQDMV832OM2s0zRjsgphR5UJR`
	- plays the whole playlist
- `youtube-play https://www.youtube.com/playlist?list=PLZHQObOWTQDMV832OM2s0zRjsgphR5UJR https://www.youtube.com/watch?v=lX44CAz-JhU https://www.youtube.com/watch?v=qeMFqkcPYcg` 
	- plays the whole playlist and another two videos
- `youtube-play ScLcV4feihM _QiSu4yO0Kk`
	- plays two videos – note that it's enough to tell the video id
- `youtube-play _QiSu4yO0Kk -f list1.txt ScLcV4feihM -f list2.txt`
	- plays one video, then reads the file `list1.txt` and plays every link on each line of the file, then plays another video and then plays every link in the file `list2.txt`

# Alternatives
You can use [foobar2000](http://www.foobar2000.org/) with the [foo_youtube](https://fy.3dyd.com/) component to achieve the same. However, it's not a command-line utility, but a whole music player.


# Planned features
- [ ] Global keyboard hook which will allow you to pause/stop the playback or skip the current song
- [ ] Random playback – `-r` flag which will turn random shuffle of the videos following after the flag, repeating the flag will turn the shuffle of for the following links and vice versa
- [ ] Supporting links of playlist with specific videos specified (currently the whole playlist is played from start to end)

# What is happening behind the scenes

It's pretty easy. I'll show you the process of doing the same thing without youtube-play (youtube-play uses a bit different commands that are more understandable for machines).

Prepare a `<link>` to a YouTube video. Open up your command-line. Execute `youtube-dl -F <link>`. youtube-dl will respond with a table with available formats for the video. Example:

format code  |extension  |resolution |note
-------------|-----------|-----------|----
139          |m4a        |audio only |DASH audio   48k , m4a_dash container, mp4a.40.5@ 48k (22050Hz)
249          |webm       |audio only |DASH audio   68k , opus @ 50k, 1.40MiB
250          |webm       |audio only |DASH audio   86k , opus @ 70k, 1.85MiB
140          |m4a        |audio only |DASH audio  128k , m4a_dash container, mp4a.40.2@128k (44100Hz)
251          |webm       |audio only |DASH audio  156k , opus @160k, 3.63MiB
171          |webm       |audio only |DASH audio  202k , vorbis@128k, 4.42MiB
278          |webm       |256x144    |144p  105k , webm container, vp9, 24fps, video only, 2.59MiB
160          |mp4        |256x144    |DASH video  110k , avc1.4d400c, 24fps, video only
242          |webm       |426x240    |240p  246k , vp9, 24fps, video only, 5.60MiB
133          |mp4        |426x240    |DASH video  285k , avc1.4d4015, 24fps, video only
243          |webm       |640x360    |360p  464k , vp9, 24fps, video only, 10.91MiB
134          |mp4        |640x360    |DASH video  634k , avc1.4d401e, 24fps, video only
244          |webm       |854x480    |480p  856k , vp9, 24fps, video only, 20.38MiB
135          |mp4        |854x480    |DASH video 1254k , avc1.4d401e, 24fps, video only
247          |webm       |1280x720   |720p 1706k , vp9, 24fps, video only, 41.67MiB
136          |mp4        |1280x720   |DASH video 2534k , avc1.4d401f, 24fps, video only
248          |webm       |1920x1080  |1080p 2938k , vp9, 24fps, video only, 74.36MiB
137          |mp4        |1920x1080  |DASH video 4363k , avc1.640028, 24fps, video only
17           |3gp        |176x144    |small , mp4v.20.3, mp4a.40.2@ 24k
36           |3gp        |320x180    |small , mp4v.20.3, mp4a.40.2
43           |webm       |640x360    |medium , vp8.0, vorbis@128k
18           |mp4        |640x360    |medium , avc1.42001E, mp4a.40.2@ 96k
22           |mp4        |1280x720   |hd720 , avc1.64001F, mp4a.40.2@192k (best)

Select the format that suits you the most and remember its `format code`. youtube-play prefers `audio only` formats with the largest bitrate possible (which you can find in the `note` column), but you can choose any format with sound, since the video will be omitted.

Next, execute `youtube-dl -f <format code> -g <link>`. This will respond with a direct link to the file. You can either copy it in the `cmd`, or pipe it into your clipboard like this:
```
youtube-dl -f <format code> -g <link> | clip
```

To stream the sound from the file, execute this command:
```
ffplay -volume <number from 0 to 100> -nodisp -vn -autoexit "<paste the direct link here>"
```

ffplay will stream the sound and exit when it's complete.