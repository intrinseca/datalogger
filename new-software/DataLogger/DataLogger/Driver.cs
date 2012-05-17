using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace DataLogger
{
    class Driver
    {
        public const int VID = 0x04D8;
        public const int PID = 0x000C;

        private const int RW_TIMEOUT = 1000;

        UsbDevice device;
        UsbDeviceFinder finder = new UsbDeviceFinder(VID, PID);
        UsbEndpointWriter writer;
        UsbEndpointReader reader;

        public Driver()
        {
        }

        public void Open()
        {
            device = UsbDevice.OpenUsbDevice(finder);

            // If the device is open and ready
            if (device == null) throw new Exception("Device Not Found.");

            writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);
            reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
        }

        public void Close()
        {
            if (device.IsOpen)
                device.Close();
        }

        public byte[] SendCommand(byte command, int responseLength)
        {
            return SendCommand(new byte[] { command }, responseLength);
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            int sent;
            writer.Write(command, RW_TIMEOUT, out sent);

            int received = 0;
            int count;
            byte[] buffer = new byte[responseLength];

            while (received < responseLength)
            {
                reader.Read(buffer, RW_TIMEOUT, out count);
                received += count;
            }

            return buffer;
        }
    }
}
