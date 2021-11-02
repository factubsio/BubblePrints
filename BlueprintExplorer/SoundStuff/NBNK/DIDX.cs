﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using RingingBloom.Common;
using RingingBloom.WWiseTypes;

namespace RingingBloom.NBNK
{
    class DIDX : IChunk
    {
        //this includes data chunk as well, because no point in separating them
        private char[] magic = new char[] { 'D', 'I', 'D', 'X' };
        private char[] DATA = new char[] { 'D', 'A', 'T', 'A' };
        public List<Wem> wemList;
        private int pLoadedMedia { get => wemList.Count; }
        public int didxSize { get => wemList.Count * 12; }
        public int dataSize { get
            {
                int size = 0;
                for(int i = 0; i < wemList.Count; i++)
                {
                    size += (int)wemList[i].length;
                }
                return size;
            } }

        public DIDX(BinaryReader br, Labels labels)
        {
            wemList = new List<Wem>();
            uint didxLength = br.ReadUInt32();
            uint wemCount = didxLength / 12;
            uint[] ids = new uint[wemCount];
            uint[] offsets = new uint[wemCount];
            uint[] lengths = new uint[wemCount];
            for (int i = 0; i < wemCount; i++)
            {
                ids[i] = br.ReadUInt32();
                offsets[i] = br.ReadUInt32();
                lengths[i] = br.ReadUInt32();
            }
            char[] dataRead = br.ReadChars(4);
            if(new string(dataRead) != "DATA")
            {
                throw new Exception("Error reading DATA section");
            }
            uint dataLength = br.ReadUInt32();
            long DataOff = br.BaseStream.Position;
            List<byte[]> wemDatas = new List<byte[]>();
            for (int i = 0; i < wemCount; i++)
            {
                br.BaseStream.Seek(DataOff + offsets[i], SeekOrigin.Begin);
                wemDatas.Add(br.ReadBytes((int)lengths[i]));
            }
            for (int i = 0; i < wemCount; i++)
            {
                string name = "Imported Wem " + i;
                if (labels.wemLabels.ContainsKey(ids[i]))
                {
                    name = labels.wemLabels[ids[i]];
                }
                Wem newWem = new Wem(name, ids[i], wemDatas[i]);
                wemList.Add(newWem);
            }
            br.BaseStream.Seek(DataOff+dataLength,SeekOrigin.Begin);
        }
        public char[] dwTag { get => magic;}

        public uint dwChunkSize { get => (uint)didxSize; set => throw new NotImplementedException(); }//not using dwChunkSize for this one since we have a combo
        public void AddWem(string aName, uint aId, BinaryReader br)
        {
            Wem newWem = new Wem(aName, Convert.ToString(aId), br);
            wemList.Add(newWem);
        }
        public void Export(BinaryWriter bw)
        {
            bw.Write(dwTag);
            bw.Write(didxSize);
            uint currentOffset = 0;
            for (int i = 0; i < pLoadedMedia; i++)
            {
                bw.Write(wemList[i].id);
                bw.Write(currentOffset);
                bw.Write(wemList[i].length);
                currentOffset += wemList[i].length;
            }
            bw.Write(DATA);
            bw.Write(currentOffset);
            for (int i = 0; i < pLoadedMedia; i++)
            {
                bw.Write(wemList[i].file);
            }
        }
    }
}
