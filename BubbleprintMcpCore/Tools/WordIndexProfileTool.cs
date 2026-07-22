using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore.Tools;

public sealed class WordIndexProfileTool : IMcpTool
{
    public string Name => "word-index-profile";
    public string Description =>
        "Profile the existing # word postings. Args: top (default 10), terms (optional array of substring terms), estimateTrigrams (default false).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        int top = Math.Clamp(args["top"]?.GetValue<int>() ?? 10, 1, 50);
        string[] terms = args["terms"] is JsonArray termNodes
            ? termNodes
                .Select(node => node?.GetValue<string>() ?? "")
                .Where(term => term.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];
        bool estimateTrigrams =
            args["estimateTrigrams"]?.GetValue<bool>() ?? false;

        var stopwatch = Stopwatch.StartNew();
        long postings = 0;
        long deltaVarintBytes = 0;
        long wordUtf8Bytes = 0;
        long wordUtf16Bytes = 0;
        int[] postingCounts = new int[db.IndexByWord.Count];
        var largest = new PriorityQueue<(string Word, int Documents), int>();
        int postingIndex = 0;

        foreach (var (word, documents) in db.IndexByWord)
        {
            postings += documents.Count;
            postingCounts[postingIndex++] = documents.Count;
            wordUtf8Bytes += Encoding.UTF8.GetByteCount(word);
            wordUtf16Bytes += word.Length * sizeof(char);

            int previous = 0;
            for (int i = 0; i < documents.Count; i++)
            {
                int delta = i == 0 ? documents[i] : documents[i] - previous;
                deltaVarintBytes += VarintSize((uint)delta);
                previous = documents[i];
            }

            largest.Enqueue((word, documents.Count), documents.Count);
            if (largest.Count > top)
                largest.Dequeue();
        }

        Array.Sort(postingCounts);
        var largestPostings = largest.UnorderedItems
            .Select(item => item.Element)
            .OrderByDescending(item => item.Documents)
            .Select(item => new
            {
                word = item.Word,
                documents = item.Documents,
                documentFraction =
                    (double)item.Documents / db.BlueprintsInOrder.Count,
            })
            .ToArray();

        var termProfiles = new List<object>();
        foreach (string term in terms)
        {
            var termWatch = Stopwatch.StartNew();
            var candidates = new bool[db.BlueprintsInOrder.Count];
            int matchingWords = 0;
            int candidateCount = 0;

            foreach (var (word, documents) in db.IndexByWord)
            {
                if (!word.Contains(term, StringComparison.OrdinalIgnoreCase))
                    continue;

                matchingWords++;
                foreach (int document in documents)
                {
                    if (candidates[document])
                        continue;
                    candidates[document] = true;
                    candidateCount++;
                }
            }

            termWatch.Stop();
            termProfiles.Add(new
            {
                term,
                matchingWords,
                candidates = candidateCount,
                candidateFraction =
                    (double)candidateCount / db.BlueprintsInOrder.Count,
                scanMs = termWatch.Elapsed.TotalMilliseconds,
            });
        }

        object? trigramEstimate = null;
        if (estimateTrigrams)
        {
            var trigrams = new Dictionary<int, TrigramStats>();
            var uniqueInWord = new HashSet<int>();
            int wordId = 0;

            foreach (string word in db.IndexByWord.Keys)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(word);
                uniqueInWord.Clear();
                for (int i = 0; i + 2 < bytes.Length; i++)
                {
                    int trigram = bytes[i] << 16 | bytes[i + 1] << 8 | bytes[i + 2];
                    if (!uniqueInWord.Add(trigram))
                        continue;

                    if (!trigrams.TryGetValue(trigram, out var stats))
                    {
                        stats = new TrigramStats();
                        trigrams.Add(trigram, stats);
                    }
                    stats.AddWord(wordId);
                }
                wordId++;
            }

            int[] trigramCounts = trigrams.Values
                .Select(stats => stats.Words)
                .Order()
                .ToArray();
            long trigramPostings = trigrams.Values.Sum(stats => (long)stats.Words);
            long trigramPostingBytes =
                trigrams.Values.Sum(stats => stats.DeltaVarintBytes);
            trigramEstimate = new
            {
                distinctTrigrams = trigrams.Count,
                postings = trigramPostings,
                compressedPostingBytes = trigramPostingBytes,
                metadataBytes = trigrams.Count * 12L,
                lowerBoundTotalBytes =
                    trigramPostingBytes + trigrams.Count * 12L,
                wordCandidatePercentiles = new
                {
                    p50 = Percentile(trigramCounts, 0.50),
                    p90 = Percentile(trigramCounts, 0.90),
                    p99 = Percentile(trigramCounts, 0.99),
                    max = trigramCounts.Length == 0 ? 0 : trigramCounts[^1],
                },
            };
        }

        stopwatch.Stop();
        return new
        {
            blueprints = db.BlueprintsInOrder.Count,
            words = db.IndexByWord.Count,
            postings,
            profileMs = stopwatch.Elapsed.TotalMilliseconds,
            lowerBoundBytes = new
            {
                currentPostingInts = postings * sizeof(int),
                deltaVarintPostings = deltaVarintBytes,
                wordUtf8Bytes,
                wordUtf16Bytes,
            },
            exactPostingCandidatePercentiles = new
            {
                p50 = Percentile(postingCounts, 0.50),
                p90 = Percentile(postingCounts, 0.90),
                p99 = Percentile(postingCounts, 0.99),
                max = postingCounts.Length == 0 ? 0 : postingCounts[^1],
            },
            largestPostings,
            termProfiles,
            trigramEstimate,
        };
    }

    private static int VarintSize(uint value)
    {
        int size = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            size++;
        }
        return size;
    }

    private static int Percentile(int[] sorted, double percentile)
    {
        if (sorted.Length == 0)
            return 0;
        int index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private sealed class TrigramStats
    {
        public int Words { get; private set; }
        public long DeltaVarintBytes { get; private set; }
        private int lastWord = -1;

        public void AddWord(int word)
        {
            uint delta = (uint)(lastWord < 0 ? word : word - lastWord);
            DeltaVarintBytes += VarintSize(delta);
            Words++;
            lastWord = word;
        }
    }
}
