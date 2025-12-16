using BlueprintExplorer;
using BubbleAssets;
using FontTested;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using WikiGen.Assets;

namespace WikiGen
{

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
            borderImage = Bitmap.FromFile(@"D:\wrath-data-raw\Sprite\circle_border.png");

            TextureDecoder.ForceLoaded();

            BlueprintAssetsContext blueprintContext = new(@"D:\wrath-data-raw\MonoBehaviour\BlueprintReferencedAssets.json");

            AssetContext assets = new();
            assets.AddDirectory(@"D:\steamlib\steamapps\common\Pathfinder Second Adventure\Bundles\");

            var blueprintAssets = assets.assetsByBundle["blueprint.assets"][0];

            if (blueprintContext.TryGetValue("cc0dee2c595dea14d8058008d0b5e441", out var wolfieAsset)) {
                var ptrToSprite = new PPtr<Sprite>(wolfieAsset, blueprintAssets);
                if (blueprintContext.TryRenderSprite(ptrToSprite.Object, out var image))
                {
                    image.Save(@"D:\wolfie.png");
                }
            }


            var progress = new ConnectionProgress();
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

            HashSet<IconRequest> iconRequests = new();

            var bpRoot = BlueprintDB.Instance.Blueprints[Guid.Parse("2d77316c72b9ed44f888ceefc2a131f6")];

            var classList = new List<String>();


            List<BlueprintHandle> classes = bpRoot.EnsureObj.Find("Progression", "m_CharacterClasses").EnumerateArray().Select(x => x.DeRef()).ToList();
            classes.Add(BlueprintDB.Instance.Blueprints[Guid.Parse("a406d6ebea5c46bba3160246be03e96f")]);

            foreach (var clazzBp in classes)
            {
                Dictionary<string, LocalReference> featuresByGuid = new();
                List<LocalReference> features = new();


                LocalReference AddFeature(BlueprintHandle bp, BlueprintHandle parent = null)
                {
                    string key;
                    if (parent != null)
                        key = $"{bp.GuidText}/{parent.GuidText}";
                    else
                        key = bp.GuidText;

                    if (bp.EnsureObj.True("HideInUI"))
                    {
                        throw new Exception();
                    }


                    if (!featuresByGuid.TryGetValue(key, out var localRef))
                    {
                        localRef = new(bp, featuresByGuid.Count);
                        featuresByGuid.Add(key, localRef);
                        features.Add(localRef);

                        var locName = bp.EnsureObj.Find("m_DisplayName").ParseAsString();

                        if (bp.TypeName == "BlueprintProgression")
                        {
                            localRef.SubProgression = new();
                            var levelEntries = bp.EnsureObj.Find("LevelEntries");
                            foreach (var levelEntry in levelEntries.EnumerateArray())
                            {
                                int level = levelEntry.Int("Level");
                                foreach (var feature in levelEntry.Find("m_Features").EnumerateArray())
                                {
                                    var featureBp = feature.DeRef();
                                    if (featureBp.EnsureObj.True("HideInUI")) continue;
                                    var reference = AddFeature(featureBp, bp);
                                    reference.ProgressionGroup = locName;
                                    localRef.SubProgression.Add((reference, level));
                                }
                            }
                            localRef.ProgressionGroup = locName;
                        }

                        if (bp.TypeName == "BlueprintFeatureSelection")
                        {
                            localRef.IsSelection = true;

                            //if (bp.Name == "OppositionSchoolSelection")
                            //{
                            //    Debugger.Break();
                            //}

                            if (bp.EnsureObj.Find("m_AllFeatures").EnumerateArray().All(fRaw => fRaw.DeRef().TypeName == "BlueprintProgression"))
                            {
                                localRef.ProgressionGroup = locName;
                            }
                        }


                        var iconGuid = bp.obj.Find("m_Icon", "guid");

                        string iconRequest = null;


                        if (!iconGuid.Nullish())
                            iconRequest = iconGuid.GetString();
                        else
                            iconRequest = "gen__" + GetAbilityAcronym(bp.EnsureObj.Find("m_DisplayName").ParseAsString());

                        //Console.WriteLine($"requesting icon: {bp.Name} -> {iconRequest}");

                        iconRequests.Add((iconRequest, localRef));
                        localRef.icon = iconRequest;
                    }
                    return localRef;
                }

                int LookupFeature(BlueprintHandle bp)
                {
                    if (!featuresByGuid.TryGetValue(bp.GuidText, out var localRef))
                        return -1;
                    return localRef.index;
                }

                //if (clazzBp.Name == "MonkClass")
                //{
                //    Debugger.Break();
                //}


                using var outStream = File.Create($"{target}/class.{clazzBp.Name}.json");
                var obj = clazzBp.EnsureObj;

                if (!obj.TryDeRef(out var progression, "m_Progression"))
                {
                    throw new Exception();
                }

                var prog = new ClassProgression();

                ExtractStatProgression(ref prog.Flags.bab, ref prog.Flags.babSpeed, obj.Find("m_BaseAttackBonus").DeRef());
                ExtractStatProgression(ref prog.Flags.fort, ref prog.Flags.fortSpeed, obj.Find("m_FortitudeSave").DeRef());
                ExtractStatProgression(ref prog.Flags.reflex, ref prog.Flags.reflexSpeed, obj.Find("m_ReflexSave").DeRef());
                ExtractStatProgression(ref prog.Flags.will, ref prog.Flags.willSpeed, obj.Find("m_WillSave").DeRef());

                prog.HitDie = int.Parse(obj.Str("HitDie")[1..]);

                if (obj.TryDeRef(out var spellsBp, "m_Spellbook"))
                {
                    ExtractSpellProgression(spellsBp,
                        ref prog.SpellsByLeveL, ref prog.CasterLevelModifier, ref prog.NewSpellsByLevel, ref prog.CasterAbility, ref prog.Spontaneous);
                    prog.CasterType = ExtractCasterType(obj);
                }

                // Get the base set of features for the class
                var levelEntries = progression.EnsureObj.Find("LevelEntries");
                foreach (var levelEntry in levelEntries.EnumerateArray())
                {
                    int level = levelEntry.Int("Level");
                    foreach (var feature in levelEntry.Find("m_Features").EnumerateArray())
                    {
                        var featureBp = feature.DeRef();

                        if (featureBp.EnsureObj.True("HideInUI")) continue;
                        var reference = AddFeature(featureBp);
                        if (reference.SubProgression == null)
                            prog.FeatureByLevel[level].Add(reference);
                        else
                        {
                            foreach (var (sub, subLevel) in reference.SubProgression)
                            {
                                prog.FeatureByLevel[subLevel].Add(sub);
                            }

                        }
                    }
                }

                // Parse the explicit ui groups (should also draw lines between them?)
                foreach (var uiGroupJson in progression.obj.Find("UIGroups").EnumerateArray())
                {
                    var set = new UIGroup();
                    foreach (var f in uiGroupJson.Find("m_Features").EnumerateArray())
                    {
                        var featureBp = f.DeRef();


                        if (featureBp.EnsureObj.True("HideInUI")) continue;

                        int index = AddFeature(featureBp).index;
                        if (index != -1)
                            set.contains.Add(index);

                        if (set.Id == -1)
                            set.Id = index;
                    }
                    prog.UIGroups.Add(set);
                }


                // Determinators go in the special left-column and ignore most row layout rules
                foreach (var determinatorFeature in progression.obj.Find("m_UIDeterminatorsGroup").EnumerateArray().Select(x => x.DeRef()))
                {
                    if (determinatorFeature.EnsureObj.True("HideInUI")) continue;

                    int index = AddFeature(determinatorFeature).index;
                    if (index != -1)
                        prog.uiDeterminators.Add(index);
                }

                foreach (var archRef in obj.Find("m_Archetypes").EnumerateArray())
                {
                    var archBp = archRef.DeRef();

                    ArchetypeProgression arch = new();

                    var archObj = archBp.EnsureObj;

                    arch.Name = archObj.Find("LocalizedName").ParseAsString();
                    arch.Desc = archObj.Find("LocalizedDescription").ParseAsString();
                    arch.Id = archBp.GuidText;
                    arch.bp = archBp;

                    if (archObj.TryDeRef(out var archSpellsBp, "m_ReplaceSpellbook"))
                    {
                        ExtractSpellProgression(archSpellsBp,
                            ref arch.SpellsByLeveL, ref arch.CasterLevelModifier, ref arch.NewSpellsByLevel, ref arch.CasterAbility, ref arch.Spontaneous);

                        if (archObj.True("ChangeCasterType"))
                            arch.CasterType = ExtractCasterType(archObj);
                    }

                    arch.removeSpells = archObj.True("RemoveSpellbook");


                    if (!archObj.Find("m_BaseAttackBonus").Nullish())
                        ExtractStatProgression(ref arch.Flags.bab, ref arch.Flags.babSpeed, archObj.Find("m_BaseAttackBonus").DeRef());
                    if (!archObj.Find("m_FortitudeSave").Nullish())
                        ExtractStatProgression(ref arch.Flags.fort, ref arch.Flags.fortSpeed, archObj.Find("m_FortitudeSave").DeRef());
                    if (!archObj.Find("m_ReflexSave").Nullish())
                        ExtractStatProgression(ref arch.Flags.reflex, ref arch.Flags.reflexSpeed, archObj.Find("m_ReflexSave").DeRef());
                    if (!archObj.Find("m_WillSave").Nullish())
                        ExtractStatProgression(ref arch.Flags.will, ref arch.Flags.willSpeed, archObj.Find("m_WillSave").DeRef());

                    foreach (var levelEntry in archBp.EnsureObj.Find("RemoveFeatures").EnumerateArray())
                    {
                        int level = levelEntry.Int("Level");

                        if (!arch.remove.TryGetValue(level, out var remove))
                        {
                            remove = new();
                            arch.remove[level] = remove;
                        }

                        foreach (var f in levelEntry.Find("m_Features").EnumerateArray())
                        {
                            remove.Add(LookupFeature(f.DeRef()));
                        }
                    }


                    foreach (var levelEntry in archBp.EnsureObj.Find("AddFeatures").EnumerateArray())
                    {
                        int level = levelEntry.Int("Level");
                        if (!arch.add.TryGetValue(level, out var add))
                        {
                            add = new();
                            arch.add[level] = add;
                        }

                        foreach (var f in levelEntry.Find("m_Features").EnumerateArray())
                        {
                            var featureBp = f.DeRef();
                            if (featureBp.EnsureObj.True("HideInUI")) continue;

                            var reference = AddFeature(featureBp);

                            if (reference.SubProgression == null)
                                add.Add(reference.index);
                            else
                            {
                                foreach (var (sub, subLevel) in reference.SubProgression)
                                {
                                    add.Add(sub.index);
                                }
                            }
                        }
                    }

                    prog.archetypes.Add(arch);
                }

                //                    if (clazzBp.Name == "BarbarianClass")
                //                    {
                //Debugger.Break();
                //                    }

                // Convert the features into a layout (a set of rows where each row can only have one feature per level)
                for (int level = 1; level <= 20; level++)
                {
                    foreach (var feature in prog.FeatureByLevel[level])
                    {
                        LayoutFeature(prog, prog, level, feature);
                    }
                }

                // Merge rows with only one entry into a previous row (that is not part of a ui group)
                MergeRows(features, prog.Rows.Values);

                AddDeterminators(features, source: prog, to: prog);

                foreach (var arch in prog.archetypes)
                {
                    //arch.Rows = prog.Rows.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
                    foreach (var addAtLevel in arch.add)
                    {
                        foreach (var f in addAtLevel.Value)
                        {
                            LayoutFeature(prog, arch, addAtLevel.Key, features[f], 1);
                        }
                    }
                    for (int level = 1; level <= 20; level++)
                    {
                        foreach (var feature in prog.FeatureByLevel[level])
                        {
                            LayoutFeature(prog, arch, level, feature);
                        }
                    }
                    MergeRows(features, arch.Rows.Values);
                    //AddDeterminators(allFeatureReferences, source: prog, to: arch);
                    AddDeterminators(features, source: arch, to: arch);
                    foreach (var removeAtLevel in arch.remove)
                    {
                        foreach (var f in removeAtLevel.Value)
                        {
                            arch.FindFeature(removeAtLevel.Key, f)?.SetAddRemove(-1);
                        }
                    }
                }


                using var jsonWriter = new Utf8JsonWriter(outStream, new()
                {
                    Indented = true,
                });

                jsonWriter.WriteStartObject();

                jsonWriter.WriteStartArray("__ui-groups");
                foreach (var uiGroup in prog.UIGroups)
                {
                    jsonWriter.WriteArray(null, uiGroup.contains);
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteStartArray("features");
                foreach (var featureRef in featuresByGuid.Values.OrderBy(x => x.index))
                {
                    var feature = featureRef.handle;
                    if (feature.EnsureObj.True("HideInUI")) continue;

                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteNumber("__index", featureRef.index);
                    jsonWriter.WriteString("name", feature.EnsureObj.Find("m_DisplayName").ParseAsString());
                    jsonWriter.WriteString("desc", feature.EnsureObj.Find("m_Description").ParseAsString());
                    jsonWriter.WriteString("icon", featureRef.icon);
                    jsonWriter.WriteBoolean("isSelection", featureRef.IsSelection);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteString("name", obj.Find("LocalizedName").ParseAsString());
                jsonWriter.WriteString("desc", obj.Find("LocalizedDescription").ParseAsString());

                WriteLayout(prog.Rows.Values, jsonWriter);
                WriteStats(prog.Flags, jsonWriter);
                WriteSpellbook(prog.SpellsByLeveL, prog.NewSpellsByLevel, prog.CasterAbility, prog.CasterType, prog.Spontaneous, jsonWriter);
                jsonWriter.WriteNumber("casterLevelModifier", prog.CasterLevelModifier);
                jsonWriter.WriteNumber("HitDie", prog.HitDie);

                jsonWriter.WriteStartArray("archetypes");
                foreach (var arch in prog.archetypes)
                {
                    jsonWriter.WriteStartObject();

                    jsonWriter.WriteString("name", arch.Name);
                    jsonWriter.WriteString("desc", arch.Desc);
                    jsonWriter.WriteString("id", arch.Id);
                    WriteLayout(arch.Rows.Values, jsonWriter);
                    WriteStats(arch.Flags, jsonWriter);

                    WriteSpellbook(arch.SpellsByLeveL, arch.NewSpellsByLevel, arch.CasterAbility, arch.CasterType, arch.Spontaneous, jsonWriter);
                    jsonWriter.WriteBoolean("removeSpells", arch.removeSpells);

                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndObject();

                classList.Add($@"new(""{clazzBp.Name}"",");
                classList.Add(string.Join(", ", prog.archetypes.Select(a => '"' + a.Name + '"')));
                classList.Add($@"),");
            }

            File.WriteAllLines(@"D:\classes.txt", classList);

            using var saber = new SaberRenderer((Bitmap)Bitmap.FromFile(@"D:\font_atlas.png"), File.ReadAllLines(@"D:\font_atlas_.txt"));

            int fromName = 0;
            int iconNotFound = 0;
            int iconFound = 0;
            int cacheHit = 0;


            var placeholderBackground = Bitmap.FromFile(@"D:\wrath-data-raw\Sprite\UI_PlaceHolderIcon7.png");


            foreach (var (request, requestor) in iconRequests.OrderBy(r => r.request))
            {
                string filename = $@"D:\wrath-wiki\wwwroot\icons\{request}.png";
                if (File.Exists(filename)) {
                    cacheHit++;
                    continue;
                }

                if (request.StartsWith("gen__"))
                {
                    Bitmap buffer = new(64, 64, PixelFormat.Format32bppArgb);
                    TextureBrush brush = new(placeholderBackground);

                    using (var g = Graphics.FromImage(buffer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillEllipse(brush, new(0, 0, buffer.Width - 1, buffer.Height - 1));
                    }

                    var acronym = request[5..];

                    saber.Render(acronym, buffer, new()
                    {
                        Thickness = 0.5f,
                        Softness = 0.6f,

                        OutlineThickness = 0.5f,
                        OutlineSoftness = 0.0f,

                        Color = new(0.8f, 0.7f, 0.5f),

                        TextScale = acronym.Length == 3 ? 0.4f : 0.5f,
                        LetterSpacing = -9,

                        Clear = false,
                        Baseline = 20,
                    });

                    using (var g = Graphics.FromImage(buffer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawImage(borderImage, new Rectangle(0, 0, 64, 64));
                    }


                    buffer.Save(filename);
                    fromName++;
                }
                else
                {
                    if (!blueprintContext.TryGetValue(request, out var assetRef))
                    {
                        Console.WriteLine("Could not find asset...");
                        iconNotFound++;
                        continue;
                    }

                    PPtr<Sprite> ptr = new(assetRef, blueprintAssets);
                    EmitSprite((request, requestor), ptr.Object);

                    iconFound++;
                }
            }

            Console.WriteLine($"{iconRequests.Count} requests for icons, {cacheHit} already cached, {fromName} generated from name, {iconFound} icons found, {iconNotFound} icons NOT found");
        }

        private static void AddDeterminators(List<LocalReference> allFeatureReferences, IProgressionLayout source, IProgressionLayout to)
        {
            foreach (var (d, addRemove) in source.Determinators)
            {
                var featureRef = allFeatureReferences[d];
                var row = to.EnumerateRows()
                    .Where(x => x.FeatureByLevel[0] == null && featureRef.ProgressionGroup == x.ProgressionGroup)
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (row == null)
                {
                    row = new()
                    {
                        IsOverflow = true,
                    };
                    to.AddFake(row);
                }
                row.Set(0, featureRef, addRemove);
            }
        }

        private static string ExtractCasterType(JsonElement archObj)
        {
            if (archObj.True("IsDivineCaster"))
                return "Divine";
            else if (archObj.True("IsArcaneCaster"))
                return "Arcane";
            else
                return "Special";
        }

        private static void WriteSpellbook(int[][] spellsByLeveL, string[][] newSpellsByLevel, string casterAbility, string casterType, bool spontaneous, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteString("casterType", casterType);
            jsonWriter.WriteString("casterAbility", casterAbility);
            jsonWriter.WriteBoolean("spontaneous", spontaneous);
            if (spellsByLeveL == null)
            {
                jsonWriter.WriteNull("spellSlots");
            }
            else
            {
                jsonWriter.WriteStartArray("spellSlots");
                foreach (var table in spellsByLeveL)
                    jsonWriter.WriteArray(null, table);
                jsonWriter.WriteEndArray();
            }

            if (newSpellsByLevel == null)
            {
                jsonWriter.WriteNull("spells");
            }
            else
            {
                jsonWriter.WriteStartArray("spells");
                foreach (var table in newSpellsByLevel)
                    jsonWriter.WriteArray(null, table);
                jsonWriter.WriteEndArray();
            }

        }

        private static void ExtractSpellProgression(BlueprintHandle spellsBp, ref int[][] spellsByLeveL, ref int casterLevelModifier, ref string[][] newSpellsByLevel, ref string casterAbility, ref bool spontaneous)
        {
            var spellsPerDay = spellsBp.EnsureObj.Find("m_SpellsPerDay").DeRef();
            var spellList = spellsBp.EnsureObj.Find("m_SpellList").DeRef();

            spontaneous = spellsBp.obj.True("Spontaneous");

            casterAbility = spellsBp.obj.Str("CastingAttribute");

            spellsByLeveL = new int[21][];

            int level = 0;
            foreach (var table in spellsPerDay.EnsureObj.Find("Levels").EnumerateArray().Select(x => x.Find("Count")))
            {
                if (level > 20)
                    break;
                spellsByLeveL[level++] = table.EnumerateArray().Select(x => x.GetInt32()).ToArray();
            }


            newSpellsByLevel = new string[11][];
            foreach (var byLevel in spellList.EnsureObj.Find("SpellsByLevel").EnumerateArray())
            {
                int cl = byLevel.Int("SpellLevel");
                if (cl == 0) continue;

                newSpellsByLevel[cl] = byLevel.Find("m_Spells").EnumerateArray().Select(x => x.DeRef().EnsureObj.Find("m_DisplayName").ParseAsString()).ToArray();
            }

            casterLevelModifier = spellsBp.obj.Int("CasterLevelModifier");
        }

        private static void WriteStats(FlagProgression flags, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteArray("bab", flags.bab);
            jsonWriter.WriteArray("fort", flags.fort);
            jsonWriter.WriteArray("reflex", flags.reflex);
            jsonWriter.WriteArray("will", flags.will);

            jsonWriter.WriteString("babSpeed", flags.babSpeed);
            jsonWriter.WriteString("fortSpeed", flags.fortSpeed);
            jsonWriter.WriteString("reflexSpeed", flags.reflexSpeed);
            jsonWriter.WriteString("willSpeed", flags.willSpeed);
        }

        private static void ExtractStatProgression(ref int[] arr, ref string speed, BlueprintHandle blueprintHandle)
        {
            speed = blueprintHandle.GuidText switch
            {
                "b3057560ffff3514299e8b93e7648a9d" => "High",
                "ff4662bde9e75f145853417313842751" => "High",

                "4c936de4249b61e419a3fb775b9f2581" => "Average",

                "dc0c7c1aba755c54f96c089cdf7d14a3" => "Low",
                "0538081888b2d8c41893d25d098dee99" => "Low",
                _ => "Unknown",
            };

            int level = 0;
            arr = new int[21];
            foreach (int current in blueprintHandle.EnsureObj.Find("Bonuses").EnumerateArray().Select(x => x.GetInt32()))
            {
                if (level <= 20)
                {
                    arr[level] = current;
                }

                level++;
            }
        }

        private static void LayoutFeature(ClassProgression prog, IProgressionLayout layout, int level, LocalReference feature, int addRemove = 0)
        {
            if (prog.uiDeterminators.Contains(feature.index))
            {
                layout.Determinators.Add((feature.index, addRemove));
                return;
            }

            var (uiGroup, row) = prog.LookupGroup(feature.index);
            if (!layout.LookupRow(row, uiGroup, feature.ProgressionGroup).Set(level, feature, addRemove))
            {
                LevelRow addto = new()
                {
                    IsOverflow = true,
                };
                addto.Set(level, feature, addRemove);
                layout.AddFake(addto);
            }
        }

        private static void MergeRows(List<LocalReference> allFeatureReferences, IEnumerable<LevelRow> rows)
        {
            List<LevelRow> mergeRows = rows.Where(r => r.MergeTarget).ToList();

            for (int i = 1; i < mergeRows.Count; i++)
            {
                var feature = mergeRows[i].FeatureByLevel.First(x => x != null);

                for (int b = 0; b < i; b++)
                {
                    var featureRef = allFeatureReferences[feature.index];
                    if (mergeRows[b].Count > 0 && mergeRows[b].FeatureByLevel[feature.level] == null && mergeRows[b].ProgressionGroup == featureRef.ProgressionGroup)
                    {
                        mergeRows[b].Set(feature.level, featureRef, feature.addRemove);
                        mergeRows[i].Remove(feature.level);
                        break;
                    }
                }
            }
        }

        private static void WriteLayout(IEnumerable<LevelRow> rows, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartArray("layout");
            var groups = rows.GroupBy(row => row.ProgressionGroup).ToList();
            groups.Sort((x, y) =>
            {
                if (x.Key == null)
                    return -1;
                return -x.Sum(r => r.Count).CompareTo(y.Sum(r => r.Count));
            });
            foreach (var group in groups)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("name", group.Key);
                jsonWriter.WriteStartArray("rows");
                foreach (var row in group)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteBoolean("lines", row.IsUIGroup || row.HasMultipleOfFeature);
                    jsonWriter.WriteStartArray("features");
                    for (int l = 0; l <= 20; l++)
                    {
                        if (row.FeatureByLevel[l] != null)
                        {
                            jsonWriter.WriteStartObject();
                            jsonWriter.WriteNumber("level", l);
                            jsonWriter.WriteNumber("feature", row.FeatureByLevel[l].index);
                            jsonWriter.WriteNumber("rank", row.FeatureByLevel[l].rank);
                            jsonWriter.WriteNumber("archType", row.FeatureByLevel[l].addRemove);
                            jsonWriter.WriteEndObject();
                        }
                    }
                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();

            }
            jsonWriter.WriteEndArray();
        }

        private static Dictionary<string, byte[]> atlasCache = new();
        private static Image borderImage;

        private static void EmitSprite(IconRequest request, Sprite sprite)
        {
            var id = request.request;

            Rectf cut = sprite.m_Rect;
            Texture2D tex;
            string atlasKey = null;

            if (sprite.m_SpriteAtlas.Identifier != 0)
            {
                var atlas = sprite.m_SpriteAtlas.Object;

                if (!atlas.m_RenderDataMap.TryGetValue(sprite.m_RenderDataKey, out var spriteAtlasData))
                {
                    Console.Error.WriteLine($"atlased sprite does not have the atlasData for requestor: {request.requestor.handle.Name}");
                    return;
                }

                cut = spriteAtlasData.textureRect;
                tex = spriteAtlasData.texture.Object;
                atlasKey = $"{spriteAtlasData.texture.FileIndex}/{spriteAtlasData.texture.Identifier}";
            }
            else if (sprite.m_RD.texture.Identifier != 0)
            {
                tex = sprite.m_RD.texture.Object;
            }
            else
            {
                throw new Exception("No texture source for sprite");
            }

            AssetFileReader resourceStream;

            if (tex.m_StreamData?.path != null)
            {
                resourceStream = new(
                    tex.Owner,
                    stream: tex.Owner.Context.resourceStreams[Path.GetFileName(tex.m_StreamData.path)].Based(tex.m_StreamData.offset, tex.m_StreamData.size),
                    endian: EndianType.Little);
            }
            else
            {
                resourceStream = new(
                    tex.Owner,
                    stream: tex.Owner.blockStream.Based(tex.Owner.blockStream.Position, tex.image_data_size),
                    endian: EndianType.Little);
            }

            var converter = new Texture2DConverter(resourceStream, tex);

            bool returnBuf = true;
            byte[] buff = null;
            try
            {
                if (atlasKey != null)
                {
                    if (!atlasCache.TryGetValue(atlasKey, out buff))
                    {
                        buff = BigArrayPool<byte>.Shared.Rent(tex.m_Width * tex.m_Height * 4);
                        converter.DecodeTexture2D(buff);
                        atlasCache[atlasKey] = buff;
                    }

                    returnBuf = false;
                }
                else
                {
                    buff = BigArrayPool<byte>.Shared.Rent(tex.m_Width * tex.m_Height * 4);
                    converter.DecodeTexture2D(buff);
                }


                const PixelFormat format = PixelFormat.Format32bppArgb;
                var bmp = new Bitmap((int)cut.width, (int)cut.height, format);
                Rectangle rect = new(0, 0, bmp.Width, bmp.Height);

                var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);
                IntPtr ptr = data.Scan0;
                if (cut.x != 0 || cut.y != 0 || cut.width != tex.m_Width || cut.height != tex.m_Height)
                {
                    for (int ly = 0; ly < cut.height; ly++)
                    {
                        int input_y = (int)(ly + cut.y);
                        int input_x = (int)cut.x;

                        Marshal.Copy(buff, (int)((input_y * tex.m_Width + input_x) * 4), ptr + ly * data.Stride, data.Stride);
                    }
                }
                else
                {
                    Marshal.Copy(buff, 0, ptr, data.Stride * data.Height);
                }
                bmp.UnlockBits(data);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Bitmap dest = new(64, 64, format);
                TextureBrush brush = new(bmp);

                using (var g = Graphics.FromImage(dest))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillEllipse(brush, new(0, 0, 64 - 1, 64 - 1));
                    g.DrawImage(borderImage, new Rectangle(0, 0, 65, 65));
                }


                dest.Save($@"D:\wrath-wiki\wwwroot\icons\{id}.png");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (buff != null && returnBuf)
                    BigArrayPool<byte>.Shared.Return(buff);
            }
        }
    }

    public static class JsonWriterExt
    {
        public static void WriteArray(this Utf8JsonWriter writer, string propName, IEnumerable<double> values) => WriteArray(writer, propName, values, v => writer.WriteNumberValue(v));

        public static void WriteArray(this Utf8JsonWriter writer, string propName, IEnumerable<int> values) => WriteArray(writer, propName, values, v => writer.WriteNumberValue(v));

        public static void WriteArray(this Utf8JsonWriter writer, string propName, IEnumerable<float> values) => WriteArray(writer, propName, values, v => writer.WriteNumberValue(v));

        public static void WriteArray(this Utf8JsonWriter writer, string propName, IEnumerable<string> values) => WriteArray(writer, propName, values, v => writer.WriteStringValue(v));
        public static void WriteArray<T>(this Utf8JsonWriter writer, string propName, IEnumerable<T> values, Action<T> writeAction)
        {
            if (values == null)
            {
                if (propName != null)
                    writer.WriteNull(propName);
                else
                    writer.WriteNullValue();
                return;
            }

            if (propName != null)
                writer.WriteStartArray(propName);
            else
                writer.WriteStartArray();

            foreach (var value in values)
                writeAction(value);

            writer.WriteEndArray();
        }

    }

    public class LocalReference
    {
        public string ProgressionGroup;
        public bool IsSelection;

        public override string ToString() => $"{ProgressionGroup ?? ""}/{handle.Name}";

        public List<(LocalReference, int Level)> SubProgression;

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
        public int addRemove = 0;
        public int index = -1;
        public int rank = -1;
        public int level = -1;
        public string __name;

        internal void SetAddRemove(int addRemove) => this.addRemove = addRemove;
    }

    public class LevelRow
    {
        public string ProgressionGroup = null;
        public int Count = 0;
        public bool HasMultipleOfFeature = false;
        public bool IsOverflow = false;
        public bool IsUIGroup = false;
        public FeatureEntry[] FeatureByLevel = new FeatureEntry[21];

        public override string ToString() => $"{ProgressionGroup??""}: {Count} / {FeatureByLevel.First(x => x != null)?.__name}";

        public bool MergeTarget
        {
            get
            {
                if (Count != 1) return false;
                //if (IsUIGroup) return false;
                if (HasMultipleOfFeature) return false;

                return true;
            }
        }

        public bool Set(int level, LocalReference feature, int addRemove = 0)
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

            HasMultipleOfFeature |= FeatureByLevel.Any(x => x?.index == feature.index);

            FeatureByLevel[level] = new()
            {
                index = feature.index,
                rank = rank,
                level = level,
                addRemove = addRemove,
                __name = feature.handle.Name,
            };

            if (ProgressionGroup != null && ProgressionGroup != feature.ProgressionGroup)
            {
                throw new Exception();
            }
            ProgressionGroup = feature.ProgressionGroup;

            Count++;
            return true;
        }

        internal void Remove(int level)
        {
            if (FeatureByLevel[level] == null)
                throw new Exception("Double free");
            FeatureByLevel[level] = null;
            Count--;
        }

        public LevelRow Clone()
        {
            LevelRow ret = new()
            {
                IsOverflow = this.IsOverflow,
                IsUIGroup = this.IsUIGroup,
                HasMultipleOfFeature = this.HasMultipleOfFeature,
                ProgressionGroup = this.ProgressionGroup,
            };
            for (int i = 1; i < FeatureByLevel.Length; i++)
            {
                var f = FeatureByLevel[i];
                if (f == null)
                    continue;
                ret.FeatureByLevel[i] = new()
                {
                    addRemove = f.addRemove,
                    index = f.index,
                    level = f.level,
                    rank = f.rank,

                };
            }
            ret.Count = ret.FeatureByLevel.Count(x => x is not null);
            return ret;
        }
    }

