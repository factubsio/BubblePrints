using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintExplorer
{
    public struct MatchSpan
    {
        public UInt16 From;
        public UInt16 Length;

        public MatchSpan(int start)
        {
            From = (ushort)start;
            Length = 1;
        }
        public void End(int end)
        {
            Length = (UInt16)(end - From);
        }
    }

    public class FuzzyMatchResult<T>
    {
        public FuzzyMatchContext<T> Context;

        public T Handle;
        public string Haystack;

        public List<MatchSpan> matches = new();
        public int MatchedCharacters;
        public int BestRun;
        public int SingleRuns;
        public int TotalMatched;
        public float Penalty;
        public float Bonus;
        public float MatchRatio => TotalMatched / (float)Context.Needle.Length;
        public float HaystackMatched;
        public int GoodRuns => matches.Count(x => x.Length > 2);

        public float Score => (HaystackMatched * MatchRatio * 1.0f) + (BestRun * 4) + (GoodRuns * 2) - Penalty + Bonus;

        public FuzzyMatchResult(FuzzyMatchContext<T> context, string haystack, T handle)
        {
            this.Context = context;
            Haystack = haystack;
            Handle = handle;
        }

    }
    public class FuzzyMatchContext<T>
    {
        private static FuzzyMatchResult<T> FuzzyMatch(FuzzyMatchResult<T> result)
        {
            //eye indexes needle, thread indexes haystack.
            int eye = 0;
            int thread = -1;

            var needle = result.Context.Needle;
            var haystack = result.Haystack;

            //find a common prefix if any, so n:cat h:catsgrace is better than n:cat h:blahcatsgrace
            thread = haystack.IndexOf(needle[eye]);
            if (thread == 0)
                result.Bonus = 2.0f;

            //penalise matches that don't have a common prefix, while increasing eye(needle) and thread(haystack) to the first match, so:
            //n:bOb  h:hellOworldbob
            //   ^         ^
            while (thread == -1 && eye < needle.Length)
            {
                if (eye == 0)
                    result.Penalty = 2;
                else
                    result.Penalty += result.Penalty * .5f;
                thread = haystack.IndexOf(needle[eye]);
                eye++;
            }

            //Continue to match the next eye(needle) greedily to haystack
            while (eye < needle.Length)
            {

                //find the next point in haystack that matches our eye:
                //n:bOb h:helloworldBob
                //    ^             ^
                thread = haystack.IndexOf(needle[eye], thread);
                if (thread == -1)
                    break;

                //continue matching while both are in sync
                MatchSpan span = new MatchSpan(thread);
                while (thread < haystack.Length && eye < needle.Length && needle[eye] == haystack[thread])
                {
                    //if this span is rooted at the start of the word give a bonus because start is most importatn
                    if (span.From == 0 && eye > 0)
                        result.Bonus += result.Bonus;
                    eye++;
                    thread++;
                }

                //record the end of the span
                span.End(thread);
                result.matches.Add(span);

                //update some stats that get used for scoring
                if (span.Length > result.BestRun)
                    result.BestRun = span.Length;
                if (span.Length == 1)
                    result.SingleRuns++;
                result.TotalMatched += span.Length;
            }
            result.HaystackMatched = result.TotalMatched / (float)haystack.Length;
            return result;
        }

        public FuzzyMatchContext(string needle)
        {
            Needle = needle;
        }

        public FuzzyMatchResult<T> Match(string haystack, T handle)
        {
            return FuzzyMatch(new FuzzyMatchResult<T>(this, haystack, handle));
        }


        public string Needle;

    }
}
