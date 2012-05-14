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

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class wndMain : Window
    {
        UsbDevice logger;
        UsbDeviceFinder finder = new UsbDeviceFinder(0x04D8, 0x000C);
        UsbEndpointWriter writer;

        public wndMain()
        {
            InitializeComponent();

            // Find and open the usb device.
            logger = UsbDevice.OpenUsbDevice(finder);

            // If the device is open and ready
            if (logger == null) throw new Exception("Device Not Found.");

            writer = logger.OpenEndpointWriter(WriteEndpointID.Ep01);

        }

        private void chkLED_Checked(object sender, RoutedEventArgs e)
        {
            int sent;
            writer.Write(new byte[] { 0xEE, 1 }, 1000, out sent);
        }

        private void chkLED_Unchecked(object sender, RoutedEventArgs e)
        {
            int sent;
            writer.Write(new byte[] { 0xEE, 0 }, 1000, out sent);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(logger.IsOpen) logger.Close();
            logger = null;
        }
    }
}
