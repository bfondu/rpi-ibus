using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBUS_sniffer
{
    public class Packet
    {
        public byte? Source { get; set; }
        public byte? Length { get; set; }
        public byte? Destination { get; set; }
        public List<byte> Data { get; set; }
        public byte Checksum { get; set; }

        public bool IsValid
        {
            get
            {
                return CalculateChecksum() == Checksum;
            }
        }

        public Packet()
        {
            Data = new List<byte>();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool includeValid)
        {
            string result = string.Format("{0:X} {1:X} {2:X} {3} {4:X}", Source, Length, Destination, string.Join(" ", Data.Select(x => x.ToString("X"))), Checksum);

            if (includeValid)
            {
                result += ", " + IsValid.ToString();
            }

            return result;
        }

        private byte CalculateChecksum()
        {
            byte calcChecksum = 0x00;

            if (Destination == null || Source == null || Length == null)
                    return 0x00;

            calcChecksum ^= Source.Value;
            calcChecksum ^= Length.Value;
            calcChecksum ^= Destination.Value;

            foreach (var b in Data)
            {
                calcChecksum ^= b;
            }

            return calcChecksum;
        }

        public byte[] GenerateSendablePacket()
        {
            Length = (byte?)(Data.Count + 2);
            Checksum = CalculateChecksum();

            var bytes = new List<byte>();
            bytes.Add(Source.Value);
            bytes.Add(Length.Value);
            bytes.Add(Destination.Value);
            bytes.AddRange(Data);
            bytes.Add(Checksum);

            return bytes.ToArray();
        }
    }
}
