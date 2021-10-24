using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace BlueprintExplorer {
    public interface ISearchable {
        Dictionary<string, Func<string>> Providers { get; }     // named functions to extract different text out of the target
        Dictionary<string, MatchResult> Matches { get; set; }   // place to store search results
    }
    public static class MatchHelpers {
        public static bool HasMatches(this ISearchable searchable) {
            if (searchable.Matches == null)
                return true;
            var fuzzyCount = 0;
            var strictFailures = 0;
            foreach (var entry in searchable.Matches) {
                var match = entry.Value;
                if (match.IsFuzzy)
                    fuzzyCount += 1;
                else if (!match.IsMatch)
                    strictFailures += 1;
            }
            return fuzzyCount >= 1 && strictFailures == 0;
        }
        public static float Score(this ISearchable searchable) {
            if (searchable.Matches == null)
                return float.PositiveInfinity;
            var score = 0f;
            foreach (var entry in searchable.Matches) {
                var match = entry.Value;
                score += match.Score;
            }
            return score;
        }
    }
    public class MatchResult {
        public struct Span {
            public UInt16 From;
            public UInt16 Length;
            public void Start(int start, int length = -1) {
                From = (ushort)start;
                Length = (ushort)length;
            }
            public void End(int end) {
                Length = (UInt16)(end - From);
            }
        }

        public ISearchable Target;
        public string Key;
        public string Text;
        public MatchQuery Context;
        public bool IsFuzzy;
        public bool IsClean;

        public bool IsMatch => TotalMatched > 0 || IsClean;
        public Span span; // Keep 1 span here for convenience. Can recompute other spans in UI
        public int MatchedCharacters;
        public int BestRun;
        public int SingleRuns;
        public int TotalMatched;
        public float Penalty;
        public float Bonus;
        public float MatchRatio => TotalMatched / (float)Context.SearchText.Length;
        public float TargetRatio;
        public int GoodRuns;

        public float Score;

        public void Reuse(ISearchable target, string key, string text, MatchQuery context, bool isFuzzy = true) {
            Target = target;
            Key = key;
            Text = text;
            Context = context;
            IsClean = false;
            IsFuzzy = isFuzzy;
            span.Start(0);
            MatchedCharacters = 0;
            BestRun = 0;
            SingleRuns = 0;
            TotalMatched = 0;
            Penalty = 0f;
            Bonus = 0f;
            TargetRatio = 0f;
            GoodRuns = 0;
        }
        public void AddSpan(int start, int length) {
            //update some stats that get used for scoring
            if (span.Length > BestRun) {
                BestRun = length;
                this.span.Start(start, length);
            }
            if (length == 1)
                SingleRuns++;
            if (length > 2)
                GoodRuns++;
            TotalMatched += length;
        }
        public void Recalculate() {
            Score = (TargetRatio * MatchRatio * 1.0f) + (BestRun * 4) + (GoodRuns * 2) - Penalty + Bonus;
        }
    }
    public class MatchQuery {
        public string SearchText;                                   // general search text which will be fuzzy and pass if any matches
        public Dictionary<string, string> StrictSearchTexts;        // these are specific keys such as type: and these will be evaluated with AND logic
        private MatchResult Match(string searchText, ISearchable searchable, string key, string text, MatchResult result) {
            result.Reuse(searchable, key, text, this, false);
            var index = text.IndexOf(searchText);
            if (index >= 0) {
                result.AddSpan(index, searchText.Length);
                result.TargetRatio = result.TotalMatched / (float)text.Length;
            }
            return result;
        }
        private void FuzzyMatch(ISearchable searchable, string key, string text, MatchResult result) {
            result.Reuse(searchable, key, text, this);

            int searchTextIndex = 0;
            int targetIndex = -1;

            var searchText = result.Context.SearchText;
            var target = result.Text;

            // find a common prefix if any, so n:cat h:catsgrace is better than n:cat h:blahcatsgrace
            targetIndex = target.IndexOf(searchText[searchTextIndex]);
            if (targetIndex == 0)
                result.Bonus = 2.0f;

            // penalise matches that don't have a common prefix, while increasing searchTextIndex and targetIndex to the first match, so:
            // n:bOb  h:hellOworldbob
            //    ^         ^
            while (targetIndex == -1 && searchTextIndex < searchText.Length) {
                if (searchTextIndex == 0)
                    result.Penalty = 2;
                else
                    result.Penalty += result.Penalty * .5f;
                targetIndex = target.IndexOf(searchText[searchTextIndex]);
                searchTextIndex++;
            }

            // continue to match the next searchTextIndex greedily in target
            while (searchTextIndex < searchText.Length) {
                // find the next point in target that matches searchIndex:
                // n:bOb h:helloworldBob
                //     ^             ^
                targetIndex = target.IndexOf(searchText[searchTextIndex], targetIndex);
                if (targetIndex == -1)
                    break;

                //continue matching while both are in sync
                var spanFrom = targetIndex;
                while (targetIndex < target.Length && searchTextIndex < searchText.Length && searchText[searchTextIndex] == target[targetIndex]) {
                    //if this span is rooted at the start of the word give a bonus because start is most importatn
                    if (spanFrom == 0 && searchTextIndex > 0)
                        result.Bonus += result.Bonus;
                    searchTextIndex++;
                    targetIndex++;
                }

                //record the end of the span
                result.AddSpan(spanFrom, targetIndex - spanFrom);
            }
            result.TargetRatio = result.TotalMatched / (float)target.Length;
            result.Recalculate();
        }

        public MatchQuery(string queryText) {
            var unrestricted = new List<string>();
            StrictSearchTexts = new();
            var terms = queryText.Split(' ');
            foreach (var term in terms) {
                if (term.Contains(':')) {
                    var pair = term.Split(':');
                    StrictSearchTexts[pair[0]] = pair[1];
                }
                else
                    unrestricted.Add(term);
            }
            SearchText = string.Join(' ', unrestricted);
        }

        public ISearchable Evaluate(ISearchable searchable) {
            if (SearchText?.Length > 0 || StrictSearchTexts.Count > 0) {
                var matches = searchable.Matches;
                if (matches == null)
                    matches = searchable.Matches = new();
                bool foundRestricted = false;
                foreach (var provider in searchable.Providers) {
                    var key = provider.Key;
                    if (!matches.TryGetValue(key, out var matchResult)) {
                        matchResult = new();
                        matches[key] = matchResult;
                    }
                    matchResult.IsClean = true; // important to set this as it will get cleared during the matching process if it is called
                    var text = provider.Value();
                    foreach (var entry in StrictSearchTexts) {
                        if (key.StartsWith(entry.Key)) {
                            Match(entry.Value, searchable, key, text, matchResult);
                            foundRestricted = key == "name";
                            break;
                        }
                    }
                }
                if (!foundRestricted && SearchText?.Length > 0) {
                    if (!matches.TryGetValue("name", out var matchResult)) {
                        matchResult = new();
                        matches["name"] = matchResult;
                    }
                    matchResult.IsClean = true; // important to set this as it will get cleared during the matching process if it is called
                    FuzzyMatch(searchable, "name", searchable.Providers["name"](), matchResult);
                }
            }
            else
                searchable.Matches = null;
            return searchable;
        }
    }

#if false
    public static class FuzzyMatcher {
        public static IEnumerable<T> FuzzyMatch<T>(this IEnumerable<(T, string)> input, string needle, float scoreThreshold = 10) {
            var result = new MatchQuery<T>(needle.ToLower());
            return input.Select(i => result.Match(i.Item2, i.Item1)).Where(match => match.Score > scoreThreshold).OrderByDescending(match => match.Score).Select(m => m.Handle);
        }
        /// <summary>
        /// Fuzzy Match all items in input against the needle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">input items</param>
        /// <param name="haystack">function to get the 'string' key of an input item to match against</param>
        /// <param name="needle">value to match against</param>
        /// <param name="scoreThreshold">discard all results under this score (default: 10)</param>
        /// <returns>An IEnumerable<out T> that contains elements from the input sequence that score above the threshold, sorted by score</out></returns>
        public static IEnumerable<T> FuzzyMatch<T>(this IEnumerable<T> input, Func<T, string> haystack, string needle, float scoreThreshold = 10) {
            return input.Select(i => (i, haystack(i))).FuzzyMatch(needle, scoreThreshold);
        }


        public class ExampleType {
            int Foo;
            string Bar;

            public string Name => $"{Bar}.{Foo}";
        }

        public static void Example() {
            //Assume some input list (or enumerable)
            List<ExampleType> inputList = new();

            //Get an enumerable of all the matches (above a score threshold, default = 10)
            var matches = inputList.FuzzyMatch(type => type.Name, "string_to_search");

            //Get top 20 results
            var top20 = matches.Take(20).ToList();
        }

    }
#endif
}
