using RingingBloom;
using RingingBloom.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlueprintExplorer.Sound
{

    public class BankAction
    {
        public bool IsPlay;
        public UInt32 TargetId;

        public BankAction(bool isPlay, uint target)
        {
            IsPlay = isPlay;
            TargetId = target;
        }
    }
    public class BankSequence
    {
        private SoundBank Bank;
        public readonly UInt32[] Sounds;

        public BankSequence(SoundBank bank ,uint[] sounds)
        {
            Bank = bank;
            Sounds = sounds;
        }

    }

    public class BankSound
    {
        public readonly UInt32 WemId;
        public BankSound(UInt32 wem)
        {
            WemId = wem;
        }
    }

    public class BankEvent
    {
        public readonly SoundBank Bank;
        public readonly UInt32[] Actions;

        public BankEvent(SoundBank newBank, UInt32[] actions)
        {
            Bank = newBank;
            Actions = actions;
        }

        public bool HasTarget => Actions.Length > 0;

        public UInt32 TargetId => Bank.Actions[Actions.First()].TargetId;

        public IEnumerable<UInt32> AsSequenceOfWEM
        {
            get
            {
                if (!Bank.Sequences.TryGetValue(TargetId, out var seq))
                {
                    return Enumerable.Empty<UInt32>();
                }

                return seq.Sounds.Select(soundId => Bank.Sounds[soundId].WemId);
            }
        }

        public override string ToString()
        {
            return HasTarget ? TargetId.ToString() : "<invalid>";
        }

    }
    public class SoundBank
    {
        private Dictionary<string, BankEvent> EventsByName = new();
        public Dictionary<UInt32, BankEvent> Events = new();
        public Dictionary<UInt32, BankSequence> Sequences = new();
        public Dictionary<UInt32, BankSound> Sounds = new();
        public Dictionary<UInt32, BankAction> Actions = new();
        public Dictionary<UInt32, HIRCTypes> ObjectTypes = new();
        public HashSet<UInt32> WemFiles = new();

        public bool TryGetBarks(string name, out IEnumerable<UInt32> barks)
        {
            if (TryGetEvent(name, out var evt))
            {
                barks = evt.AsSequenceOfWEM;
                return true;
            }
            else
            {
                barks = null;
                return false;
            }

        }

        public bool TryGetEvent(string name, out BankEvent evt)
        {
            evt = null;
            if (!EventsByName.TryGetValue(name, out evt))
            {
                var id = FNVHash(name);
                if (!Events.TryGetValue(id, out evt))
                    return false;
                EventsByName[name] = evt;
            }

            return true;
        }

        public string Name;

        public static UInt32 FNVHash(string str)
        {
            const UInt32 OffsetBasis = 0x811c9dc5;
            const UInt32 Prime = 0x1000193;

            var rawStr = Encoding.ASCII.GetBytes(str.ToLower());

            var hash0 = OffsetBasis;

            foreach (byte b in rawStr)
            {
                hash0 = hash0 * Prime ^ (UInt32)b;
            }

            return hash0;
        }
    }

    public static class SoundManager
    {
        private static Dictionary<string, SoundBank> SoundBanks = new();

        public static bool TryGetBank(string name, out SoundBank bank)
        {
            if (!SoundBanks.TryGetValue(name, out bank))
            {
                var path = Path.Combine(BubblePrints.WrathPath, @"Wrath_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/", name + ".bnk");

                if (!File.Exists(path))
                    return false;

                var newBank = new SoundBank();

                using (var file = File.OpenRead(path))
                {
                    using var reader = new BinaryReader(file);

                    var bnk = new NBNKFile(reader, RingingBloom.Common.SupportedGames.None);

                    foreach (var kv in bnk.ObjectHierarchy.all)
                    {
                        newBank.ObjectTypes[kv.Key] = kv.Value.Type;
                    }

                    foreach (var wem in bnk.DataIndex.wemList)
                    {
                        newBank.WemFiles.Add(wem.id);
                    }

                    foreach (var sound in bnk.ObjectHierarchy.wwiseObjects[HIRCTypes.Sound].Values)
                    {
                        newBank.Sounds[sound.Id] = new BankSound(sound.Sound.TargetId);
                    }

                    foreach (var seq in bnk.ObjectHierarchy.wwiseObjects[HIRCTypes.RandomSequence].Values)
                    {
                        newBank.Sequences[seq.Id] = new BankSequence(newBank, seq.Sequence.Elements);
                    }

                    foreach (var obj in bnk.ObjectHierarchy.wwiseObjects[HIRCTypes.Action].Values)
                    {
                        if (obj.EventAction.type == RingingBloom.Common.HIRC3ActionType.Play)
                        {
                            newBank.Actions[obj.Id] = new BankAction(true, obj.EventAction.TargetId);
                        }
                    }

                    foreach (var obj in bnk.ObjectHierarchy.wwiseObjects[HIRCTypes.Event].Values)
                    {
                        newBank.Events[obj.Id] = new BankEvent(newBank, obj.Event.ulActionIDs.ToArray());
                    }
                }

                bank = newBank;
                SoundBanks[name] = newBank;
            }
            return true;

        }

        private static string testBnk = @"D:\WOTR-1.1-DEBUG\Wrath_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows\PC_Female_Confident_GVR_ENG.bnk";
        public static void GetWEMIds()
        {
            List<string> barks = new()
            {
                "PC_Female_Confident_CombatStart_01",
                "PC_Female_Confident_CombatStart_02",
                "PC_Female_Confident_CombatStart_03",
                "PC_Female_Confident_Pain",
                "PC_Female_Confident_Fatigue",
                "PC_Female_Confident_Death",
                "PC_Female_Confident_Unconscious",
                "PC_Female_Confident_LowHealth_01",
                "PC_Female_Confident_LowHealth_02",
                "PC_Female_Confident_CharCrit_01",
                "PC_Female_Confident_CharCrit_02",
                "PC_Female_Confident_CharCrit_03",
                "PC_Female_Confident_AttackOrder_01",
                "PC_Female_Confident_AttackOrder_02",
                "PC_Female_Confident_AttackOrder_03",
                "PC_Female_Confident_AttackOrder_04",
                "PC_Female_Confident_Move_01",
                "PC_Female_Confident_Move_02",
                "PC_Female_Confident_Move_03",
                "PC_Female_Confident_Move_04",
                "PC_Female_Confident_Move_05",
                "PC_Female_Confident_Move_06",
                "PC_Female_Confident_Move_07",
                "PC_Female_Confident_Select_01",
                "PC_Female_Confident_Select_02",
                "PC_Female_Confident_Select_03",
                "PC_Female_Confident_SelectJoke",
                "PC_Female_Confident_Select_04",
                "PC_Female_Confident_Select_05",
                "PC_Female_Confident_Select_06",
                "PC_Female_Confident_CantEquip_01",
                "PC_Female_Confident_CantEquip_02",
                "PC_Female_Confident_CantCast",
                "PC_Female_Confident_CheckSuccess_01",
                "PC_Female_Confident_CheckSuccess_02",
                "PC_Female_Confident_CheckFail_01",
                "PC_Female_Confident_CheckFail_02",
                "PC_Female_Confident_Discovery_01",
                "PC_Female_Confident_Discovery_02",
                "PC_Female_Confident_SteatlhMode",
                "PC_Female_Confident_AttackShort",
                "PC_Female_Confident_CoupDeGrace"
            };

        }

        internal static void Debug()
        {
            BubblePrints.SetupLogging();
            SoundManager.TryGetBank("PC_Female_Confident_GVR_ENG", out var bank);
            bank.TryGetBarks("PC_Female_Confident_Fatigue", out var barks);
            Console.WriteLine("Hello");
        }
    }
}
