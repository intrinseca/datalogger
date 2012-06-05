using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLogger
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Buffer { get; set; }
    }

    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

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

    enum COMMANDS
    {
        ADC_READ = 0x10,
        PORTD_SET = 0x20,
        CAPTURE_START = 0x30,
        CAPTURE_STOP = 0x31,
    };

    interface IDriver : IDisposable
    {
        bool IsOpen { get; }
        bool ConnectAutomatically { get; set; }

        event DataReceivedEventHandler DataReceived;
        event EventHandler Connected;
        event EventHandler Disconnected;

        bool TryOpen();
        void Open();
        void Close();

        byte[] SendCommand(COMMANDS command, int responseLength);
        byte[] SendCommand(byte[] command, int responseLength);
    }
}
