using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    public class State
    {
        public byte CurrentCD { get; set; }
        public byte CurrentTrack { get; set; }

        public PlayState CurrentPlayState { get; set; }
        public RequestPlayState CurrentRequestPlayState { get; set; }
    }
}
