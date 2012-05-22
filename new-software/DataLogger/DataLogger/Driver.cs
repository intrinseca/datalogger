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

        UsbEndpointWriter bulkWriter;
        UsbEndpointReader bulkReader;
        UsbEndpointReader isoReader;

        UsbTransferQueue queue;

        public Driver()
        {
        }

        public void Open()
        {
            device = UsbDevice.OpenUsbDevice(finder);

            // If the device is open and ready
            if (device == null) throw new Exception("Device Not Found.");

            bulkWriter = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            bulkReader = device.OpenEndpointReader(ReadEndpointID.Ep01, 64, EndpointType.Bulk);

            //isoReader = device.OpenEndpointReader(ReadEndpointID.Ep02, 1024, EndpointType.Isochronous);
        }

        public void ReceiveIso()
        {
            queue = new UsbTransferQueue(isoReader, 3, 64, 1000, 64);

            int transferCount = 0;
            ErrorCode ec;

            do
            {
                UsbTransferQueue.Handle h;

                ec = queue.Transfer(out h);
                if (ec != ErrorCode.Success)
                    throw new Exception("Read Failed");

                transferCount++;
            } while (transferCount < 10);
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
            bulkWriter.Write(command, RW_TIMEOUT, out sent);

            int received = 0;
            int count;
            byte[] buffer = new byte[responseLength];

            while (received < responseLength)
            {
                bulkReader.Read(buffer, RW_TIMEOUT, out count);
                received += count;
            }

            return buffer;
        }
    }
}
