using RingingBloom.Common;
using RingingBloom.WWiseTypes.NBNK.HIRC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingingBloom.NBNK
{
    class HIRC
    {
        char[] magic = { 'H', 'I', 'R', 'C' };
        uint sectionLength;
        uint objectCount;
        public Dictionary<HIRCTypes, Dictionary<UInt32, HIRCObject>> wwiseObjects = new();
        public Dictionary<UInt32, HIRCObject> all = new();

        public HIRC(BinaryReader br)
        {
            sectionLength = br.ReadUInt32();
            objectCount = br.ReadUInt32();

            foreach (HIRCTypes type in Enum.GetValues<HIRCTypes>())
                wwiseObjects[type] = new();

            Console.WriteLine($"Reading {objectCount} objects, length: {sectionLength} (end: {br.BaseStream.Position + sectionLength})");
            for(int i = 0; i < objectCount; i++)
            {
                HIRCObject newObj = new HIRCObject(br);
                wwiseObjects[newObj.Type].Add(newObj.Id, newObj);
                all.Add(newObj.Id, newObj);
            }
        }

        public int ReturnSectionLength()
        {
            int length = 4;
            for(int i = 0; i < wwiseObjects.Count; i++)
            {
                //length += wwiseObjects[i].CalculateSectionLength();
            }
            return length;
        }

        public void ExportHIRC(BinaryWriter bw)
        {
            bw.Write(magic);
            bw.Write(ReturnSectionLength());
            bw.Write(wwiseObjects.Count);

        }
    }
}
