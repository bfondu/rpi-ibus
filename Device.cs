using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    public static class Device
    {
        public static byte? CDC = 0x18;
        public static byte? RAD = 0x68;
        public static byte? Broadcast = 0xFF;
    }
}
