﻿using Discord;
using Discord.Audio;
using ShikashiBot.Services.YouTube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace ShikashiBot.Services
{
    public class SongService
    {
        private IVoiceChannel _voiceChannel;
        private IMessageChannel _messageChannel;
        private BufferBlock<DownloadedVideo> _songQueue;

        public AudioPlaybackService AudioPlaybackService { get; set; }
        public DownloadedVideo NowPlaying { get; private set; }

        public SongService()
        {
            this._songQueue = new BufferBlock<DownloadedVideo>();
        }

        public void SetVoiceChannel(IVoiceChannel voiceChannel)
        {
            this._voiceChannel = voiceChannel;
            ProcessQueue();
        }

        public void SetMessageChannel(IMessageChannel messageChannel)
        {
            this._messageChannel = messageChannel;
        }

        private async void ProcessQueue()
        {
            IAudioClient audioClient = await _voiceChannel.ConnectAsync();

            while (await _songQueue.OutputAvailableAsync())
            {
                NowPlaying = await _songQueue.ReceiveAsync();
                await _messageChannel?.SendMessageAsync($"Now playing {NowPlaying.Title} | `{TimeSpan.FromSeconds(NowPlaying.Duration)}` | requested by {NowPlaying.Requester} | {NowPlaying.Url}");
                await AudioPlaybackService.SendAsync(audioClient, NowPlaying.FileName);
                File.Delete(NowPlaying.FileName);
            }
        }

        public void Next()
        {
            AudioPlaybackService.StopCurrentOperation();
        }

        public IList<DownloadedVideo> Clear()
        {
            IList<DownloadedVideo> skippedSongs;
            _songQueue.TryReceiveAll(out skippedSongs);
            Next();

            return skippedSongs;
        }

        public void Queue(DownloadedVideo video)
        {
            _songQueue.Post(video);
        }
    }
}