    public interface IProgressionLayout
    {
        void AddFake(LevelRow addto);
        LevelRow LookupRow(int row, bool uiGroup, string progressionGroup);
        IEnumerable<LevelRow> EnumerateRows();

        List<(int id, int addRemove)> Determinators { get; }

        FlagProgression Flags { get; }

        public static string RowKey(int row, string progressionGroup)
        {
            return $"{progressionGroup ?? "main"}:{row}";
        }

    }

    public class FlagProgression
    {
        public int[] bab = null;
        public int[] will = null;
        public int[] reflex = null;
        public int[] fort = null;
        internal string babSpeed;
        internal string fortSpeed;
        internal string reflexSpeed;
        internal string willSpeed;
    }


    public class ArchetypeProgression : IProgressionLayout
    {
        public BlueprintHandle bp;
        public string Name;
        public string Id;

        public FlagProgression Flags { get; } = new();

        public int[][] SpellsByLeveL;
        public string[][] NewSpellsByLevel;

        public List<(int id, int addRemove)> Determinators { get; } = new();

        public Dictionary<int, List<int>> remove = new();
        public Dictionary<int, List<int>> add = new();
        public Dictionary<string, LevelRow> Rows = new();

        public LevelRow LookupRow(int row, bool uiGroup, string progressionGroup)
        {
            string key = IProgressionLayout.RowKey(row, progressionGroup);
            if (!Rows.TryGetValue(key, out var levelRow))
            {
                levelRow = new()
                {
                    IsUIGroup = uiGroup
                };
                if (row == -1)
                    levelRow.IsOverflow = true;
                Rows[key] = levelRow;
            }
            return levelRow;
        }

