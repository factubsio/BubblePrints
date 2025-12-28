using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bprint;

internal static class DialogSer
{
    public static string ToMermaid(DialogSource source)
    {
        var sb = new System.Text.StringBuilder("graph LR;\n");

        // Define styles
        sb.AppendLine("    classDef cAnswerList color:#000,fill:#FFD700,stroke:#333,stroke-width:2px;");
        sb.AppendLine("    classDef cAnswer color:#000,fill:#90EE90,stroke:#000,stroke-width:2px;");
        sb.AppendLine("    classDef cCue color:#000,fill:#87CEFA,stroke:#000,stroke-width:2px;");
        sb.AppendLine("    classDef cCueSequence fill:#4682B4,stroke:#333,stroke-width:2px;");
        sb.AppendLine("    classDef cCueSequenceExit fill:#4682B4,stroke:#333,stroke-width:2px;");
        sb.AppendLine("    classDef cBookPage color:#000,fill:#FFFACD,stroke:#333,stroke-width:2px;");
        sb.AppendLine("    classDef cCheck color:#000,fill:#FF6347,stroke:#333,stroke-width:2px;");
        sb.AppendLine("    classDef cDefault fill:#fff,stroke:#333,stroke-width:1px;");

        // Legend
        //sb.AppendLine("    subgraph Legend");
        //sb.AppendLine("        direction LR");
        //sb.AppendLine("        k1[AnswerList]:::cAnswerList");
        //sb.AppendLine("        k2[Answer]:::cAnswer");
        //sb.AppendLine("        k3[Cue]:::cCue");
        //sb.AppendLine("        k4[CueSequence]:::cCueSequence");
        //sb.AppendLine("        k5[BookPage]:::cBookPage");
        //sb.AppendLine("        k6[Check]:::cCheck");
        //sb.AppendLine("        k6[SequenceExit]:::cCueSequenceExit");
        //sb.AppendLine("    end");

        var visited = new HashSet<Guid>();
        var queue = new Queue<DialogNode>(source.First);

        //if (source.First.Count > 0)
        //{
        //    string startId = $"N{source.First[0].Id:N}";
        //    sb.AppendLine($"    k1 ~~~ {startId};");
        //}

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.Id)) continue;

            string nodeId = $"N{node.Id:N}";
            string label = node.Text.Sanitized();

            // Determine style class based on Type
            string className = node.Type switch
            {
                DialogNodeType.AnswerList => "cAnswerList",
                DialogNodeType.Answer => "cAnswer",
                DialogNodeType.Cue => "cCue",
                DialogNodeType.CueSequence => "cCueSequence",
                DialogNodeType.CueSequenceExit => "cCueSequenceExit",
                DialogNodeType.BookPage => "cBookPage",
                DialogNodeType.Check => "cCheck",
                _ => "cDefault"
            };

            // Append class syntax :::className
            sb.AppendLine($"    {nodeId}[\"{label}\"]:::{className};");
            sb.AppendLine($"    click {nodeId} \"{nodeId[1..]}\" \"Click to view\";");
            string exitNode = nodeId;

            if (node.Sequence != null)
            {
                string? first = null;
                string? last = null;
                sb.AppendLine($"    subgraph seq_{nodeId}");
                sb.AppendLine($"       direction TB;");
                foreach (var cue in node.Sequence)
                {
                    string cueId = $"N{cue.Id:N}";
                    first ??= cueId;
                    if (last != null)
                    {
                        sb.AppendLine($"{last} --> {cueId};");
                    }
                    last = cueId;
                    sb.AppendLine($"    {cueId}[\"{cue.Text.Sanitized()}\"]:::cCue;");
                    sb.AppendLine($"    click {cueId} \"{cueId[1..]}\" \"Click to view\";");
                }
                sb.AppendLine("    end");
                if (first != null)
                {
                    sb.AppendLine($"{nodeId} --> {first};");
                }

                if (last != null)
                    exitNode = last;
            }

            foreach (var (child, port) in node.To)
            {
                string childId = $"N{child.Id:N}";
                if (child.Condition.Length > 0)
                    sb.AppendLine($"    {exitNode} --\"{child.Condition}\"--> {childId};");
                else
                    sb.AppendLine($"    {exitNode} --> {childId};");

                if (!visited.Contains(child.Id))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return sb.ToString();
    }
    public static byte[] ToJson(DialogSource source)
    {
        var buffer = new MemoryStream();
        var json = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = true,
        });

        json.WriteStartObject();

        json.WriteStartObject("nodes");
        foreach (var node in source.All)
        {
            json.WriteStartObject(node.IdString);
            json.WriteNumber("lvl", node.Level);
            json.WriteString("typ", node.Type.ToString());
            uint flags = 0;
            if(node.IsConditional)
            {
                flags |= 1;
            }
            json.WriteNumber("flg", flags);
            if (node.Text.Length > 0)
                json.WriteString("text", node.Text);

            if (node.Sequence?.Count > 0)
            {
                json.WriteStartArray("seq");
                foreach (var cue in node.Sequence)
                {
                    json.WriteStringValue(cue.IdString);
                }
                json.WriteEndArray();
            }

            if (node.To.Count >0)
            {
                json.WriteStartArray("out");
                foreach (var (target, port) in node.To)
                {
                    json.WriteStartObject();
                    json.WriteString("port", port);
                    json.WriteString("to", target.IdString);
                    json.WriteEndObject();
                }
                json.WriteEndArray();
            }

            if (node.Props.Count > 0)
            {
                json.WriteStartObject("props");
                foreach (var (k, v) in node.Props)
                {
                    json.WritePropertyName(k);
                    json.WriteRawValue(JsonSerializer.Serialize(v));
                }
                json.WriteEndObject();
            }

            json.WriteEndObject();
        }
        
        json.WriteEndObject();

        json.WriteEndObject();

        json.Flush();
        buffer.Flush();

        return buffer.GetBuffer();
    }

    private static void SerializePropObject(string key, object val)
    {

    }

    private static string Sanitized(this string str) => str.Replace('"', '\'');
}
