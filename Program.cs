using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using log4net;
using System.Configuration;
using System.Diagnostics;

namespace IBUS_sniffer
{
    internal class Program
    {
        private static SerialPort sp;

        private static BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();

        private static bool _isFirstPacket = true;

        private static MusicPlayer _player = new MusicPlayer();

        private static State _state = new State
            {
                CurrentCD = _player.CurrentDirectory,
                CurrentTrack = _player.CurrentFile, 
                CurrentPlayState = PlayState.Stop, 
                CurrentRequestPlayState = RequestPlayState.Pause
            };

        private static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
			AppDomain.CurrentDomain.UnhandledException += GlobalErrorHandler;

            sp = new SerialPort(ConfigurationManager.AppSettings["SerialPort"], 9600, Parity.Even, 8, StopBits.One);
            sp.Handshake = Handshake.None;

            try
            {
                sp.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Startup error: " + ex.Message);

                Log.Exception(ex);
                
                return;
            }

            if (File.Exists(ConfigurationManager.AppSettings["PIDFile"]))
            {
                Console.WriteLine("Already running");

                Log.Fatal("Already running");

                return;
            }

            File.AppendAllText(ConfigurationManager.AppSettings["PIDFile"], Process.GetCurrentProcess().Id.ToString());

            Task.Factory.StartNew(() => HandleData(_packetQueue));

            _player.TrackChanged += PlayerOnTrackChanged;

            int curByte;
            var curPacket = new Packet();
            bool isInit = true;
            while ((curByte = sp.ReadByte()) != -1)
            {
                if (isInit)
                {
                    if (curByte != 0x68)
                        continue;

                    isInit = false;
                }

                if (curPacket.Source == null)
                {
                    curPacket.Source = (byte)curByte;
                }
                else if (curPacket.Length == null)
                {
                    curPacket.Length = (byte)curByte;
                }
                else if (curPacket.Destination == null)
                {
                    curPacket.Destination = (byte)curByte;
                }
                else
                {
                    if (curPacket.Data.Count < curPacket.Length - 2)
                    {
                        curPacket.Data.Add((byte)curByte);
                    }
                    else
                    {
                        curPacket.Checksum = (byte)curByte;

                        Log.Debug(string.Format("{0:HH:mm:ss} - {1}\r\n", DateTime.Now, curPacket.ToString(true)));

                        if (curPacket.IsValid)
                        {
                            _packetQueue.Add(curPacket);
                        }
                        else
                        {
                            isInit = true;
                        }

                        curPacket = new Packet();
                    }
                }
            }

            Console.ReadKey();

            StopProgram();
        }

        private static void GlobalErrorHandler(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            StopProgram();
        }

        private static void StopProgram()
        {
            if (File.Exists(ConfigurationManager.AppSettings["PIDFile"]))
            {
                File.Delete(ConfigurationManager.AppSettings["PIDFile"]);
            }

            _packetQueue.CompleteAdding();

            sp.Close();
        }

        private static void PlayerOnTrackChanged(MusicPlayer sender, TrackChangedEventArgs trackChangedEventArgs)
        {
            _state.CurrentCD = trackChangedEventArgs.Directory;
            _state.CurrentTrack = trackChangedEventArgs.File;

            WriteToSerial(new CDCPacket
            {
                PlayMode = _state.CurrentPlayState,
                RequestMode = _state.CurrentRequestPlayState,
                CurrentCD = _state.CurrentCD,
                CurrentTrack = _state.CurrentTrack
            });
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            StopProgram();
        }

        private static void HandleData(BlockingCollection<Packet> packets)
        {
            foreach (var item in packets.GetConsumingEnumerable())
            {
                Console.WriteLine(item.ToString(true));

                if (_isFirstPacket)
                {
                    WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.Broadcast, Data = new List<byte> { 0x02, 0x01 } });

                    _isFirstPacket = false;
                    continue;
                }

