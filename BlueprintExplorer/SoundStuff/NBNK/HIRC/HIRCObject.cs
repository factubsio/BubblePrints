using RingingBloom.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingingBloom.WWiseTypes.NBNK.HIRC
{
    public class HIRC_Switch
    {

        public HIRC_Switch(BinaryReader br)
        {
            Console.WriteLine($"Parsing switch at: {br.BaseStream.Position}");

        }

    }

    public class HIRC_Sequence
    {
        public UInt32 []Elements;
        public HIRC_Sequence(BinaryReader br)
        {
            //Console.WriteLine($"Parsing sequennce at: {br.BaseStream.Position}");
            br.BaseStream.Seek(14, SeekOrigin.Current);
            byte test = br.ReadByte();
            if (test == 0)
                br.BaseStream.Seek(35, SeekOrigin.Current);
            else if (test == 3)
                br.BaseStream.Seek(36, SeekOrigin.Current);
            else if (test == 6)
                br.BaseStream.Seek(45, SeekOrigin.Current);
            uint elementCount = br.ReadUInt32();
            if (elementCount > 100)
            {
                Console.WriteLine($"Got suspicious sequence with count: {elementCount}");
            }
            Elements = new UInt32[elementCount];
            for (uint i = 0; i < Elements.Length; i++)
            {
                Elements[i] = br.ReadUInt32();
            }
        }
    }
    public class HIRC_Sound
    {
        public UInt32 TargetId;
        public HIRC_Sound(BinaryReader br)
        {
            br.ReadUInt32();
            UInt32 streamLocation = br.ReadByte();
            TargetId = br.ReadUInt32();
            UInt32 sourceID = br.ReadUInt32();
        }
    }

    public class HIRCObject
    {
        public HIRC_Switch Switch = null;
        public HIRC_Sequence Sequence = null;
        public HIRC_Sound Sound = null;
        public HIRC1_Settings SettingsObject = null;
        public HIRC3_Action EventAction = null;
        public HIRC_Event Event = null;
        public HIRC10 MusicSegment = null;
        public byte[] RawUnknown = null;

        public HIRCTypes Type;
        public UInt32 Id;
        public UInt32 Length;

        public override string ToString()
        {
            return $"{Type} id:{Id} length:{Length}";
        }


        public HIRCObject(BinaryReader br)
        {
            Type = (HIRCTypes)br.ReadByte();
            Length = br.ReadUInt32();
            long after = br.BaseStream.Position + Length;

            Id = br.ReadUInt32();

            //Console.WriteLine($"reading section, type: {Type}, length: {Length}   (current: {br.BaseStream.Position})");

            switch (Type)
            {
                case HIRCTypes.Settings:
                    SettingsObject = new HIRC1_Settings(br);
                    break;
                case HIRCTypes.RandomSequence:
                    Sequence = new HIRC_Sequence(br);
                    break;
                case HIRCTypes.Switch:
                    Switch = new HIRC_Switch(br);
                    Console.WriteLine($"Length: {Length}");
                    break;
                case HIRCTypes.Sound:
                    Sound = new HIRC_Sound(br);
                    break;
                case HIRCTypes.Action:
                    EventAction = new HIRC3_Action(br);
                    break;
                case HIRCTypes.Event:
                    Event = new HIRC_Event(br);
                    break;
                default:
                    RawUnknown = br.ReadBytes((int)(Length - 4));
                    break;
            }

            if (br.BaseStream.Position > after)
            {
                throw new Exception("Parsing HIRC section overrun");
            }
            else if (br.BaseStream.Position < after)
            {
                //Console.WriteLine("skipping padding bytes after section");
                br.BaseStream.Seek(after, SeekOrigin.Begin);
            }


        }

        public int CalculateSectionLength()
        {
            int length = 5;
            if (SettingsObject != null)
            {
                length += SettingsObject.GetLength();
            }
            if (EventAction != null)
            {
                length += EventAction.GetLength();
            }
            if (Event != null)
            {
                length += Event.GetLength();
            }
            //if (Datums != null)
            //{
            //    length += Datums.Sum(d => d.GetLength());
            //}
            return length;
        }

        public void Export(BinaryWriter bw)
        {
            if(SettingsObject != null)
            {
                SettingsObject.Export(bw);
            }
            if (EventAction != null)
            {
                EventAction.Export(bw);
            }
            if (Event != null)
            {
                Event.Export(bw);
            }
            //if (Datums != null)
            //{
            //    //help
            //    //Datums.Export(bw);
            //}
        }
    }
}
