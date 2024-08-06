﻿using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using LibVLCSharp.Shared;
using LiveChartsCore.Geo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Playback;
using YoutubeExplode;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for MediaSlider.xaml
    /// </summary>
    public partial class MediaSlider : UserControl
    {
        private static LibVLC LibVlc { get; set; }
        private static LibVLCSharp.Shared.MediaPlayer MediaPlayer { get; set; }

        private MediaSliderViewModel ViewModel { get; set; }
        private bool timeSliderDragged = false;
        private bool loaded = false;
        private List<string> MediaUrls = new List<string>();
        public MediaSlider()
        {
            InitializeComponent();
            ViewModel = new MediaSliderViewModel();
            this.DataContext = ViewModel;
        }
        public void Unload()
        {
            if (ViewModel.MediaPlayer.Media != null)
            {
                ViewModel.MediaPlayer.Stop();
                ViewModel.MediaPlayer.Media.Dispose();
                ViewModel.MediaPlayer.Media = null;
            }
        }
        public void Init(GameMetadata data)
        {
            if (loaded)
                return;

            loaded = true;
            PrepareMetadata(data);
            if (MediaSlider.LibVlc == null)
            {
                MediaSlider.LibVlc = new LibVLC();
            }
            if (MediaSlider.MediaPlayer == null)
            {
                MediaSlider.MediaPlayer = new LibVLCSharp.Shared.MediaPlayer(MediaSlider.LibVlc);
            }
            ViewModel.MediaPlayer = MediaSlider.MediaPlayer;
            ViewModel.MediaPlayer.TimeChanged += (s, e) =>
            {
                try
                {
                    ViewModel.CurrentTime = TimeSpan.FromMilliseconds(ViewModel.MediaPlayer.Time);
                    if (!timeSliderDragged)
                    {
                        ViewModel.CurrentTimeInMilliseconds = ViewModel.MediaPlayer.Time;
                    }
                }
                catch { }
            };
            ViewModel.MediaPlayer.LengthChanged += (s, e) =>
            {
                try
                {
                    ViewModel.CurrentLenght = TimeSpan.FromMilliseconds(ViewModel.MediaPlayer.Length);
                    ViewModel.CurrentLenghtInMilliseconds = ViewModel.MediaPlayer.Length;
                    AdaptUIToMediaType();
                }
                catch { }
            };
            ViewModel.MediaPlayer.Volume = 15;           
        }
        private void PrepareMetadata(GameMetadata data)
        {
            for (int i = 0; i < data?.Trailers?.Count(); i++)
            {
                MediaUrls.Add(data?.Trailers[i]);
            }
            for (int i = 0; i < data?.Gameplays?.Count(); i++)
            {
                MediaUrls.Add(data?.Gameplays[i]);
            }
            for (int i = 0; i < data?.Screenshots?.Count(); i++)
            {
                MediaUrls.Add(data?.Screenshots[i]);
            }
        }
        private void AdaptUIToMediaType()
        {
            if (ViewModel.MediaPlayer.AudioTrackCount <= 0)
            {
                ViewModel.VideoControlBarVisibility = Visibility.Hidden;
            }
            else
            {
                ViewModel.VideoControlBarVisibility = Visibility.Visible;
            }
        }
        int mediaIndex = -1;
        private async Task PrepareAndPlayMedia(string uri)
        {
            if (uri.Contains("youtube", StringComparison.OrdinalIgnoreCase))
            {
                var youtube = new YoutubeClient();
                var videoUrl = uri;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
                var streamInfo = streamManifest.GetMuxedStreams().First();
                var streamUrl = streamInfo.Url;
                ViewModel.MediaPlayer.Play(new LibVLCSharp.Shared.Media(MediaSlider.LibVlc, streamUrl, FromType.FromLocation));
                ViewModel.IsPlaying = true;
            }
            else
            {
                MemoryStream ms = await BitmapHelper.UrlToMemoryStream(uri);
                ViewModel.MediaPlayer.Play(new LibVLCSharp.Shared.Media(MediaSlider.LibVlc, new StreamMediaInput(ms)));
                ViewModel.IsPlaying = true;
            }
        }
        private async void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (mediaIndex < MediaUrls.Count - 1)
            {
                mediaIndex++;
                await PrepareAndPlayMedia(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = 0;
            }
            //TO DO
            //ViewModel.MediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, $"{mediaIndex + 1}/{MediaUrls.Count}"); //to set subtitle or any other text
        }

        private async void PrevMedia_Click(object sender, RoutedEventArgs e)
        {
            if (mediaIndex > 0)
            {
                mediaIndex--;
                await PrepareAndPlayMedia(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = MediaUrls.Count - 1;
            }
            //TO DO
            //ViewModel.MediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, $"{mediaIndex + 1}/{MediaUrls.Count}"); //to set subtitle or any other text
        }

        private void PlayPause_Click(object sender, EventArgs e)
        {
            if (ViewModel.MediaPlayer.IsPlaying)
            {
                ViewModel.MediaPlayer.Pause();
                ViewModel.IsPlaying = false;
            }
            else if (!ViewModel.MediaPlayer.IsPlaying)
            {
                ViewModel.MediaPlayer.Play();
                ViewModel.IsPlaying = true;
            }           
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MediaPlayer.Mute = !ViewModel.MediaPlayer.Mute;
        }
        private void TimeSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            timeSliderDragged = true;
        }

        private void TimeSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ViewModel.MediaPlayer.Time = (long)((Slider)sender).Value;
            timeSliderDragged = false;
        }
    }
}