                // RAD -> CDC: PING
                if (item.Source == Device.RAD && item.Destination == Device.CDC && item.Data.Count == 1 && item.Data[0] == 0x01)
                {
                    byte responseByte = 0x00;

                    WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.Broadcast, Data = new List<byte> { 0x02, responseByte } });
                }
                // RAD -> Broadcast: PING RESET
                else if (item.Source == Device.RAD && item.Destination == Device.Broadcast && item.Data.Count == 2 && item.Data[0] == 0x02 && item.Data[1] == 0x01)
                {
                    //WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.Broadcast, Data = new List<byte> { 0x02, 0x01 } });
                    Log.Debug("Radio sends PING RESET");
                }
                // RAD -> CDC: Change to CD #
                else if (item.Source == Device.RAD && item.Destination == Device.CDC 
                    && item.Data.Count == 3 && item.Data[0] == 0x38 && item.Data[1] == 0x06)
                {
                    byte targetCD = item.Data[2];

                    // Buttons 1 .. 6
                    switch (targetCD)
                    {
                        case 1:
                            _state.CurrentCD = 0x09;

                            WriteToSerial(new CDCPacket
                            {
                                PlayMode = _state.CurrentPlayState,
                                RequestMode = _state.CurrentRequestPlayState,
                                CurrentCD = _state.CurrentCD,
                                CurrentTrack = _state.CurrentTrack
                            });
                            break;

                        case 2:
                            _state.CurrentCD = 0x09;
                            _state.CurrentTrack = 0x99;

                            WriteToSerial(new CDCPacket
                            {
                                PlayMode = _state.CurrentPlayState,
                                RequestMode = _state.CurrentRequestPlayState,
                                CurrentCD = _state.CurrentCD,
                                CurrentTrack = _state.CurrentTrack
                            });
                            break;

                        case 3:
                            Random r = new Random();

                            _state.CurrentCD = 0x0F;
                            _state.CurrentTrack = (byte)r.Next(1, 99);

                            WriteToSerial(new CDCPacket
                            {
                                PlayMode = _state.CurrentPlayState,
                                RequestMode = _state.CurrentRequestPlayState,
                                CurrentCD = _state.CurrentCD,
                                CurrentTrack = _state.CurrentTrack
                            });
                            break;

                        case 4:
                            string text4 = "Hello World";
                            byte[] textList4 = text4.ToCharArray().Select(x => (byte)x).ToArray();

                            var data4 = new List<byte> { 0x23 };
                            data4.AddRange(textList4);

                            WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.RAD, Data = data4 });
                            break;

                        case 5:
                            // Try to send "Hello World" to RAD
                            string text5 = "Hello World";
                            byte[] textList5 = text5.ToCharArray().Select(x => (byte)x).ToArray();

                            //var data5 = new List<byte> { 0x1A, 0x38, 0x00 };
                            var data5 = new List<byte> { 0x23, 0xc0, 0x30, 0x07 };
                            data5.AddRange(textList5);

                            WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.RAD, Data = data5 });
                            break;

                        case 6:
                            // Try to send "Hello World" to RAD
                            string text6 = "Hello World";
                            byte[] textList6 = text6.ToCharArray().Select(x => (byte)x).ToArray();

                            var data6 = new List<byte> { 0x42 };
                            data6.AddRange(textList6);

                            WriteToSerial(new Packet { Source = Device.CDC, Destination = Device.RAD, Data = data6 });
                            break;
                    }
                }
                // RAD -> CDC: Status request (which CDs available)
                else if (item.Source == Device.RAD && item.Destination == Device.CDC && item.Data.Count == 3 && 
                         item.Data[0] == 0x38 && item.Data[1] == 0x00 && item.Data[2] == 0x00)
                {
                    WriteToSerial(new CDCPacket
                        {
                            PlayMode = _state.CurrentPlayState,
                            RequestMode = _state.CurrentRequestPlayState,
                            CurrentCD = _state.CurrentCD,
                            CurrentTrack = _state.CurrentTrack
                        });
                }
                // RAD -> CDC: Stop
                else if (item.Source == Device.RAD && item.Destination == Device.CDC && item.Data.Count == 3 &&
                         item.Data[0] == 0x38 && item.Data[1] == 0x01)
                {
                    _state.CurrentPlayState = PlayState.Stop;
                    _state.CurrentRequestPlayState = RequestPlayState.Stop;

                    _player.StopPlayer();

                    WriteToSerial(new CDCPacket
                    {
                        PlayMode = _state.CurrentPlayState,
                        RequestMode = _state.CurrentRequestPlayState,
                        CurrentCD = _state.CurrentCD,
                        CurrentTrack = _state.CurrentTrack
                    });
                }
                // RAD -> CDC: Fast FWD/ REV
                else if (item.Source == Device.RAD && item.Destination == Device.CDC && item.Data.Count == 3 &&
                         item.Data[0] == 0x38 && item.Data[1] == 0x04)
                {
                    if (item.Data[2] == 0x01)
                    {
                        //_state.CurrentCD++;

                        _player.NextDirectory();
                    }
                    else
                    {
                        //_state.CurrentCD--;

                        _player.PreviousDirectory();
                    }

                    _state.CurrentCD = _player.CurrentDirectory;
                    _state.CurrentTrack = _player.CurrentFile;

                    _state.CurrentPlayState = PlayState.Play;
                    _state.CurrentRequestPlayState = RequestPlayState.Play;

                    WriteToSerial(new CDCPacket
                    {
                        PlayMode = _state.CurrentPlayState,
                        RequestMode = _state.CurrentRequestPlayState,
                        CurrentCD = _state.CurrentCD,
                        CurrentTrack = _state.CurrentTrack
                    });
                }
                // RAD -> CDC: Request track
                else if (item.Source == Device.RAD && item.Destination == Device.CDC && item.Data.Count == 3 &&
                         item.Data[0] == 0x38)
                {
                    if (item.Data[1] == 0x05) // 0x0A according to spec
                    {
                        if (item.Data[2] == 0x00)
                        {
                            //_state.CurrentTrack++;

                            _player.NextFile();
                        }
                        else
                        {
                            //_state.CurrentTrack--;

                            _player.PreviousFile();
                        }

                        _state.CurrentCD = _player.CurrentDirectory;
                        _state.CurrentTrack = _player.CurrentFile;
                    }
                  
                    _state.CurrentPlayState = PlayState.Play;
                    _state.CurrentRequestPlayState = RequestPlayState.Play;

                    WriteToSerial(new CDCPacket
                        {
                            PlayMode = _state.CurrentPlayState,
                            RequestMode = _state.CurrentRequestPlayState,
                            CurrentCD = _state.CurrentCD,
                            CurrentTrack = _state.CurrentTrack
                        });
                }
            }
        }

        private static void WriteToSerial(Packet packet)
        {
            if (sp.IsOpen)
            {
                var bytes = packet.GenerateSendablePacket();
                sp.Write(bytes, 0, bytes.Length);

                Log.Debug(string.Format("{0:HH:mm:ss} - OUT - {1}\r\n", DateTime.Now, packet.ToString(true)));
            }
        }
    }
}
