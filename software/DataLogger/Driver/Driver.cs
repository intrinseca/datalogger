using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Diagnostics;
using System.Threading;
using LibUsbDotNet.DeviceNotify;

namespace DataLogger
{
    class Driver : IDriver, IDisposable
    {
        public const int VID = 0x04D8;
        public const int PID = 0x000C;

        private const int RW_TIMEOUT = 1000;

        UsbDevice device;
        UsbDeviceFinder finder = new UsbDeviceFinder(VID, PID);
        IDeviceNotifier notifier;

        UsbEndpointWriter commandWriter;
        UsbEndpointReader commandReader;
        UsbEndpointReader dataReader;

        Thread dataReceiveThread;
        volatile bool closing;

        public bool AutomaticConnection { get; private set; }

        public event DataReceivedEventHandler DataReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsOpen
        {
            get
            {
                return (device != null && device.IsOpen);
            }
        }

        public Driver(bool _automaticConnection)
        {
            AutomaticConnection = _automaticConnection;

            if (AutomaticConnection)
            {
                notifier = DeviceNotifier.OpenDeviceNotifier();
                notifier.OnDeviceNotify += new EventHandler<DeviceNotifyEventArgs>(notifier_OnDeviceNotify);

                TryOpen();
            }
        }

        void notifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (e.Device.IdVendor == Driver.VID && e.Device.IdProduct == Driver.PID)
            {
                switch (e.EventType)
                {
                    case EventType.DeviceArrival:
                        if (AutomaticConnection)
                            Open();
                        break;
                    case EventType.DeviceRemoveComplete:
                        Close();
                        break;
                }
            }
        }

        private void OnConnect()
        {
            if (Connected != null)
                Connected(this, new EventArgs());
        }

        private void OnDisconnect()
        {
            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }

        public bool TryOpen()
        {
            device = UsbDevice.OpenUsbDevice(finder);

            if (device != null)
            {
                Open();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Open()
        {
            if (device == null)
            {
                device = UsbDevice.OpenUsbDevice(finder);
                if (device == null)
                {
                    throw new DeviceNotFoundException("Could not find device");
                }
            }

            commandWriter = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            commandReader = device.OpenEndpointReader(ReadEndpointID.Ep01, 64, EndpointType.Bulk);
            dataReader = device.OpenEndpointReader(ReadEndpointID.Ep02, 64, EndpointType.Interrupt);

            OnConnect();

            closing = false;
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
                if (count > 0 && !closing)
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
                device = null;
            }

            OnDisconnect();
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            return SendCommand(new byte[] { (byte)command }, responseLength);
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            int sent;
            try
            {
                commandWriter.Write(command, RW_TIMEOUT, out sent);
            }
            catch(ObjectDisposedException)
            {
                throw new DriverException("Writing to closed device");
            }

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

        public void Dispose()
        {
            //close connection
            Close();

            //stop event notifier
            if (notifier != null)
            {
                notifier.Enabled = false;
                notifier = null;
            }
        }
    }
}
