using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibUsbDotNet.DeviceNotify;
using System.Windows;

namespace DataLogger
{
    class DeviceMonitor
    {
        public bool DevicePresent { get; set; }

        private IDeviceNotifier notifier = DeviceNotifier.OpenDeviceNotifier();
        private IDriver driver;

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public DeviceMonitor(IDriver _driver)
        {
            driver = _driver;
            notifier.OnDeviceNotify += new EventHandler<DeviceNotifyEventArgs>(notifier_OnDeviceNotify);

            DevicePresent = driver.CheckDevicePresent();
            tryOpen();
        }

        void notifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (e.Device.IdVendor == Driver.VID && e.Device.IdProduct == Driver.PID)
            {
                switch (e.EventType)
                {
                    case EventType.DeviceArrival:

                        DevicePresent = driver.CheckDevicePresent();
                        tryOpen();
                        break;
                    case EventType.DeviceRemoveComplete:
                        if (DevicePresent)
                        {
                            onDisconnect();
                            driver.Close();
                            DevicePresent = driver.CheckDevicePresent();
                        }
                        break;
                }
            }
        }

        private void tryOpen()
        {
            if (DevicePresent)
            {
                driver.Open();
                onConnect();
            }
        }

        private void onConnect()
        {
            if (Connected != null)
                Connected(this, new EventArgs());
        }

        private void onDisconnect()
        {
            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }
    }
}
