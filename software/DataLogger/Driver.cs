using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace DataLogger
{
    [Serializable]
    public class DeviceNotFoundException : Exception
    {
        public DeviceNotFoundException() { }
        public DeviceNotFoundException(string message) : base(message) { }
        public DeviceNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected DeviceNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class DriverException : Exception
    {
        public DriverException() { }
        public DriverException(string message) : base(message) { }
        public DriverException(string message, Exception inner) : base(message, inner) { }
        protected DriverException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    class Driver
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

        public byte[] SendCommand(byte command, int responseLength)
        {
            return SendCommand(new byte[] { command }, responseLength);
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
