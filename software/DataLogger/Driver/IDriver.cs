using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    enum COMMANDS
    {
        ADC_READ = 0x10,
        PORTD_SET = 0x20,
        SAMPLING_START = 0x30,
        SAMPLING_STOP = 0x31,
        SAMPLING_SEND = 0x32,
    };

    interface IDriver
    {
        void Open();
        void Close();
        bool CheckDevicePresent();

        byte[] SendCommand(COMMANDS command, int responseLength);
        byte[] SendCommand(byte[] command, int responseLength);
    }
}
