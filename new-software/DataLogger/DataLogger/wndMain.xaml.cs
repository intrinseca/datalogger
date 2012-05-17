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
using System.Windows.Threading;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class wndMain : Window
    {
        public bool LED
        {
            get { return (bool)GetValue(LEDProperty); }
            set { SetValue(LEDProperty, value); }
        }

        public static readonly DependencyProperty LEDProperty = DependencyProperty.Register(
            "LED",
            typeof(bool),
            typeof(wndMain), 
            new UIPropertyMetadata(false));

        UsbDevice logger;
        UsbDeviceFinder finder = new UsbDeviceFinder(0x04D8, 0x000C);
        UsbEndpointWriter writer;
        UsbEndpointReader reader;

        DispatcherTimer poll = new DispatcherTimer();

        public wndMain()
        {
            InitializeComponent();

            // Find and open the usb device.
            logger = UsbDevice.OpenUsbDevice(finder);

            // If the device is open and ready
            if (logger == null) throw new Exception("Device Not Found.");

            writer = logger.OpenEndpointWriter(WriteEndpointID.Ep01);
            reader = logger.OpenEndpointReader(ReadEndpointID.Ep01);

            poll.Interval = new TimeSpan(0, 0, 0, 0, 10);
            poll.Tick += new EventHandler(poll_Tick);
            poll.Start();

            chkLED.SetBinding(CheckBox.IsCheckedProperty, new Binding("LED") { Source = this });
        }

        void poll_Tick(object sender, EventArgs e)
        {
            readADC();
            LED = !LED;
            updateLED();
        }

        private void updateLED()
        {
            if (LED)
            {
                var response = commandWithResponse(new byte[] { 0xEE, 1 }, 2);
            }
            else
            {
                var response = commandWithResponse(new byte[] { 0xEE, 0 }, 2);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (logger.IsOpen) logger.Close();
            logger = null;
        }

        private byte[] commandWithResponse(byte command, int responseLength)
        {
            return commandWithResponse(new byte[] { command }, responseLength);
        }

        private byte[] commandWithResponse(byte[] command, int responseLength)
        {
            int sent;
            writer.Write(command, 1000, out sent);

            int received = 0;
            int count;
            byte[] buffer = new byte[responseLength];

            while (received < responseLength)
            {
                reader.Read(buffer, 1000, out count);
                received += count;
            }

            return buffer;
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            readADC();
        }

        private void readADC()
        {
            var result = commandWithResponse(0xED, 2);
            sldValue.Value = result[1];
        }
    }
}
