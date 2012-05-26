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

            var b = new Binding("Samples");
            b.Source = tLogger.Audio;
            gphData.SetBinding(Graph.DataProperty, b);
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

            sldValue.Value = sample;

            //TODO: Add all recently added samples
            //gphData.AddPoint(t, sample);
        }

        private void sldPoll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            poll.Interval = new TimeSpan(0, 0, 0, 0, (int)sldPoll.Value);
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
            tLogger.Audio.ProcessSpectrum();

            //TODO: Waveform display or something here
            //grhWaveform.ClearPoints();
            grhWaveform.Timebase = 8.0f / tLogger.Audio.SamplingRate;
            //for (int i = 0; i < audio.Samples.Count; i++)
            //{
            //    grhWaveform.AddPoint(i, audio.Samples[i], false);
            //}

            grhSpectrum.Timebase = grhWaveform.Timebase;
            grhSpectrum.BlockSize = tLogger.Audio.BlockSize;
            grhSpectrum.Data = tLogger.Audio.Spectrum;
            grhSpectrum.Refresh();
        }
    }
}
