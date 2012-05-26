using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DataLogger
{
    /// <summary>
    /// Container class for telephone logger
    /// </summary>
    class TelephoneLogger
    {
        private const int SAMPLING_RATE = 10;
        private const int BLOCK_SIZE = 256;

        /// <summary>
        /// Connection to the USB device
        /// </summary>
        public IDriver Device = new MockDriver();

        /// <summary>
        /// Storage and processing of audio data
        /// </summary>
        public AudioProcessor Audio = new AudioProcessor(SAMPLING_RATE, BLOCK_SIZE);

        public bool Connected { get; private set; }

        /// <summary>
        /// TODO: Move into device class
        /// </summary>
        private DeviceMonitor monitor;

        public TelephoneLogger()
        {
            monitor = new DeviceMonitor(Device);
        }

        public void PollDevice()
        {
            var result = Device.SendCommand(COMMANDS.ADC_READ, 64);

            for (int i = 1; i < result.Length; i++)
            {
                int sample = 128 - result[i];
                Audio.Samples.Add((short)sample);
            }
        }
    }
}
