using BrewLib.Data;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrewLib.Audio
{
    public class AudioManager : IDisposable
    {
        private readonly List<AudioChannel> audioChannels = new List<AudioChannel>();

        private float volume = 1;
        public float Volume
        {
            get { return volume; }
            set
            {
                if (volume == value) return;

                volume = value;
                foreach (var audio in audioChannels)
                    audio.UpdateVolume();
            }
        }

        static AudioManager() => NativeBass.SetBassDllPath();

        public AudioManager(IntPtr windowHandle)
        {
            Trace.WriteLine($"Initializing audio - Bass {Bass.Version}");
            for (var i = 0; i < Bass.DeviceCount; i++)
            {
                var device = Bass.GetDeviceInfo(i);
                Trace.WriteLine($"Audio device - {device.Name}, {device.Driver}, {device.Type}, default:{device.IsDefault}, enabled:{device.IsEnabled}, init:{device.IsInitialized}, loopback:{device.IsLoopback}");
            }

            if (!Bass.Init(-1, 44100, DeviceInitFlags.Default, windowHandle))
            {
                Trace.WriteLine($"Failed to initialize audio with default device: {Bass.LastError}");

                var initialized = false;
                for (var i = 0; i < Bass.DeviceCount; i++)
                {
                    var device = Bass.GetDeviceInfo(i);
                    if (device.Driver == null || device.IsDefault)
                        continue;

                    if (Bass.Init(i, 44100, DeviceInitFlags.Default, windowHandle))
                    {
                        initialized = true;
                        break;
                    }

                    Trace.WriteLine($"Failed to initialize audio with device {i}: {Bass.LastError}");
                }

                if (!initialized)
                    throw new Exception($"Failed to initialize audio - {Bass.LastError}");
            }

            Bass.PlaybackBufferLength = 100;
            Bass.NetBufferLength = 500;
            Bass.UpdatePeriod = 10;
        }

        public void Update()
        {
            for (var i = 0; i < audioChannels.Count; i++)
            {
                var channel = audioChannels[i];
                if (channel.Temporary && channel.Completed)
                {
                    channel.Dispose();
                    i--;
                }
            }
        }

        public AudioStream LoadStream(string path, ResourceContainer resourceContainer = null)
        {
            var audio = new AudioStream(this, path, resourceContainer);
            RegisterChannel(audio);
            return audio;
        }

        public AudioStreamPush CreateStream(int frequency, int channels)
        {
            var audio = new AudioStreamPush(this, frequency, channels);
            RegisterChannel(audio);
            return audio;
        }

        public AudioStreamPull CreateStream(int frequency, int channels, AudioStreamPull.CallbackDelegate callback)
        {
            var audio = new AudioStreamPull(this, frequency, channels, callback);
            RegisterChannel(audio);
            return audio;
        }

        public AudioSample LoadSample(string path, ResourceContainer resourceContainer = null)
            => new AudioSample(this, path, resourceContainer);

        internal void RegisterChannel(AudioChannel channel)
           => audioChannels.Add(channel);

        internal void UnregisterChannel(AudioChannel channel)
            => audioChannels.Remove(channel);

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                Bass.Free();
                disposedValue = true;
            }
        }

        ~AudioManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
