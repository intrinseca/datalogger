using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Diagnostics;
using System.Threading;

namespace DataLogger
{
    class Driver : IDriver
    {
        public const int VID = 0x04D8;
        public const int PID = 0x000C;

        private const int RW_TIMEOUT = 1000;

        UsbDevice device;
        UsbDeviceFinder finder = new UsbDeviceFinder(VID, PID);

        UsbEndpointWriter commandWriter;
        UsbEndpointReader commandReader;
        UsbEndpointReader dataReader;

        private Thread dataReceiveThread;
        private volatile bool closing;

        public event DataReceivedEventHandler DataReceived;

        public bool IsOpen
        {
            get
            {
                return device.IsOpen;
            }
        }

        public Driver()
        {
        }

        public bool CheckDevicePresent()
        {
            device = UsbDevice.OpenUsbDevice(finder);

            return (device != null);
        }

        public void Open()
        {
            if (!CheckDevicePresent())
            {
                throw new DeviceNotFoundException("Could not find device");
            }

            device = UsbDevice.OpenUsbDevice(finder);

            commandWriter = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            commandReader = device.OpenEndpointReader(ReadEndpointID.Ep01, 64, EndpointType.Bulk);
            dataReader = device.OpenEndpointReader(ReadEndpointID.Ep02, 64, EndpointType.Interrupt);

            dataReceiveThread = new Thread(new ThreadStart(dataReceive));
            dataReceiveThread.Start();
        }

        void dataReader_DataReceived(object sender, EndpointDataEventArgs e)
        {
            Debug.Print(e.Buffer[0].ToString());
        }

        void dataReceive()
        {
            byte[] buffer = new byte[64];
            int count;

            while (!closing)
            {
                dataReader.Read(buffer, RW_TIMEOUT, out count);
                if (count > 0)
                    OnDataReceived(buffer, count);
            }
        }

        void OnDataReceived(byte[] buffer, int count)
        {
            if (DataReceived != null)
            {
                var e = new DataReceivedEventArgs();
                e.Buffer = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    e.Buffer[i] = buffer[i];
                }
                DataReceived(this, e);
            }
        }

        public void Close()
        {
            closing = true;
            dataReceiveThread.Join(2 * RW_TIMEOUT);
            dataReceiveThread.Abort();

            if (device != null && device.IsOpen)
            {
                device.Close();
            }
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            return SendCommand(new byte[] { (byte)command }, responseLength);
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            int sent;
            commandWriter.Write(command, RW_TIMEOUT, out sent);

            int received = 0;
            int count;
            byte[] buffer = new byte[responseLength];

            while (received < responseLength)
            {
                try
                {
                    commandReader.Read(buffer, RW_TIMEOUT, out count);
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