        internal FeatureEntry FindFeature(int level, int f)
        {
            foreach (var row in Rows.Values)
            {
                if (level == 1 && row.FeatureByLevel[0]?.index == f)
                {
                    return row.FeatureByLevel[0];
                }

                if (row.FeatureByLevel[level]?.index == f)
                {
                    return row.FeatureByLevel[level];
                }
            }
            return null;
        }

        public void AddFake(LevelRow row)
        {
            Rows[$"fake:{FakeRow++}"] = row;
        }

        public IEnumerable<LevelRow> EnumerateRows() => Rows.Values;

        int FakeRow = -100000;
        internal bool removeSpells;
        internal int CasterLevelModifier = 0;
        internal string CasterType;
        internal string CasterAbility;
        public bool Spontaneous;
        internal string Desc;
    }

    public class ClassProgression : IProgressionLayout
    {
        public List<LocalReference>[] FeatureByLevel = new List<LocalReference>[21];
        public FlagProgression Flags { get; } = new();

        public List<UIGroup> UIGroups = new();
        public List<(int id, int addRemove)> Determinators { get; } = new();

        public Dictionary<string, LevelRow> Rows = new();
        public List<ArchetypeProgression> archetypes = new();
        public int FakeRow = -100000;
        public IEnumerable<LevelRow> EnumerateRows() => Rows.Values;

