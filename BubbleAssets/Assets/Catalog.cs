using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BubbleAssets.Assets
{
    public class Catalog
    {
        [JsonInclude]
        public string m_BucketDataString;
        [JsonInclude]
        public string[] m_ProviderIds = Array.Empty<string>();

        [JsonInclude]
		public string[] m_InternalIds;

        [JsonInclude]
		public string m_KeyDataString;

        [JsonInclude]
		public string m_EntryDataString;
        [JsonInclude]
		public string m_ExtraDataString;

        [JsonInclude]
		public string m_LocatorId;


        public static Catalog FromJson(string path)
        {
            var text = File.ReadAllText(path);
            var catalog = JsonSerializer.Deserialize<Catalog>(text);
            return catalog;
        }

		public Dictionary<object, CompactLocation[]> CreateLocator(string? providerSuffix = null)
		{
			byte[] array = Convert.FromBase64String(this.m_BucketDataString);
			int num = BitConverter.ToInt32(array, 0);
			Bucket[] allBuckets = new Bucket[num];
			int rd = 4;
			for (int i = 0; i < num; i++)
			{
				int dataOffset = SerializationUtilities.ReadInt32FromByteArray(array, rd);
				rd += 4;
				int entryCount = SerializationUtilities.ReadInt32FromByteArray(array, rd);
				rd += 4;
				int[] entries = new int[entryCount];
				for (int j = 0; j < entryCount; j++)
				{
					entries[j] = SerializationUtilities.ReadInt32FromByteArray(array, rd);
					rd += 4;
				}
				allBuckets[i] = new Bucket
				{
					entries = entries,
					dataOffset = dataOffset
				};
			}
			if (!string.IsNullOrEmpty(providerSuffix))
			{
				for (int k = 0; k < this.m_ProviderIds.Length; k++)
				{
					if (!this.m_ProviderIds[k].EndsWith(providerSuffix, StringComparison.Ordinal))
					{
						this.m_ProviderIds[k] = this.m_ProviderIds[k] + providerSuffix;
					}
				}
			}
			byte[] extraData = Convert.FromBase64String(this.m_ExtraDataString);
			byte[] keyData = Convert.FromBase64String(this.m_KeyDataString);
			object[] keys = new object[BitConverter.ToInt32(keyData, 0)];
			for (int l = 0; l < allBuckets.Length; l++)
			{
				keys[l] = SerializationUtilities.ReadObjectFromByteArray(keyData, allBuckets[l].dataOffset);
			}
            Dictionary<object, CompactLocation[]> resourceLocationMap = new(); // this.m_LocatorId, allBuckets.Length);
			byte[] data = Convert.FromBase64String(this.m_EntryDataString);
			int locationCount = SerializationUtilities.ReadInt32FromByteArray(data, 0);
			CompactLocation[] allLocations = new CompactLocation[locationCount];
			for (int resIndex = 0; resIndex < locationCount; resIndex++)
			{
				int num5 = 4 + resIndex * 28;
				int num6 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int num7 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int num8 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int depHash = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int num9 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int num10 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				num5 += 4;
				int num11 = SerializationUtilities.ReadInt32FromByteArray(data, num5);
				object data2 = (num9 < 0) ? null : SerializationUtilities.ReadObjectFromByteArray(extraData, num9);
                //var publicId = Addressables.ResolveInternalId(ContentCatalogData.ExpandInternalId(this.m_InternalIdPrefixes, this.m_InternalIds[num6]))
                //var type = this.m_resourceTypes[num11].Value
                allLocations[resIndex] = new CompactLocation(resourceLocationMap, m_InternalIds[num6], this.m_ProviderIds[num7], (num8 < 0) ? null : keys[num8], data2, depHash, keys[num10].ToString(), typeof(string));
            }
            for (int bucketIndex = 0; bucketIndex < allBuckets.Length; bucketIndex++)
			{
				Bucket bucket = allBuckets[bucketIndex];
				object key = keys[bucketIndex];
				CompactLocation[] bucketLocations = new CompactLocation[bucket.entries.Length];
				for (int entryIndex = 0; entryIndex < bucket.entries.Length; entryIndex++)
				{
					bucketLocations[entryIndex] = allLocations[bucket.entries[entryIndex]];
				}
				resourceLocationMap.Add(key, bucketLocations);
			}
			return resourceLocationMap;
		}

		private struct Bucket
		{
			// Token: 0x040000EF RID: 239
			public int dataOffset;

			// Token: 0x040000F0 RID: 240
			public int[] entries;
		}
    }

    public record class CompactLocation(object locator,
                                        string publicId,
                                        string providerId,
                                        object dependencyKey,
                                        object data,
                                        int depHash,
                                        string primaryKey,
                                        Type type);

    public static class SerializationUtilities
	{
		// Token: 0x060001C2 RID: 450 RVA: 0x00007055 File Offset: 0x00005255
		public static int ReadInt32FromByteArray(byte[] data, int offset)
		{
			return (int)data[offset] | (int)data[offset + 1] << 8 | (int)data[offset + 2] << 16 | (int)data[offset + 3] << 24;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00007074 File Offset: 0x00005274
		public static int WriteInt32ToByteArray(byte[] data, int val, int offset)
		{
			data[offset] = (byte)(val & 255);
			data[offset + 1] = (byte)(val >> 8 & 255);
			data[offset + 2] = (byte)(val >> 16 & 255);
			data[offset + 3] = (byte)(val >> 24 & 255);
			return offset + 4;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x000070B4 File Offset: 0x000052B4
		public static object ReadObjectFromByteArray(byte[] keyData, int dataIndex)
        {
            SerializationUtilities.ObjectType objectType = (SerializationUtilities.ObjectType)keyData[dataIndex];
            dataIndex++;
            switch (objectType)
            {
                case SerializationUtilities.ObjectType.AsciiString:
                    {
                        int count = BitConverter.ToInt32(keyData, dataIndex);
                        return Encoding.ASCII.GetString(keyData, dataIndex + 4, count);
                    }
                case SerializationUtilities.ObjectType.UnicodeString:
                    {
                        int count2 = BitConverter.ToInt32(keyData, dataIndex);
                        return Encoding.Unicode.GetString(keyData, dataIndex + 4, count2);
                    }
                case SerializationUtilities.ObjectType.UInt16:
                    return BitConverter.ToUInt16(keyData, dataIndex);
                case SerializationUtilities.ObjectType.UInt32:
                    return BitConverter.ToUInt32(keyData, dataIndex);
                case SerializationUtilities.ObjectType.Int32:
                    return BitConverter.ToInt32(keyData, dataIndex);
                case SerializationUtilities.ObjectType.Hash128:
                    throw new NotSupportedException();
                //return Hash128.Parse(Encoding.ASCII.GetString(keyData, dataIndex + 1, (int)keyData[dataIndex]));
                case SerializationUtilities.ObjectType.Type:
                    return Type.GetTypeFromCLSID(new Guid(Encoding.ASCII.GetString(keyData, dataIndex + 1, (int)keyData[dataIndex])));
                case SerializationUtilities.ObjectType.JsonObject:
                    {
                        int num = (int)keyData[dataIndex];
                        dataIndex++;
                        string @string = Encoding.ASCII.GetString(keyData, dataIndex, num);
                        dataIndex += num;
                        int num2 = (int)keyData[dataIndex];
                        dataIndex++;
                        string string2 = Encoding.ASCII.GetString(keyData, dataIndex, num2);
                        dataIndex += num2;
                        int count3 = BitConverter.ToInt32(keyData, dataIndex);
                        dataIndex += 4;
                        string string3 = Encoding.Unicode.GetString(keyData, dataIndex, count3);
                        Type type = Assembly.Load(@string).GetType(string2);
                        return (string3, type);
                        //return JsonUtility.FromJson(string3, type);
                    }
            }
            return null;
        }

        // Token: 0x060001C5 RID: 453 RVA: 0x00007248 File Offset: 0x00005448
        //public static int WriteObjectToByteList(object obj, List<byte> buffer)
        //{
        //	Type type = obj.GetType();
        //	if (type == typeof(string))
        //	{
        //		string text = obj as string;
        //		if (text == null)
        //		{
        //			text = string.Empty;
        //		}
        //		byte[] bytes = Encoding.Unicode.GetBytes(text);
        //		byte[] bytes2 = Encoding.ASCII.GetBytes(text);
        //		if (Encoding.Unicode.GetString(bytes) == Encoding.ASCII.GetString(bytes2))
        //		{
        //			buffer.Add(0);
        //			buffer.AddRange(BitConverter.GetBytes(bytes2.Length));
        //			buffer.AddRange(bytes2);
        //			return bytes2.Length + 5;
        //		}
        //		buffer.Add(1);
        //		buffer.AddRange(BitConverter.GetBytes(bytes.Length));
        //		buffer.AddRange(bytes);
        //		return bytes.Length + 5;
        //	}
        //	else
        //	{
        //		if (type == typeof(uint))
        //		{
        //			byte[] bytes3 = BitConverter.GetBytes((uint)obj);
        //			buffer.Add(3);
        //			buffer.AddRange(bytes3);
        //			return bytes3.Length + 1;
        //		}
        //		if (type == typeof(ushort))
        //		{
        //			byte[] bytes4 = BitConverter.GetBytes((ushort)obj);
        //			buffer.Add(2);
        //			buffer.AddRange(bytes4);
        //			return bytes4.Length + 1;
        //		}
        //		if (type == typeof(int))
        //		{
        //			byte[] bytes5 = BitConverter.GetBytes((int)obj);
        //			buffer.Add(4);
        //			buffer.AddRange(bytes5);
        //			return bytes5.Length + 1;
        //		}
        //		if (type == typeof(int))
        //		{
        //			byte[] bytes6 = BitConverter.GetBytes((uint)obj);
        //			buffer.Add(3);
        //			buffer.AddRange(bytes6);
        //			return bytes6.Length + 1;
        //		}
        //		if (type == typeof(Hash128))
        //		{
        //			Hash128 hash = (Hash128)obj;
        //			byte[] bytes7 = Encoding.ASCII.GetBytes(hash.ToString());
        //			buffer.Add(5);
        //			buffer.Add((byte)bytes7.Length);
        //			buffer.AddRange(bytes7);
        //			return bytes7.Length + 2;
        //		}
        //		if (type == typeof(Type))
        //		{
        //			byte[] array = type.GUID.ToByteArray();
        //			buffer.Add(6);
        //			buffer.Add((byte)array.Length);
        //			buffer.AddRange(array);
        //			return array.Length + 2;
        //		}
        //		if (type.GetCustomAttributes(typeof(SerializableAttribute), true).Length == 0)
        //		{
        //			return 0;
        //		}
        //		int num = 0;
        //		buffer.Add(7);
        //		int num2 = num + 1;
        //		byte[] bytes8 = Encoding.ASCII.GetBytes(type.Assembly.FullName);
        //		buffer.Add((byte)bytes8.Length);
        //		int num3 = num2 + 1;
        //		buffer.AddRange(bytes8);
        //		int num4 = num3 + bytes8.Length;
        //		string text2 = type.FullName;
        //		if (text2 == null)
        //		{
        //			text2 = string.Empty;
        //		}
        //		byte[] bytes9 = Encoding.ASCII.GetBytes(text2);
        //		buffer.Add((byte)bytes9.Length);
        //		int num5 = num4 + 1;
        //		buffer.AddRange(bytes9);
        //		int num6 = num5 + bytes9.Length;
        //		byte[] bytes10 = Encoding.Unicode.GetBytes(JsonUtility.ToJson(obj));
        //		buffer.AddRange(BitConverter.GetBytes(bytes10.Length));
        //		int num7 = num6 + 4;
        //		buffer.AddRange(bytes10);
        //		return num7 + bytes10.Length;
        //	}
        //}

        // Token: 0x0200003E RID: 62
        public enum ObjectType
		{
			// Token: 0x040000B5 RID: 181
			AsciiString,
			// Token: 0x040000B6 RID: 182
			UnicodeString,
			// Token: 0x040000B7 RID: 183
			UInt16,
			// Token: 0x040000B8 RID: 184
			UInt32,
			// Token: 0x040000B9 RID: 185
			Int32,
			// Token: 0x040000BA RID: 186
			Hash128,
			// Token: 0x040000BB RID: 187
			Type,
			// Token: 0x040000BC RID: 188
			JsonObject
		}
	}
}
