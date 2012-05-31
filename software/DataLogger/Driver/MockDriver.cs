using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLogger
{
    class MockDriver : IDriver
    {
        double f1 = 941; //941
        double f2 = 1209; //1209

        double sigma = 20;

        long startTicks;

        double t;

        Random r = new Random();

        public MockDriver()
        {
            startTicks = System.DateTime.Now.Ticks;
        }

        public bool IsOpen
        {
            get
            {
                return true;
            }
        }

        public void Open()
        {
        }

        public void Close()
        {
        }

        public bool CheckDevicePresent()
        {
            return true;
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            byte[] response = new byte[responseLength];
            response[0] = (byte)command;

            switch (command)
            {
                case COMMANDS.ADC_READ:
                    double audio;

                    int i;

                    for (i = 2; i < responseLength; i++)
                    {
                        audio = 128.0 + 50.0 * Math.Sin(2.0 * Math.PI * f1 * t) + 50.0 * Math.Sin(2.0 * Math.PI * f2 * t) + (sigma * (r.NextDouble() - 0.5));
                        //audio = 128.0 + 10 * (sigma * (r.NextDouble() - 0.5));

                        response[i] = (byte)audio;

                        t += 1.0 / TelephoneLogger.SAMPLING_RATE;
                    }

                    response[1] = (byte)i;

                    break;
                default:
                    throw new NotImplementedException();
            }

            return response;
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            throw new NotImplementedException();
        }
    }
}
