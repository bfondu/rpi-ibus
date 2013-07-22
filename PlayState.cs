using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    //public static class PlayState
    //{
    //    /// <summary>
    //    /// Byte value 0x02
    //    /// </summary>
    //    public static byte Pause = 0x02;
    //    /// <summary>
    //    /// Byte value 0x09
    //    /// </summary>
    //    public static byte Play = 0x09;
    //    /// <summary>
    //    /// Byte value 0x00
    //    /// </summary>
    //    public static byte Stop = 0x00;
    //}

    public enum PlayState : byte
    {
        Stop = 0x00,
        Play = 0x02,
        End = 0x07,
        CDCheck = 0x09
    }
}
