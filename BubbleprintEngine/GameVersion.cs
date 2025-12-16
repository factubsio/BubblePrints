using System;

namespace BlueprintExplorer
{
    public partial class BlueprintDB
    {
        public struct GameVersion : IComparable<GameVersion>
        {
            public int Major, Minor, Patch;
            public string Suffix;
            public int Bubble;

            public GameVersion(int major, int minor, int patch, string suffix, int bubble)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
                Suffix = suffix;
                Bubble = bubble;
            }

            public int CompareTo(GameVersion other)
            {
                int c = Major.CompareTo(other.Major);
                if (c != 0) return c;

                c = Minor.CompareTo(other.Minor);
                if (c != 0) return c;

                c = Patch.CompareTo(other.Patch);
                if (c != 0) return c;

                c = Suffix.CompareTo(other.Suffix);
                if (c != 0) return c;

                return Bubble.CompareTo(other.Bubble);
            }

            public override bool Equals(object obj) => obj is GameVersion version && Major == version.Major && Minor == version.Minor && Patch == version.Patch && Suffix == version.Suffix && Bubble == version.Bubble;
            public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Suffix, Bubble);


            public override string ToString()
            {
                if (int.TryParse(Suffix, out var _))
                {
                    return $"{Major}.{Minor}.{Patch}.{Suffix}_{Bubble}";
                }
                else
                {
                    return $"{Major}.{Minor}.{Patch}{(Suffix == default ? "" : Suffix)}_{Bubble}";
                }
            }
        }
    }
}
