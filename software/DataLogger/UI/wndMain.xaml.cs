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
        /// Wrapper class for business logic
        /// </summary>
        private TelephoneLogger tLogger;

        /// <summary>
        /// Constructor
        /// </summary>
        public wndMain()
        {
            InitializeComponent();

            //Initialise business logic
            tLogger = new TelephoneLogger();

            //Attach event handlers
            tLogger.Device.Connected += new EventHandler(Device_Connected);
            tLogger.Device.Disconnected += new EventHandler(Device_Disconnected);

            //Enable connection
            tLogger.Device.ConnectAutomatically = true;

            this.DataContext = tLogger;

            //Set up tone display blocksize
            tones.BlockSize = tLogger.Audio.BlockSize;
        }

        void Device_Connected(object sender, EventArgs e)
        {
            sbiConnectionStatus.Content = "Connected";
        }

        void Device_Disconnected(object sender, EventArgs e)
        {
            sbiConnectionStatus.Content = "Not Connected";
        }

        /// <summary>
        /// Start capturing audio data
        /// </summary>
        private void startCapture()
        {
            if (tLogger.Device.IsOpen)
            {
                //If the device is connected, start capturing
                tLogger.Capturing = true;
            }
        }

        /// <summary>
        /// Stop capturing audio data
        /// </summary>
        private void stopCapturing()
        {
            tLogger.Capturing = false;
        }

        private void btnStopCapture_Click(object sender, RoutedEventArgs e)
        {
            stopCapturing();
        }

        private void btnStartCapture_Click(object sender, RoutedEventArgs e)
        {
            startCapture();
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

        private void btnSample_Click(object sender, RoutedEventArgs e)
        {
            byte result = tLogger.GetADC();
            Debug.Print(result.ToString());
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            tLogger.Dispose();
        }
    }
}
