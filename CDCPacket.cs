using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    public class CDCPacket : Packet
    {
        /*
         * 39 = Play Or 38 = Pause
07 = End Or 02 = Play Or 00 = Stop Or 09 = CD Check
09 = Request
00 = Errors (FF for all; Bit0,HIGH_TEMP,Bit2,NO_DISC,NO_DISCS,Bit5,Bit6,Bit7)
30 = CDs loaded (send FF for all)
00 = ?
05 = CD 
14 = Track
         */

        public byte Mode
        {
            get { return Data[0]; }
            set { Data[0] = value; }
        }

        public PlayState PlayMode
        {
            get { return (PlayState)Data[1]; }
            set { Data[1] = (byte)value; }
        }

        public RequestPlayState RequestMode
        {
            get { return (RequestPlayState)Data[2]; }
            set { Data[2] = (byte)value; }
        }

        public byte Errors
        {
            get { return Data[3]; }
            set { Data[3] = value; }
        }

        public byte LoadedCDs
        {
            get { return Data[4]; }
            set { Data[4] = value; }
        }

        public byte UnknownBit
        {
            get { return Data[5]; }
            set { Data[5] = value; }
        }

        public byte CurrentCD
        {
            get { return Data[6]; }
            set { Data[6] = value; }
        }

        public byte CurrentTrack
        {
            get { return Data[7]; }
            set { Data[7] = value; }
        }

        public CDCPacket()
        {
            Data = new List<byte>(new byte[8]);

            // Default data
            Mode = 0x39;
            LoadedCDs = 0xFF;
            CurrentCD = 0x01;
            CurrentTrack = 0x01;
            Source = Device.CDC;
            Destination = Device.RAD;
        }
    }
}
