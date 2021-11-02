using RingingBloom.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RingingBloom.WWiseTypes.NBNK.HIRC
{
    public class HIRC3_Action
    {
        public struct type1Add 
        {
            public byte unkn1;
            public uint unkn2;
        }

        public struct ActionPlayParameters
        {
            public byte unkn1;
            public uint NBNKId;
        }

        public struct type10Add
        {
            public byte unknD1;
            public byte unknD2;
            public float unknE1;
            public float unknE2;
            public float unknE3;
            public float unknE4;
        }

        public struct type11Add
        {
            public byte[] unknF;
        }

        public struct type18Add
        {
            public uint objectId;
            public uint id;
        }

        public HIRCTypes Htype = HIRCTypes.Action;
        public uint length;
        public uint objectID;
        public HIRC3ActionScope scope;
        public HIRC3ActionType type;
        public uint TargetId;
        public byte nullByte;
        public byte additionalParameters;
        public List<byte> parameterType = new();
        public List<uint> parameterValue = new();
        public type1Add type1;
        public ActionPlayParameters PlayParameters;
        public type10Add type10;
        public type11Add type11;
        public type11Add type13;
        public type10Add type14;
        public type10Add type15;
        public type18Add type18;
        public type11Add type19;
        public type18Add type25;

        public HIRC3_Action(BinaryReader br)
        {
            scope = (HIRC3ActionScope)br.ReadByte();
            type = (HIRC3ActionType)br.ReadByte();
            TargetId = br.ReadUInt32();
            nullByte = br.ReadByte();
            additionalParameters = br.ReadByte();
            for(int i = 0; i < additionalParameters; i++)
            {
                byte type = br.ReadByte();
                parameterType.Add(type);
            }
            for(int i = 0; i < additionalParameters; i++)
            {
                uint value = br.ReadUInt32();
                parameterValue.Add(value);
            }
            br.ReadByte();
            if (type == HIRC3ActionType.SetState)
            {
                br.ReadUInt32();
                br.ReadUInt32();
            } else if (type == HIRC3ActionType.SetSwitch)
            {
                br.ReadUInt32();
                br.ReadUInt32();
            }
            switch ((int)type)
            {
                case 1:
                    type1.unkn1 = br.ReadByte();
                    type1.unkn2 = br.ReadUInt32();
                    break;
                case 4:
                    PlayParameters.unkn1 = br.ReadByte();
                    PlayParameters.NBNKId = br.ReadUInt32();
                    break;
                case 10:
                    type10.unknD1 = br.ReadByte();
                    type10.unknD2 = br.ReadByte();
                    type10.unknE1 = br.ReadSingle();
                    type10.unknE2 = br.ReadSingle();
                    type10.unknE3 = br.ReadSingle();
                    type10.unknE4 = br.ReadSingle();
                    break;
                case 11:
                    type11.unknF = br.ReadBytes(18);
                    break;
                case 13:
                    type13.unknF = br.ReadBytes(18);
                    break;
                case 14:
                    type14.unknD1 = br.ReadByte();
                    type14.unknD2 = br.ReadByte();
                    type14.unknE1 = br.ReadSingle();
                    type14.unknE2 = br.ReadSingle();
                    type14.unknE3 = br.ReadSingle();
                    type14.unknE4 = br.ReadSingle();
                    break;
                case 15:
                    type15.unknD1 = br.ReadByte();
                    type15.unknD2 = br.ReadByte();
                    type15.unknE1 = br.ReadSingle();
                    type15.unknE2 = br.ReadSingle();
                    type15.unknE3 = br.ReadSingle();
                    type15.unknE4 = br.ReadSingle();
                    break;
                case 18:
                    type18.objectId = br.ReadUInt32();
                    type18.id = br.ReadUInt32();
                    break;
                case 19:
                    type19.unknF = br.ReadBytes(19);
                    break;
                case 25:
                    type25.objectId = br.ReadUInt32();
                    type25.id = br.ReadUInt32();
                    break;
                default:
                    break;
            }
        }

        public void AddParameter()
        {
            additionalParameters++;
            parameterType.Add(0);
            parameterValue.Add(0);
            RecalcLength();
        }

        public void RecalcLength()
        {
            int addLength = 0;
            switch ((int)type)
            {
                case 1:
                    addLength = 5;
                    break;
                case 4:
                    addLength = 5;
                    break;
                case 10:
                    addLength = 18;
                    break;
                case 11:
                    addLength = 18;
                    break;
                case 13:
                    addLength = 18;
                    break;
                case 14:
                    addLength = 18;
                    break;
                case 15:
                    addLength = 18;
                    break;
                case 18:
                    addLength = 8;
                    break;
                case 19:
                    addLength = 19;
                    break;
                case 25:
                    addLength = 8;
                    break;
                default:
                    break;
            }
            length =(uint)(12 + (additionalParameters * 5) + addLength);
        }

        public int GetLength()
        {
            RecalcLength();
            return (int)length;
        }

        public void Export(BinaryWriter bw)
        {
            bw.Write((byte)Htype);
            bw.Write(length); //to do: figure out best approach for fixing length value
            bw.Write(objectID);
            bw.Write((byte)scope);
            bw.Write((byte)type);
            bw.Write(TargetId);
            bw.Write(nullByte);
            bw.Write(additionalParameters);
            for (int i = 0; i < additionalParameters; i++)
            {
                bw.Write(parameterType[i]);
            }
            for (int i = 0; i < additionalParameters; i++)
            {
                bw.Write(parameterValue[i]);
            }
            switch ((int)type)
            {
                case 1:
                    bw.Write(type1.unkn1);
                    bw.Write(type1.unkn2);
                    break;
                case 4:
                    bw.Write(PlayParameters.unkn1);
                    bw.Write(PlayParameters.NBNKId);
                    break;
                case 10:
                    bw.Write(type10.unknD1);
                    bw.Write(type10.unknD2);
                    bw.Write(type10.unknE1);
                    bw.Write(type10.unknE2);
                    bw.Write(type10.unknE3);
                    bw.Write(type10.unknE4);
                    break;
                case 11:
                    bw.Write(type11.unknF);
                    break;
                case 13:
                    bw.Write(type13.unknF);
                    break;
                case 14:
                    bw.Write(type14.unknD1);
                    bw.Write(type14.unknD2);
                    bw.Write(type14.unknE1);
                    bw.Write(type14.unknE2);
                    bw.Write(type14.unknE3);
                    bw.Write(type14.unknE4);
                    break;
                case 15:
                    bw.Write(type15.unknD1);
                    bw.Write(type15.unknD2);
                    bw.Write(type15.unknE1);
                    bw.Write(type15.unknE2);
                    bw.Write(type15.unknE3);
                    bw.Write(type15.unknE4);
                    break;
                case 18:
                    bw.Write(type18.objectId);
                    bw.Write(type18.id);
                    break;
                case 19:
                    bw.Write(type19.unknF);
                    break;
                case 25:
                    bw.Write(type25.objectId);
                    bw.Write(type25.id);
                    break;
                default:
                    break;
            }
        }
    }
}
