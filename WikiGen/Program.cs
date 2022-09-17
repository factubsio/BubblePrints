using BlueprintExplorer;
using FontTested;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WikiGen.Assets;

namespace WikiGen
{
    public class BlueprintReferencedAssets
    {
        public List<BlueprintAssetReference> m_Entries { get; set; } = new();
    }
    public class BlueprintAssetReference
    {
        public string AssetId { get; set; }
        public long FileId { get; set; }
        public UnityAssetReference Asset { get; set; }
    }
    public class UnityAssetReference
    {
        public int m_FileID { get; set; }
        public long m_PathID { get; set; }
    }


    public static class Program
    {
        public static string GetAbilityAcronym(string name)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name, i))
                {
                    stringBuilder.Append(name[i]);
                    stringBuilder.Append(" ");
                }
            }
            string text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(stringBuilder.ToString(), "[^0-9A-Za-z ,]", "").ToLowerInvariant());
            text = new string((from s in text.Split(new char[]
            {
                ' '
            }, StringSplitOptions.RemoveEmptyEntries) select s[0]).ToArray<char>());
            if (text.Length > 3)
            {
                text = text.Remove(2);
            }
            return text;
        }

        public static void Main(string[] args)
        {
            BubblePrints.Install();
            Console.WriteLine("Here");

            var references = JsonSerializer.Deserialize<BlueprintReferencedAssets>(File.ReadAllText(@"D:\wrath-data-raw\MonoBehaviour\BlueprintReferencedAssets.json"));
            Dictionary<string, UnityAssetReference> refToAsset = new();

            foreach (var entry in references.m_Entries)
                refToAsset[entry.AssetId] = entry.Asset;

            AssetContext context = new();
            context.AddDirectory(@"D:\steamlib\steamapps\common\Pathfinder Second Adventure\Bundles\");
            //context.AddBundle(@"D:\steamlib\steamapps\common\Pathfinder Second Adventure\Bundles\blueprint.assets", true);

            Console.WriteLine(string.Join(", ", context.assetsByBundle["blueprint.assets"]));

            TextureDecoder.ForceLoaded();

            //foreach (var assets in assetIndex.Values)
            //{
            //    int id = 1;

            //    foreach (var ext in assets.Externals)
            //    {
            //        //if (cabToBundle.TryGetValue(ext.fileName, out var bundle))
            //        //{
            //        //    Console.WriteLine($"resolved depdendency [{id}] {ext.fileName} -> {bundle}");
            //        //}
            //        //else
            //        //{
            //        //    Console.WriteLine($" *** error: could not resolve depdendency [{id}] {ext.fileName}");
            //        //}

            //        id++;
            //    }

            //    Console.WriteLine("target: " + assets.TargetPlatform);

            //    foreach (var info in assets.ObjectIndex)
            //    {

            //        //Console.WriteLine("object of type: " + objType);

            //        if (assets.TargetPlatform == BuildTarget.NoTarget)
            //            objReader.ReadUInt32();

            //        //if (objType == ClassIDType.MonoBehaviour)
            //        //{
            //        //    var gameObj = objReader.ReadPtr();
            //        //    byte enabled = objReader.ReadByte();
            //        //    objReader.AlignStream();
            //        //    var scriptRef = objReader.ReadPtr();
            //        //    var name = objReader.ReadAlignedString();

            //        //    if (name == "BlueprintReferencedAssets")
            //        //    {
            //        //        Console.WriteLine("Found blueprint referenced assets");
            //        //        objReader.Reset();
            //        //        Console.WriteLine(TreeDumper.ReadTypeString(info.serializedType.TypeTree, objReader));
            //        //    }

            //        //}

            //    }
            //}

            var progress = new BlueprintDB.ConnectionProgress();
            var load = Task.Run(() => BlueprintDB.Instance.TryConnect(progress));

            var print = Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine(progress.Status);
                    if (load.IsCompleted)
                        return;

                    Thread.Sleep(200);
                }
            });

            print.Wait();

            var bpByType = BlueprintDB.Instance.Blueprints.ToLookup(x => x.Value.Type);

            var target = @"D:\wrath-wiki\wwwroot\wrath-data";
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            //static (string typeName, BlueprintHandle typeBp) GetWeaponType(KeyValuePair<Guid, BlueprintHandle> x)
            //{
            //    var typeObj = x.Value.EnsureObj.Find("m_Type");
            //    var typeBp = typeObj.Resolve();
            //    if (typeBp is null)
            //        return ("__error", null);

            //    if (typeBp.EnsureObj.True("m_IsNatural"))
            //        return ("__skip", null);
            //    if (typeBp.EnsureObj.True("m_IsUnarmed"))
            //        return ("__skip", null);

            //    if (!WikiFilters.AllowedWeaponTypes.Contains(typeBp.Name))
            //    {
            //        Console.WriteLine($"Skipping: {x.Value.Name} of banned type {typeBp.Name}");
            //        return ("__skip", null);
            //    }


            //    return (typeBp.Name, typeBp);
            //}


            //foreach (var weaponType in bpByType["Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon"].GroupBy(GetWeaponType))
            //{
            //    if (weaponType.Key.typeBp == null) continue;

            //    using var writer = File.CreateText($"{target}/weapons.{weaponType.Key.typeName}.json");
            //    writer.WriteLine("[");

            //    bool first = true;

            //    foreach (var weapon in weaponType)
            //    {
            //        if (!first)
            //            writer.WriteLine(",");
            //        first = false;

            //        TextExporter.Export(writer, weapon.Value);
            //    }

            //    writer.WriteLine("]");

            //}

            HashSet<string> iconRequests = new();


            foreach (var clazz in bpByType["Kingmaker.Blueprints.Classes.BlueprintCharacterClass"])
            {
                Dictionary<Guid, LocalReference> features = new();
                List<BlueprintHandle> allFeatures = new();

                LocalReference AddFeature(BlueprintHandle bp)
                {
                    var key = Guid.Parse(bp.GuidText);
                    if (!features.TryGetValue(key, out var localRef))
                    {
                        localRef = new(bp, features.Count);
                        features.Add(key, localRef);
                        allFeatures.Add(bp);

                        var iconGuid = bp.obj.Find("m_Icon", "guid");

                        string iconRequest = null;

                        if (iconGuid.ValueKind != JsonValueKind.Null)
                            iconRequest = iconGuid.GetString();
                        else
                            iconRequest = "gen__" + GetAbilityAcronym(bp.EnsureObj.Find("m_DisplayName").ParseAsString());

                        iconRequests.Add(iconRequest);
                        localRef.icon = iconRequest;
                    }
                    return localRef;
                }

                int LookupFeature(BlueprintHandle bp)
                {
                    var key = Guid.Parse(bp.GuidText);
                    if (!features.TryGetValue(key, out var localRef))
                        return -1;
                    return localRef.index;
                }

                using var outStream = File.Create($"{target}/class.{clazz.Value.Name}.json");
                var obj = clazz.Value.EnsureObj;
                var progression = obj.Resolve("m_Progression");

                var prog = new ClassProgression();

                var levelEntries = progression.EnsureObj.Find("LevelEntries");
                foreach (var levelEntry in levelEntries.EnumerateArray())
                {
                    int level = levelEntry.Int("Level");
                    foreach (var feature in levelEntry.Find("m_Features").EnumerateArray())
                    {
                        var featureBp = feature.Resolve();
                        if (featureBp.EnsureObj.True("HideInUI")) continue;
                        var reference = AddFeature(featureBp);
                        prog.FeatureByLevel[level].Add(reference);
                    }
                }

                foreach (var uiGroupJson in progression.obj.Find("UIGroups").EnumerateArray())
                {
                    var set = new UIGroup();
                    foreach (var f in uiGroupJson.Find("m_Features").EnumerateArray())
                    {
                        int index = LookupFeature(f.Resolve());
                        if (index != -1)
                            set.contains.Add(index);

                        if (set.Id == -1)
                            set.Id = index;
                    }
                    prog.UIGroups.Add(set);
                }

                for (int level = 1; level <= 20; level++)
                {
                    foreach (var feature in prog.FeatureByLevel[level])
                    {
                        int row = prog.LookupGroup(feature.index);
                        if (!prog.LookupRow(row).Set(level, feature))
                        {
                            LevelRow overflow = new();
                            prog.Rows[prog.FakeRow++] = overflow;
                            overflow.Set(level, feature);
                        }
                    }
                }

                using var jsonWriter = new Utf8JsonWriter(outStream, new()
                {
                    Indented = true,
                });

                jsonWriter.WriteStartObject();

                jsonWriter.WriteStartArray("features");
                foreach (var featureRef in features.Values.OrderBy(x => x.index))
                {
                    var feature = featureRef.handle;
                    if (feature.EnsureObj.True("HideInUI")) continue;

                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteString("name", feature.EnsureObj.Find("m_DisplayName").ParseAsString());
                    jsonWriter.WriteString("desc", feature.EnsureObj.Find("m_Description").ParseAsString());
                    jsonWriter.WriteString("icon", featureRef.icon);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteStartArray("progression");
                foreach (var entry in prog.FeatureByLevel)
                {
                    jsonWriter.WriteStartArray();
                    foreach (var f in entry)
                    {
                        jsonWriter.WriteNumberValue(f.index);
                    }
                    jsonWriter.WriteEndArray();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteStartArray("__ui-groups");
                foreach (var group in prog.UIGroups)
                {
                    jsonWriter.WriteStartArray();
                    foreach (var f in group.contains)
                    {
                        jsonWriter.WriteNumberValue(f);
                    }
                    jsonWriter.WriteEndArray();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteString("name", clazz.Value.obj.Find("LocalizedName").ParseAsString());

                jsonWriter.WriteStartArray("layout");
                foreach (var group in prog.Rows.Values)
                {
                    jsonWriter.WriteStartArray();
                    for (int l = 1; l <= 20; l++)
                    {
                        if (group.FeatureByLevel[l] != null)
                        {
                            jsonWriter.WriteStartObject();
                            jsonWriter.WriteNumber("level", l);
                            jsonWriter.WriteNumber("feature", group.FeatureByLevel[l].index);
                            jsonWriter.WriteNumber("rank", group.FeatureByLevel[l].rank);
                            jsonWriter.WriteString("__debug", allFeatures[group.FeatureByLevel[l].index].Name);
                            jsonWriter.WriteEndObject();
                        }
                    }
                    jsonWriter.WriteEndArray();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndObject();
            }

            var blueprintAssets = context.assetsByBundle["blueprint.assets"][0];

            using var saber = new SaberRenderer((Bitmap)Bitmap.FromFile(@"D:\font_atlas.png"), File.ReadAllLines(@"D:\font_atlas.txt"));

            int fromName = 0;
            int iconNotFound = 0;
            int iconFound = 0;

            var placeholderBackground = Bitmap.FromFile(@"D:\wrath-data-raw\Sprite\UI_PlaceHolderIcon7.png");

            foreach (var request in iconRequests)
            {
                if (request.StartsWith("gen__"))
                {
                    Bitmap buffer = new(64, 64, PixelFormat.Format32bppArgb);
                    TextureBrush brush = new(placeholderBackground);

                    using (var g = Graphics.FromImage(buffer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillEllipse(brush, new(Point.Empty, buffer.Size));
                    }

                    saber.Render(request[5..], buffer, new()
                    {
                        Thickness = 0.5f,
                        Softness = 0.6f,

                        OutlineThickness = 0.5f,
                        OutlineSoftness = 0.0f,

                        Color = new(0.8f, 0.7f, 0.5f),

                        TextScale = 0.5f,
                        LetterSpacing = -9,

                        Clear = false,

                    });


                    buffer.Save($@"D:\wrath-wiki\wwwroot\icons\{request}.png");
                    fromName++;
                }
                else
                {
                    if (!refToAsset.TryGetValue(request, out var assetRef))
                    {
                        Console.WriteLine("Could not find asset...");
                        iconNotFound++;
                        continue;
                    }

                    if (assetRef.m_FileID == 0)
                    {
                        var spriteInfo = blueprintAssets.Resolve(assetRef.m_PathID);
                        EmitSprite(request, spriteInfo);
                    }
                    iconFound++;
                }
            }

            Console.WriteLine($"{iconRequests.Count} requests for icons, {fromName} generated from name, {iconFound} icons found, {iconNotFound} icons NOT found");
        }

        private static void EmitSprite(string id, ObjectInfo spriteInfo)
        {
            if (spriteInfo.ClassType != ClassIDType.Sprite)
                throw new Exception("not a sprite");
            var objReader = new VersionedReader(spriteInfo);
            var sprite = new Sprite(objReader);

            if (sprite.m_RD.texture.Identifier == 0)
                throw new Exception("No texture");

            if (sprite.m_RD.texture.FileIndex != 0)
                throw new Exception("texture is in a different bundle");

            var texInfo = spriteInfo.Owner.Resolve(sprite.m_RD.texture.Identifier);

            if (texInfo.ClassType != ClassIDType.Texture2D)
                throw new Exception("texture is not Texture2D");

            var texReader = new VersionedReader(texInfo);
            var tex = new Texture2D(texReader);

            VersionedReader resourceStream;

            if (tex.m_StreamData?.path != null)
            {
                resourceStream = new(
                    version: texInfo.Owner.Version,
                    target: texInfo.Owner.TargetPlatform,
                    stream: texInfo.Owner.Context.resourceStreams[Path.GetFileName(tex.m_StreamData.path)].Based(tex.m_StreamData.offset, tex.m_StreamData.size),
                    endian: EndianType.Little);
            }
            else
            {
                resourceStream = new(
                    version: texInfo.Owner.Version,
                    target: texInfo.Owner.TargetPlatform,
                    stream: texInfo.Owner.blockStream.Based(texInfo.Owner.blockStream.Position, tex.image_data_size),
                    endian: EndianType.Little);
            }

            var converter = new Texture2DConverter(resourceStream, tex);
            var buff = BigArrayPool<byte>.Shared.Rent(tex.m_Width * tex.m_Height * 4);
            try
            {
                if (converter.DecodeTexture2D(buff))
                {
                    const PixelFormat format = PixelFormat.Format32bppArgb;
                    var bmp = new Bitmap(tex.m_Width, tex.m_Height, format);
                    Rectangle rect = new(0, 0, bmp.Width, bmp.Height);

                    var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);
                    IntPtr ptr = data.Scan0;
                    Marshal.Copy(buff, 0, ptr, data.Stride * data.Height);
                    bmp.UnlockBits(data);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    Bitmap dest = new(tex.m_Width, tex.m_Height, format);
                    TextureBrush brush = new(bmp);

                    using (var g = Graphics.FromImage(dest))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillEllipse(brush, rect);
                    }


                    dest.Save($@"D:\wrath-wiki\wwwroot\icons\{id}.png");
                }
            }
            finally
            {
                BigArrayPool<byte>.Shared.Return(buff);
            }
        }
    }


    public class FileIdentifier
    {
        public Guid guid;
        public int type; //enum { kNonAssetType = 0, kDeprecatedCachedAssetType = 1, kSerializedAssetType = 2, kMetaAssetType = 3 };
        public string pathName;

        //custom
        public string fileName;

        public AssetFile Resolved;
    }

    public class LocalSerializedObjectIdentifier
    {
        public int FileIndex;
        public long Identifier;

        public override string ToString() => $"file:{FileIndex} id:{Identifier}";

    }

    public class TypeTreeNode
    {
        public string Type;
        public string Name;
        public int ByteSize;
        public int Index;
        public int TypeFlags; //IsArray
        public int Version;
        public int MetaFlag;
        public int Level;
        public uint TypeStrOffset;
        public uint NameStrOffset;
        public ulong RefTypeHash;
    }

    public enum AssVer
    {
        Unsupported = 1,
        Unknown_2 = 2,
        Unknown_3 = 3,
        /// <summary>
        /// 1.2.0 to 2.0.0
        /// </summary>
        Unknown_5 = 5,
        /// <summary>
        /// 2.1.0 to 2.6.1
        /// </summary>
        Unknown_6 = 6,
        /// <summary>
        /// 3.0.0b
        /// </summary>
        Unknown_7 = 7,
        /// <summary>
        /// 3.0.0 to 3.4.2
        /// </summary>
        Unknown_8 = 8,
        /// <summary>
        /// 3.5.0 to 4.7.2
        /// </summary>
        Unknown_9 = 9,
        /// <summary>
        /// 5.0.0aunk1
        /// </summary>
        Unknown_10 = 10,
        /// <summary>
        /// 5.0.0aunk2
        /// </summary>
        HasScriptTypeIndex = 11,
        /// <summary>
        /// 5.0.0aunk3
        /// </summary>
        Unknown_12 = 12,
        /// <summary>
        /// 5.0.0aunk4
        /// </summary>
        HasTypeTreeHashes = 13,
        /// <summary>
        /// 5.0.0unk
        /// </summary>
        Unknown_14 = 14,
        /// <summary>
        /// 5.0.1 to 5.4.0
        /// </summary>
        SupportsStrippedObject = 15,
        /// <summary>
        /// 5.5.0a
        /// </summary>
        RefactoredClassId = 16,
        /// <summary>
        /// 5.5.0unk to 2018.4
        /// </summary>
        RefactorTypeData = 17,
        /// <summary>
        /// 2019.1a
        /// </summary>
        RefactorShareableTypeTreeData = 18,
        /// <summary>
        /// 2019.1unk
        /// </summary>
        TypeTreeNodeWithTypeFlags = 19,
        /// <summary>
        /// 2019.2
        /// </summary>
        SupportsRefObject = 20,
        /// <summary>
        /// 2019.3 to 2019.4
        /// </summary>
        StoresTypeDependencies = 21,
        /// <summary>
        /// 2020.1 to x
        /// </summary>
        LargeFilesSupport = 22
    }

    public class BasedStream : Stream
    {
        public readonly Stream BaseStream;
        public readonly long BaseOffset;
        public readonly long _Length;

        public BasedStream(Stream baseStream, long offset, long length)
        {
            BaseStream = baseStream;
            BaseOffset = offset;
            _Length = length;
            Position = 0;
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => _Length;

        public override long Position
        {
            get => BaseStream.Position - BaseOffset;
            set => BaseStream.Position = value + BaseOffset;
        }

        public override void Flush() => BaseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }
        public override void SetLength(long value) => BaseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);
    }

    public class LocalReference
    {
        public readonly BlueprintHandle handle;
        public readonly int index;
        public string icon = "";

        public LocalReference(BlueprintHandle bp, int index)
        {
            this.handle = bp;
            this.index = index;
        }

        internal void Deconstruct(out BlueprintHandle feature, out int index)
        {
            index = this.index;
            feature = this.handle;
        }
    }

    public class UIGroup
    {
        public int Id = -1;
        public HashSet<int> contains = new();

        public bool Contains(int feature) => contains.Contains(feature);
    }

    public class FeatureEntry
    {
        public int index = -1;
        public int rank = -1;
    }

    public class LevelRow
    {
        public FeatureEntry[] FeatureByLevel = new FeatureEntry[21];

        public bool Set(int level, LocalReference feature)
        {
            if (FeatureByLevel[level] != null)
                return false;

            int rank = -1;
            if (feature.handle.obj.Int("Ranks") > 1)
            {
                rank = 1;
                var prev = FeatureByLevel.LastOrDefault(x => x?.index == feature.index);
                if (prev != null)
                    rank = prev.rank + 1;
            }

            FeatureByLevel[level] = new()
            {
                index = feature.index,
                rank = rank
            };
            return true;
        }
    }

    public class ClassProgression
    {
        public List<LocalReference>[] FeatureByLevel = new List<LocalReference>[21];

        public List<UIGroup> UIGroups = new();

        public Dictionary<int, LevelRow> Rows = new();
        public int FakeRow = -100000;

        public int LookupGroup(int feature)
        {
            return UIGroups.FirstOrDefault(set => set.Contains(feature))?.Id ?? feature;
        }

        public LevelRow LookupRow(int row)
        {
            if (!Rows.TryGetValue(row, out var levelRow))
            {
                levelRow = new();
                Rows[row] = levelRow;
            }
            return levelRow;
        }

        public ClassProgression()
        {
            for (int i = 0; i < FeatureByLevel.Length; i++)
            {
                FeatureByLevel[i] = new();
            }
        }
    }
}
