using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace DataLogger
{
    class Driver : IDriver
    {
        public const int VID = 0x04D8;
        public const int PID = 0x000C;

        private const int RW_TIMEOUT = 1000;

        UsbDevice device;
        UsbDeviceFinder finder = new UsbDeviceFinder(VID, PID);

        UsbEndpointWriter bulkWriter;
        UsbEndpointReader bulkReader;

        UsbTransferQueue queue;

        public Driver()
        {
        }

        public void Open()
        {
            device = UsbDevice.OpenUsbDevice(finder);

            if (device == null) throw new DeviceNotFoundException("Could not find device");

            bulkWriter = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            bulkReader = device.OpenEndpointReader(ReadEndpointID.Ep01, 64, EndpointType.Bulk);
        }

        public void Close()
        {
            if (device != null && device.IsOpen)
                device.Close();
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            return SendCommand(new byte[] { (byte)command }, responseLength);
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            int sent;
            bulkWriter.Write(command, RW_TIMEOUT, out sent);

            int received = 0;
            int count;
            byte[] buffer = new byte[responseLength];

            while (received < responseLength)
            {
                try
                {
                    bulkReader.Read(buffer, RW_TIMEOUT, out count);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new DriverException("Read aborted by disconnect", ex);
                }

                received += count;
            }

            return buffer;
        }
    }
}
