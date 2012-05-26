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

        private IDriver logger = new MockDriver();
        private Audio audio = new Audio(10, 256);

        private DeviceMonitor monitor;

        DispatcherTimer poll = new DispatcherTimer();

        int t = 0;

        public wndMain()
        {
            InitializeComponent();

            monitor = new DeviceMonitor(logger);

            monitor.Connected += new EventHandler(monitor_Connected);
            monitor.Disconnected += new EventHandler(monitor_Disconnected);

            poll.Interval = new TimeSpan(0, 0, 0, 0, 10);
            poll.Tick += new EventHandler(poll_Tick);

            IsPolling = false;
        }

        void monitor_Disconnected(object sender, EventArgs e)
        {
            stopSampling();
            logger.Close();

            sbiConnectionStatus.Content = "Not Connected";
        }

        void monitor_Connected(object sender, EventArgs e)
        {
            logger.Open();
            sbiConnectionStatus.Content = "Connected";
        }

        private void stopSampling()
        {
            poll.Stop();
            IsPolling = false;
        }

        void poll_Tick(object sender, EventArgs e)
        {
            readADC();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            logger.Close();
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            readADC();
        }

        private void readADC()
        {
            try
            {
                var result = logger.SendCommand(COMMANDS.ADC_READ, 64);

                for (int i = 1; i < result.Length; i++)
                {
                    int sample = 128 - result[i];
                    audio.Samples.Add((short)sample);

                    sldValue.Value = result[i];

                    gphData.AddPoint(t, (float)(sample / 255.0f));
                    t++;
                }
            }
            catch (DriverException ex)
            {
                Debug.Print("DriverException: {0}", ex.Message);
                return;
            }
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
            if (monitor.DevicePresent)
            {
                IsPolling = true;
                poll.Start();
            }
        }

        private void btnAnalyse_Click(object sender, RoutedEventArgs e)
        {
            audio.ProcessSpectrum();

            //grhWaveform.ClearPoints();
            grhWaveform.Timebase = 1.0f / audio.SamplingRate;
            //for (int i = 0; i < audio.Samples.Count; i++)
            //{
            //    grhWaveform.AddPoint(i, audio.Samples[i], false);
            //}

            grhSpectrum.Timebase = grhWaveform.Timebase;
            grhSpectrum.BlockSize = audio.BlockSize;
            grhSpectrum.Data = audio.Spectrum;
            grhSpectrum.Refresh();
        }
    }
}
