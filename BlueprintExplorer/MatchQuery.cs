using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace BlueprintExplorer {
    public interface ISearchable {
        Dictionary<string, Func<string>> Providers { get; }     // named functions to extract different text out of the target
        MatchResult[] Matches { get; }   // place to store search results
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

        public string Key;
        public ISearchable Target;
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
        public float MatchRatio;
        public float TargetRatio;
        public int GoodRuns;

        public float Score;
        public override string ToString() {
            //             Score = (TargetRatio * MatchRatio * TotalMatched * 4) + (BestRun * 4) + (GoodRuns * 2) - Penalty + Bonus;
            var fuzzyText = IsFuzzy ? "F" : "";
            var cleanText = IsClean ? "C" : "";
            var matchText = IsMatch ? "M" : "";
            var scoreText = $"score:{Score} = {TargetRatio * MatchRatio * TotalMatched * 4:0.00} ({TargetRatio:0.00}*{MatchRatio:0.00}*{TargetRatio:0.00}*{TotalMatched}*4) + {BestRun * 4} ({BestRun}*4) + {GoodRuns * 2}({GoodRuns}*2) - penalty({Penalty}) + bonus({Bonus})";
var result = base.GetType().Name + $" - {Context.SearchText} vs {Text} --> {scoreText} - key:{Key} <{string.Join("", matchText, cleanText, fuzzyText)}>";
            return result;
        }

        public MatchResult(string key, ISearchable target) {
            Key = key;
            Target = target;
            Clean();
        }
        public void Clean() {
            if (IsClean)
                return;
            IsClean = true;
            Text = null;
            Context = null;
            IsFuzzy = false;
            span.Start(0);
            MatchedCharacters = 0;
            BestRun = 0;
            SingleRuns = 0;
            TotalMatched = 0;
            Penalty = 0f;
            Bonus = 0f;
            MatchRatio = 0f;
            TargetRatio = 0f;
            GoodRuns = 0;
            Score = 0;
        }

        public void Reuse(string text, MatchQuery context, bool isFuzzy = true) {
            Text = text;
            Context = context;
            IsClean = false;
            IsFuzzy = isFuzzy;
        }
        public void AddSpan(int start, int length) {
            //update some stats that get used for scoring
            if (span.Length > BestRun) {
                BestRun += 1;
                this.span.Start(start, length);
            }
            if (length == 1)
                SingleRuns++;
            if (length > 2)
                GoodRuns++;
            TotalMatched += length;
        }
        public void Recalculate(string text) {
            TargetRatio = TotalMatched / (float)text.Length;
            MatchRatio = TotalMatched / (float)Context.SearchText.Length;
            Score = (TargetRatio * MatchRatio * TotalMatched * 4) + (BestRun * 4) + (GoodRuns * 2) - Penalty + Bonus;
        }
    }
    public class MatchQuery {
        public string SearchText;                                   // general search text which will be fuzzy and pass if any matches
        public Dictionary<string, string> StrictSearchTexts;        // these are specific keys such as type: and these will be evaluated with AND logic
        private MatchResult Match(string searchText, string text, MatchResult result) {
            result.Reuse(text, this, false);
            if (searchText.Length == 0) {
                // empty strict searches will select all
                result.AddSpan(0, text.Length);
                result.Recalculate(text);
                return result;
            }
            if (searchText.Length == 1) result.Bonus = 10; // We want to let matches to single char search strings show up
            var index =  text.IndexOf(searchText);
            if (index >= 0) {
                result.AddSpan(index, searchText.Length);
                result.Recalculate(text);
            }
            return result;
        }
        private void FuzzyMatch(string text, MatchResult result) {
            result.Reuse(text, this);

            int searchTextIndex = 0;
            int targetIndex = -1;

            var searchText = result.Context.SearchText;

            // find a common prefix if any, so n:cat h:catsgrace is better than n:cat h:blahcatsgrace
            targetIndex = text.IndexOf(searchText[searchTextIndex]);
            if (targetIndex == 0) result.Bonus = 2.0f;
            if (searchText.Length == 1)result.Bonus += 10; // We want to let matches to single char search strings show up

            // penalise matches that don't have a common prefix, while increasing searchTextIndex and targetIndex to the first match, so:
            // n:bOb  h:hellOworldbob
            //    ^         ^
            while (targetIndex == -1 && searchTextIndex < searchText.Length) {
                if (searchTextIndex == 0)
                    result.Penalty = 2;
                else
                    result.Penalty += result.Penalty * .5f;
                targetIndex = text.IndexOf(searchText[searchTextIndex]);
                searchTextIndex++;
            }
            // continue to match the next searchTextIndex greedily in target
            while (searchTextIndex < searchText.Length) {
                // find the next point in target that matches searchIndex:
                // n:bOb h:helloworldBob
                //     ^             ^
                targetIndex = text.IndexOf(searchText[searchTextIndex], targetIndex);
                if (targetIndex == -1)
                    break;

                //continue matching while both are in sync
                var spanFrom = targetIndex;
                while (targetIndex < text.Length && searchTextIndex < searchText.Length && searchText[searchTextIndex] == text[targetIndex]) {
                    //if this span is rooted at the start of the word give a bonus because start is most importatn
                    if (spanFrom == 0 && searchTextIndex > 0)
                        result.Bonus += 2 * searchTextIndex;
                    searchTextIndex++;
                    targetIndex++;
                }
                var span = targetIndex - spanFrom;
                //record the end of the span
                result.AddSpan(spanFrom, span);
                result.Bonus += span * (span + 1) / 2 + span; // give a bonus for span size
                if (span == searchText.Length)
                    result.Bonus *= 2f;
            }

            result.Recalculate(text);
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
            var matches = searchable.CleanedMatches();
            if (SearchText?.Length > 0 || StrictSearchTexts.Count > 0) {
                bool foundRestricted = false;
                foreach (var match in matches) { 
                    var key = match.Key;
                    var text = searchable.Providers[key]();
                    foreach (var entry in StrictSearchTexts) {
                        if (key.StartsWith(entry.Key)) {
                            Match(entry.Value, text, match);
                            foundRestricted = key == "name";
                            break;
                        }
                    }
                }
                if (!foundRestricted && SearchText?.Length > 0) {
                    FuzzyMatch(searchable.Providers["name"](), matches[0]);
                }
            }
            return searchable;
        }
    }
    public static class MatchHelpers {
        public static bool HasMatches(this ISearchable searchable) {
            if (searchable.Matches == null)
                return true;
            var fuzzyCount = 0;
            var strictFailures = 0;
            var strictMatches = 0;
            foreach (var match in searchable.Matches) {
                if (match.IsFuzzy) {
                    if (match.Score >= 10)
                        fuzzyCount += 1;
                }
                else {
                    if (match.TotalMatched > 0)
                        strictMatches += 1;
                    else if (!match.IsClean)
                        strictFailures += 1;
                }
            }
            return (fuzzyCount >= 1 || strictMatches > 0) && strictFailures == 0;
        }
        public static float Score(this ISearchable searchable) {
            if (searchable.Matches == null)
                return float.PositiveInfinity;
            var score = 0f;
            foreach (var match in searchable.Matches) {
                score += match.Score;
            //    if (match.Score > 50) {
            //        System.Console.WriteLine($"{match}");
            //    }
            }
            return score;
        }
        public static MatchResult[] CleanedMatches(this ISearchable searchable) {
            var matches = searchable.Matches;
            // cleanup old match results
            var count = matches.Length;
            for (var ii = 0; ii<count; ii++)
                matches[ii].Clean();
            return matches;
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
