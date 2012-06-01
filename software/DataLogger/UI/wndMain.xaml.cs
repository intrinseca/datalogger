using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.DeviceNotify;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.Media;
using System.IO;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class wndMain : Window
    {
        /// <summary>
        /// Flag indicating whether the device is being polled
        /// </summary>
        public bool IsPolling { get; set; }


        /// <summary>
        /// Wrapper class for business logic
        /// </summary>
        private TelephoneLogger tLogger;

        /// <summary>
        /// Timer to trigger polling of the device
        /// </summary>
        //TODO: Move to TelephoneLogger
        DispatcherTimer poll = new DispatcherTimer();

        /// <summary>
        /// Constuctor
        /// </summary>
        public wndMain()
        {
            InitializeComponent();

            //Initialise business logic
            tLogger = new TelephoneLogger();
            this.DataContext = tLogger;

            //Initialise poll timer
            poll.Interval = new TimeSpan(0, 0, 0, 0, 10);
            poll.Tick += new EventHandler(poll_Tick);
            IsPolling = false;

            //Set up tone display blocksize
            tones.BlockSize = tLogger.Audio.BlockSize;
        }

        //TODO: Link with TelephoneLogger
        void monitor_Disconnected(object sender, EventArgs e)
        {
            stopSampling();

            sbiConnectionStatus.Content = "Not Connected";
        }

        //TODO: Link with TelephoneLogger
        void monitor_Connected(object sender, EventArgs e)
        {
            sbiConnectionStatus.Content = "Connected";
        }

        /// <summary>
        /// Start sampling
        /// </summary>
        private void startSampling()
        {
            if (tLogger.Device.IsOpen)
            {
                //If the device is connected, start polling
                IsPolling = true;
                poll.Start();
            }
        }

        /// <summary>
        /// Stop sampling
        /// </summary>
        private void stopSampling()
        {
            poll.Stop();
            IsPolling = false;
        }

        /// <summary>
        /// Request a batch of samples
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void poll_Tick(object sender, EventArgs e)
        {
            tLogger.PollDevice();
        }

        private void btnStopSampling_Click(object sender, RoutedEventArgs e)
        {
            stopSampling();
        }

        private void btnStartSampling_Click(object sender, RoutedEventArgs e)
        {
            startSampling();
        }

        /// <summary>
        /// Load a file into the interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadWav_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Wave Audio (.wvav)|*.wav";

            if (dlg.ShowDialog() == true)
            {
                tLogger.LoadFile(dlg.FileName);
            }
        }

        /// <summary>
        /// Start audio playback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Audio.Play();
        }

        /// <summary>
        /// Stop audio playback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Audio.Stop();
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Clear();
        }

        /// <summary>
        /// Sync the scrolling of the tones and the spectrogram
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tones_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            grhSpectrum.ScrollTo(e.HorizontalOffset);
        }

        /// <summary>
        /// Sync the scrolling of the tones and the spectrogram
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grhSpectrum_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            tones.ScrollTo(e.HorizontalOffset);
        }
    }
}