        public int[][] SpellsByLeveL;
        public string[][] NewSpellsByLevel;
        public string CasterType;
        public string CasterAbility;
        public bool Spontaneous;
        internal int CasterLevelModifier;
        internal int HitDie;
        internal HashSet<int> uiDeterminators = new();

        public (bool, int) LookupGroup(int feature)
        {
            var id = UIGroups.Find(set => set.Contains(feature));
            if (id != null)
                return (true, id.Id);
            return (false, feature);
        }
        public void AddFake(LevelRow row)
        {
            Rows[$"fake:{FakeRow++}"] = row;
        }

        public LevelRow LookupRow(int row, bool uiGroup, string progressionGroup)
        {
            string key = IProgressionLayout.RowKey(row, progressionGroup);
            if (!Rows.TryGetValue(key, out var levelRow))
            {
                levelRow = new()
                {
                    IsUIGroup = uiGroup
                };

                if (row == -1)
                    levelRow.IsOverflow = true;

                Rows[key] = levelRow;
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

    internal record struct IconRequest(string request, LocalReference requestor)
    {
        public static implicit operator (string request, LocalReference requestor)(IconRequest value)
        {
            return (value.request, value.requestor);
        }

        public static implicit operator IconRequest((string request, LocalReference requestor) value)
        {
            return new IconRequest(value.request, value.requestor);
        }
    }
}
