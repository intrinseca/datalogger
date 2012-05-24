using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLogger
{
    class MockDriver : IDriver
    {
        int t;

        public void Open()
        {
        }

        public void Close()
        {
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            t++;

            switch (command)
            {
                case COMMANDS.ADC_READ:
                    double result;
                    if (t < 256)
                    {
                        result = 128.0 + 50.0 * Math.Sin(2.0 * Math.PI * t / 30.9) + 50.0 * Math.Sin(2.0 * Math.PI * t / 10.0);
                        //double result = 128.0 + (50.0 * Math.Cos(Math.PI * t));
                    }
                    else
                    {
                        result = 0;
                    }
                    return new byte[] { (byte)COMMANDS.ADC_READ, (byte)result };
                default:
                    throw new NotImplementedException();
            }
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            throw new NotImplementedException();
        }
    }
}
