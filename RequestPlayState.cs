using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    public enum RequestPlayState : byte
    {
        Stop = 0x00,
        Pause = 0x02,
        Play = 0x09
    }
}
