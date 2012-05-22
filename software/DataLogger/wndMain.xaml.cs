﻿using System;
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
        public bool IsConnected { get; set; }
        public bool IsPolling { get; set; }

        private Driver logger = new Driver();
        private IDeviceNotifier notifier = DeviceNotifier.OpenDeviceNotifier();

        DispatcherTimer poll = new DispatcherTimer();

        int t = 0;

        public wndMain()
        {
            InitializeComponent();

            notifier.OnDeviceNotify += new EventHandler<DeviceNotifyEventArgs>(notifier_OnDeviceNotify);

            try
            {
                connect();
            }
            catch (DeviceNotFoundException)
            {

            }

            poll.Interval = new TimeSpan(0, 0, 0, 0, 5);
            poll.Tick += new EventHandler(poll_Tick);
            IsPolling = false;
            IsConnected = false;
        }

        private void connect()
        {
            logger.Open();
            IsConnected = true;

            sbiConnectionStatus.Content = "Connected";
        }

        private void disconnect()
        {
            stopSampling();

            logger.Close();
            IsConnected = false;

            sbiConnectionStatus.Content = "Not Connected";
        }

        private void stopSampling()
        {
            poll.Stop();
            IsPolling = false;
        }

        void notifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (e.Device.IdVendor == Driver.VID && e.Device.IdProduct == Driver.PID)
            {
                switch (e.EventType)
                {
                    case EventType.DeviceArrival:
                        connect();
                        break;
                    case EventType.DeviceRemoveComplete:
                        disconnect();
                        break;
                }
            }
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
                var result = logger.SendCommand(0xED, 2);
                sldValue.Value = result[1];
                gphData.AddPoint(t, (float)(result[1] / 255.0f) - 0.5f);
                t++;
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
            IsPolling = true;
            poll.Start();
        }
    }
}
