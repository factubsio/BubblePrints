using BlueprintExplorer;
using static BlueprintExplorer.BlueprintDB;
using BinzFactory;
using System.Runtime.ExceptionServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using BubbleprintEngine;

namespace bprint;

public static class Program
{
    public static void Main(string[] args)
    {
        BubblePrints.Install();

        var cmdletsIt = Assembly.GetExecutingAssembly().GetTypes()
                .SelectMany(type => type
                    .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Select(m => new { Method = m, Attrib = m.GetCustomAttribute<CmdletAttribute>() })
                    .Where(m => m.Attrib != null));

        Dictionary<string, Action<string[]>> cmdlets = [];

        foreach (var cmd in cmdletsIt)
        {
            cmdlets[cmd.Attrib!.Name] = args =>
            {
                Task? t = cmd.Method.Invoke(null, [args]) as Task;
                t?.Wait();
                
            };
        }

        if (args.Length == 0)
        {
            Console.Error.WriteLine("A command is required. Available commands:");
            Console.Error.WriteLine(string.Join('\n', cmdlets.Keys));
            return;
        }

        if (!cmdlets.TryGetValue(args[0], out var cmdlet))
        {
            Console.Error.WriteLine($"No command '{args[0]}', available commands:");
            Console.Error.WriteLine(string.Join('\n', cmdlets.Keys));
            return;
        }

        cmdlet(args);
    }

    public static async Task<BlueprintDB> LoadDB(string game)
    {
        var bins = new BinzManager();
        var binz = bins.Available.FirstOrDefault(
            b => b.Version.Game.Equals(game, StringComparison.InvariantCultureIgnoreCase))
            ?? throw new Exception($"No {game} binz is available locally or remotely.");

        if (!binz.Local)
        {
            Console.WriteLine($"Downloading {binz.Version}...");
            int lastReported = -10;
            await bins.Download(binz, progress =>
            {
                if (progress >= lastReported + 10)
                {
                    Console.WriteLine($"{progress}%");
                    lastReported = progress;
                }
            });
            binz.Local = true;
            Console.WriteLine($"Downloaded to {binz.Path}");
        }

        BlueprintDB db = new(game);

        var loadProgress = new ConnectionProgress();
        var initialize = Task.Run(() =>
            db.TryConnect(loadProgress, binz.Path, saveSettings: false));
        var idle = Task.Run(() =>
        {
            while (!initialize.IsCompleted)
            {
                Console.Write(".");
                Thread.Sleep(200);
            }
        });
        await initialize;
        await idle;
        return db;
    }

}

[AttributeUsage(AttributeTargets.Method)]
public class CmdletAttribute(string name) : Attribute
{
    public string Name => name;
}
