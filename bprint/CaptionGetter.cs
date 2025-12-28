using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bprint;

internal class CaptionGetter
{
    //public static List<string> GetStringsFromMethod(MethodInfo method, MetadataReader meta)
    //{
    //    List<string> results = [];
    //    var body = method.GetMethodBody();

    //    if (body == null) return results;

    //    byte[] il = body.GetILAsByteArray() ?? [];

    //    // Iterate through the bytes
    //    for (int i = 0; i < il.Length; i++)
    //    {
    //        byte opCode = il[i];

    //        if (opCode == (byte)OpCodes.Ldstr.Value)
    //        {
    //            // The next 4 bytes form the Metadata Token
    //            // We need to ensure we don't go out of bounds
    //            if (i + 4 < il.Length)
    //            {
    //                int token = BitConverter.ToInt32(il, i + 1);

    //                try
    //                {
    //                    // Resolve the string using the module
    //                    string resolvedString = meta.GetUserString(MetadataTokens.UserStringHandle(token));
    //                    results.Add(resolvedString);
    //                }
    //                catch
    //                {
    //                    // Token resolution might fail in dynamic contexts, ignore
    //                }
    //            }

    //            // Move the index forward by 4 bytes (operand size)
    //            i += 4;
    //        }

    //        // Note: A full IL parser is complex because instructions have different lengths.
    //        // This simple loop assumes that if we see 0x72, it is an instruction.
    //        // In rare cases, 0x72 could be the operand of a jump instruction, 
    //        // but for string extraction, this is usually 99% effective.
    //    }

    //    return results;
    //}

    //private static void bob(string managedPath = @"C:\Program Files (x86)\Steam\steamapps\common\Warhammer 40,000 Dark Heresy Playtest\WH40KDH_Data\Managed")
    //{
    //    var resolver = new PathAssemblyResolver(Directory.EnumerateFiles(managedPath, "*.dll"));
    //    var _mlc = new MetadataLoadContext(resolver);

    //    var asm = _mlc.LoadFromAssemblyPath(Path.Combine(managedPath, "Code.dll"));

    //    var assemblies = Directory
    //        .EnumerateFiles(Path.GetDirectoryName(asm.Location) ?? throw new NotSupportedException(), "*.dll")
    //        .SelectMany(assFile =>
    //        {
    //            try
    //            {
    //                return new[] { _mlc.LoadFromAssemblyPath(assFile) };
    //            }
    //            catch
    //            {
    //                return [];
    //            }
    //        });

    //    var types = assemblies.SelectMany(ass =>
    //    {
    //        try
    //        {
    //            return ass.GetTypes();
    //        }
    //        catch (ReflectionTypeLoadException ex)
    //        {
    //            return ex.Types.Where(t => t != null);
    //        }
    //    });
    //    const string typeIdTypeName = "Owlcat.Runtime.Core.Utility.TypeIdAttribute";
    //    var typeIdType = assemblies
    //        .Select(ass => ass.GetType(typeIdTypeName))
    //        .FirstOrDefault(t => t is not null) ?? throw new NotSupportedException("cannot find TypeId Type");


    //    using var fs = File.OpenRead(asm.Location);
    //    using var pe = new PEReader(fs);
    //    MetadataReader meta = pe.GetMetadataReader();

    //    foreach (var type in types)
    //    {
    //        if (type?.FullName == null) continue;

    //        foreach (var data in type.GetCustomAttributesData())
    //        {
    //            try
    //            {
    //                if (data.AttributeType.Name == typeIdType.Name)
    //                {
    //                    if (data.ConstructorArguments[0].Value is string guid)
    //                    {
    //                        Console.WriteLine(type.FullName);
    //                        var getCaption = type.GetMethod("GetConditionCaption", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy, []);
    //                        if (getCaption != null)
    //                        {
    //                            var strings = GetStringsFromMethod(getCaption, meta);
    //                            Console.WriteLine(string.Join(',', strings));

    //                        }
    //                    }
    //                }
    //            }
    //            catch // Pretty sure this is some cursed attribute and not the TypeId we want
    //            { }
    //        }

    //    }

    //}

}
