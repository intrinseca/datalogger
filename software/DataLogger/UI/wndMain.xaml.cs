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
        public bool IsPolling { get; set; }

        private TelephoneLogger tLogger;

        DispatcherTimer poll = new DispatcherTimer();

        public wndMain()
        {
            InitializeComponent();

            tLogger = new TelephoneLogger();

            poll.Interval = new TimeSpan(0, 0, 0, 0, 10);
            poll.Tick += new EventHandler(poll_Tick);

            IsPolling = false;

            tones.BlockSize = tLogger.Audio.BlockSize;

            this.DataContext = tLogger;
        }

        void monitor_Disconnected(object sender, EventArgs e)
        {
            stopSampling();

            sbiConnectionStatus.Content = "Not Connected";
        }

        void monitor_Connected(object sender, EventArgs e)
        {
            sbiConnectionStatus.Content = "Connected";
        }

        private void stopSampling()
        {
            poll.Stop();
            IsPolling = false;
        }

        void poll_Tick(object sender, EventArgs e)
        {
            tLogger.PollDevice();

            short sample = tLogger.Audio.Samples[tLogger.Audio.Samples.Count - 1];
        }

        private void btnStopSampling_Click(object sender, RoutedEventArgs e)
        {
            stopSampling();
        }

        private void btnStartSampling_Click(object sender, RoutedEventArgs e)
        {
            startSampling();
        }

        private void startSampling()
        {
            if (tLogger.Device.IsOpen)
            {
                IsPolling = true;
                poll.Start();
            }
        }

        private void btnAnalyse_Click(object sender, RoutedEventArgs e)
        {
            tLogger.UpdateAnalysis();
        }

        private void btnLoadWav_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Wave Audio (.wvav)|*.wav";

            if (dlg.ShowDialog() == true)
            {
                tLogger.LoadFile(dlg.FileName);
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Audio.Play();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Clear();
        }

        private void tones_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            grhSpectrum.ScrollTo(e.HorizontalOffset);
        }

        private void grhSpectrum_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            tones.ScrollTo(e.HorizontalOffset);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            tLogger.Audio.Stop();
        }
    }
}
