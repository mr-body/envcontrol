using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace envcontrol.module
{
    internal class SerialPortManager
    {
        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames()
                .OrderBy(port => port)
                .ToArray();
        }
    }
}
