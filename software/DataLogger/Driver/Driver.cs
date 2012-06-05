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

        private bool _connectAutomatically;
        public bool ConnectAutomatically
        {
            get
            {
                return _connectAutomatically;
            }
            set
            {
                if (value && !IsOpen)
                {
                    TryOpen();
                }

                _connectAutomatically = value;
            }
        }

        public event DataReceivedEventHandler DataReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsOpen { get; set; }

        public Driver()
        {
            notifier = DeviceNotifier.OpenDeviceNotifier();
            notifier.OnDeviceNotify += new EventHandler<DeviceNotifyEventArgs>(notifier_OnDeviceNotify);
        }

        void notifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (e.Device.IdVendor == Driver.VID && e.Device.IdProduct == Driver.PID)
            {
                switch (e.EventType)
                {
                    case EventType.DeviceArrival:
                        if (ConnectAutomatically)
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

            device.Open();

            commandWriter = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            commandReader = device.OpenEndpointReader(ReadEndpointID.Ep01, 64, EndpointType.Bulk);
            dataReader = device.OpenEndpointReader(ReadEndpointID.Ep02, 64, EndpointType.Interrupt);

            OnConnect();
            IsOpen = true;

            closing = false;
            dataReceiveThread = new Thread(new ThreadStart(dataReceive));
            dataReceiveThread.Name = "DataReceive";
            dataReceiveThread.Start();
        }

        void dataReceive()
        {
            byte[] buffer = new byte[64];
            int count = 0;
            ErrorCode ec = ErrorCode.MonoApiError;
            
            while (!closing && device.IsOpen)
            {
                ec = dataReader.Read(buffer, RW_TIMEOUT, out count);

                if (ec != ErrorCode.None && ec != ErrorCode.IoTimedOut)
                    break;

                if (count > 0 && !closing)
                    OnDataReceived(buffer, count);
            }
        }

        void OnDataReceived(byte[] buffer, int count)
        {
            Debug.Print("{0}", buffer[0]);

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
            if (!IsOpen)
                return;

            IsOpen = false;

            closing = true;
            dataReader.Abort();
            dataReceiveThread.Join();
            dataReceiveThread.Abort();
            dataReceiveThread = null;

            device.Close();
            commandReader.Dispose();
            commandWriter.Dispose();
            dataReader.Dispose();

            device = null;

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
            catch (ObjectDisposedException)
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
            if (IsOpen)
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
