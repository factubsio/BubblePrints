using BubbleAssets;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WikiGen.Assets
{

    public enum EndianType { Big, Little }

    public class BinaryReaderWithEndian : BinaryReader
    {
        public bool BigEndian = false;
        public BinaryReaderWithEndian(System.IO.Stream stream, EndianType endian) : base(stream)
        {
            BigEndian = endian == EndianType.Big;
        }

        public void Align(int to)
        {
            long next = ((BaseStream.Position + to - 1) / to) * to;
            BaseStream.Position = next;
        }
        public override UInt16 ReadUInt16()
        {
            var data = base.ReadBytes(2);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override UInt32 ReadUInt32()
        {
            var data = base.ReadBytes(4);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override UInt64 ReadUInt64()
        {
            var data = base.ReadBytes(8);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override Int16 ReadInt16()
        {
            var data = base.ReadBytes(2);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override Int32 ReadInt32()
        {
            var data = base.ReadBytes(4);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override Int64 ReadInt64()
        {
            var data = base.ReadBytes(8);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public string ReadCString(int sofLimit = int.MaxValue, int hardLimit = 32000)
        {
            StringBuilder str = new();

            while (true)
            {
                var next = base.ReadByte();
                if (next == 0)
                    break;

                str.Append((char)next);

                if (str.Length >= sofLimit)
                    break;

                if (str.Length > hardLimit)
                    throw new Exception();
            }


            return str.ToString();
        }
    }
}
