using Server;
using Server.Mobiles;
using Server.Items;
using Server.Spells;
using Server.Network;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Mobiles
{
    public class CityBots : BaseCreature
    {
        private string m_BotTitle;
        private bool m_HasMount;
        private string m_BotClass;
        private Point3D m_PrimaryLocation;
        private Point3D m_AlternateLocation;
        private Timer m_SpellTimer;
        private Timer m_MovementTimer;
        private Timer m_SpeechTimer;
        private Timer m_SkillTimer;
        private BaseCreature m_SummonedCreature;
        private int m_SummonCount;
        private Dictionary<string, DateTime> m_SpellCooldowns;
        private static Dictionary<string, List<string>> m_CityChats = new Dictionary<string, List<string>>();
        private int m_Mana;
        private bool m_MovingToAlternate;
        private DateTime m_LastSpellCast;

        private static readonly Dictionary<string, string> SpellWords = new Dictionary<string, string>
        {
            { "SummonElemental", "Kal Vas Xen" },
            { "Reactive Armor", "Flam Sanct" },
            { "Agility", "Ex Uus" },
            { "Strength", "Uus Sanct" },
            { "Protection", "In Sanct" },
            { "Bless", "Rel Sanct" },
            { "Cure", "An Nox" },
            { "Greater Heal", "In Vas Mani" },
            { "Heal", "In Mani" },
            { "Nightsight", "In Lor" },
            { "Cunning", "Uus Wis" },
            { "SummonCreature", "Kal Xen" }
        };

        public CityBots(string name, string title, bool female, bool hasMount, string botClass, Point3D primaryLoc, Point3D alternateLoc)
            : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = name;
            Title = title;
            Female = female;
            m_HasMount = hasMount;
            m_BotClass = botClass;
            m_PrimaryLocation = primaryLoc;
            m_AlternateLocation = alternateLoc;
            m_SummonCount = 0;
            m_SpellCooldowns = new Dictionary<string, DateTime>();
            m_Mana = 100;
            m_MovingToAlternate = Utility.RandomBool();
            m_LastSpellCast = DateTime.MinValue;

            Home = primaryLoc;
            RangeHome = 0;

            InitBody();
            InitOutfit();
            if (hasMount)
                AddMount();

            m_SpellTimer = new SpellTimer(this);
            m_MovementTimer = new MovementTimer(this);
            m_SpeechTimer = new SpeechTimer(this);
            m_SkillTimer = new SkillTimer(this);
            m_SpellTimer.Start();
            m_MovementTimer.Start();
            m_SpeechTimer.Start();
            m_SkillTimer.Start();
        }

        public override void OnThink()
        {
        }

        private void InitBody()
        {
            Body = Female ? 0x191 : 0x190;
            Hue = Utility.RandomSkinHue();
            HairItemID = Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2046, 0x2047);
            HairHue = Utility.RandomHairHue();
            NameHue = 0x35;
        }

        private void InitOutfit()
        {
            switch (m_BotClass.ToLower())
            {
                case "mage":
                    AddItem(new Robe(Utility.RandomNeutralHue()));
                    AddItem(new WizardsHat(Utility.RandomNeutralHue()));
                    AddItem(new Sandals());
                    break;
                case "swordsman":
                    AddItem(new PlateChest());
                    AddItem(new PlateArms());
                    AddItem(new PlateLegs());
                    AddItem(new Longsword());
                    break;
                case "fencer":
                    AddItem(new LeatherChest());
                    AddItem(new LeatherArms());
                    AddItem(new Kryss());
                    break;
                case "knight":
                    AddItem(new PlateChest());
                    AddItem(new PlateArms());
                    AddItem(new PlateLegs());
                    AddItem(new Halberd());
                    break;
                case "archer":
                    AddItem(new LeatherChest());
                    AddItem(new Bow());
                    AddItem(new LeatherLegs());
                    break;
                case "hunter":
                    AddItem(new LeatherChest());
                    AddItem(new Bow());
                    break;
                case "healer":
                    AddItem(new Robe(Utility.RandomYellowHue()));
                    AddItem(new Sandals());
                    break;
                case "alchemist":
                    AddItem(new Robe(Utility.RandomGreenHue()));
                    AddItem(new Sandals());
                    break;
                case "thief":
                    AddItem(new LeatherChest());
                    AddItem(new Dagger());
                    break;
                case "blacksmith":
                    AddItem(new HalfApron());
                    AddItem(new Hammer());
                    break;
                case "tailor":
                    AddItem(new FancyShirt());
                    AddItem(new SewingKit());
                    break;
            }
        }

        private void AddMount()
        {
            Horse horse = new Horse();
            horse.Controlled = true;
            horse.ControlMaster = this;
            horse.MoveToWorld(Location, Map);
        }

        public override void OnDelete()
        {
            if (m_MovementTimer != null)
            {
                m_MovementTimer.Stop();
                m_MovementTimer = null;
            }
            if (m_SpellTimer != null)
            {
                m_SpellTimer.Stop();
                m_SpellTimer = null;
            }
            if (m_SpeechTimer != null)
            {
                m_SpeechTimer.Stop();
                m_SpeechTimer = null;
            }
            if (m_SkillTimer != null)
            {
                m_SkillTimer.Stop();
                m_SkillTimer = null;
            }

            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseCreature creature && creature.ControlMaster == this && !creature.Deleted)
                {
                    toDelete.Add(creature);
                }
            }
            foreach (Mobile m in toDelete)
            {
                m.Delete();
            }

            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
            {
                m_SummonedCreature.Delete();
            }
            m_SummonedCreature = null;
            m_SummonCount = 0;

            base.OnDelete();
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);
            SayRandomCityChat();
        }

        private void SayRandomCityChat()
        {
            string city = Map.Name;
            if (m_CityChats.ContainsKey(city) && m_CityChats[city].Count > 0)
            {
                string chat = m_CityChats[city][Utility.Random(m_CityChats[city].Count)];
                Say(chat);
            }
        }

        private bool CanCastSpell(string spellName, int manaCost, TimeSpan cooldown)
        {
            if (m_Mana < manaCost)
            {
                return false;
            }

            if (m_SpellCooldowns.ContainsKey(spellName) && DateTime.UtcNow < m_SpellCooldowns[spellName])
            {
                return false;
            }

            m_Mana -= manaCost;
            m_SpellCooldowns[spellName] = DateTime.UtcNow + cooldown;
            m_LastSpellCast = DateTime.UtcNow;
            return true;
        }

        private bool CanCastSpellOnPlayer()
        {
            TimeSpan timeSinceLastCast = DateTime.UtcNow - m_LastSpellCast;
            TimeSpan minCooldown = TimeSpan.FromSeconds(Utility.RandomMinMax(60, 180));
            if (timeSinceLastCast < minCooldown)
            {
                return false;
            }
            return true;
        }

        private void RegenerateMana()
        {
            m_Mana = Math.Min(100, m_Mana + Utility.RandomMinMax(5, 10));
        }

        private class SpellTimer : Timer
        {
            private CityBots m_Bot;

            public SpellTimer(CityBots bot) : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 30), TimeSpan.FromSeconds(10))
            {
                m_Bot = bot;
            }

            protected override void OnTick()
            {
                if (m_Bot == null || m_Bot.Deleted)
                    return;

                m_Bot.RegenerateMana();

                if (!m_Bot.CanCastSpellOnPlayer())
                    return;

                switch (m_Bot.m_BotClass.ToLower())
                {
                    case "mage":
                        if (m_Bot.m_SummonCount < 1 && Utility.RandomDouble() < 0.5)
                        {
                            if (m_Bot.CanCastSpell("SummonElemental", 30, TimeSpan.FromMinutes(3)))
                            {
                                string[] summons = { "Ice Elemental", "Fire Elemental", "Earth Elemental" };
                                string summon = summons[Utility.Random(summons.Length)];
                                Type summonType = summon == "Ice Elemental" ? typeof(WaterElemental) :
                                    summon == "Fire Elemental" ? typeof(FireElemental) : typeof(EarthElemental);
                                BaseCreature elemental = (BaseCreature)Activator.CreateInstance(summonType);
                                elemental.Controlled = true;
                                elemental.ControlMaster = m_Bot;
                                Point3D summonLoc = m_Bot.GetSpawnLocation(2);
                                elemental.MoveToWorld(summonLoc, m_Bot.Map);
                                m_Bot.m_SummonedCreature = elemental;
                                m_Bot.m_SummonCount++;
                                new DespawnTimer(elemental, m_Bot).Start();
                                m_Bot.PlaySound(0x1F2);
                                m_Bot.Animate(17, 7, 1, true, false, 0);
                                Effects.SendLocationEffect(summonLoc, m_Bot.Map, 0x3728, 10, 10);
                                m_Bot.Say(SpellWords["SummonElemental"]);
                            }
                        }
                        else
                        {
                            Mobile target = m_Bot.GetSmartPlayerTarget(10);
                            if (target != null)
                            {
                                string[] spells = { "Reactive Armor", "Agility", "Strength" };
                                string spell = spells[Utility.Random(spells.Length)];
                                int manaCost = spell == "Reactive Armor" ? 20 : 15;
                                if (m_Bot.CanCastSpell(spell, manaCost, TimeSpan.FromMinutes(1)))
                                {
                                    m_Bot.PlaySound(0x1F2);
                                    m_Bot.Animate(17, 7, 1, true, false, 0);
                                    Effects.SendTargetEffect(target, 0x375A, 10, 10);
                                    target.SendMessage($"{m_Bot.Name} casts {spell} on you!");
                                    m_Bot.Say(SpellWords[spell]);
                                }
                            }
                        }
                        break;
                    case "healer":
                        Mobile healTarget = m_Bot.GetSmartPlayerTarget(10, true);
                        if (healTarget != null)
                        {
                            string[] spells = { "Protection", "Bless", "Cure", "Greater Heal" };
                            string spell = m_Bot.ChooseHealSpell(healTarget, spells);
                            int manaCost = spell == "Greater Heal" ? 25 : spell == "Cure" ? 15 : 10;
                            if (m_Bot.CanCastSpell(spell, manaCost, TimeSpan.FromMinutes(1)))
                            {
                                m_Bot.PlaySound(0x1F2);
                                m_Bot.Animate(17, 7, 1, true, false, 0);
                                Effects.SendTargetEffect(healTarget, 0x376A, 10, 10);
                                healTarget.SendMessage($"{m_Bot.Name} casts {spell} on you!");
                                m_Bot.Say(SpellWords[spell]);
                            }
                        }
                        break;
                    case "swordsman":
                    case "fencer":
                    case "knight":
                    case "archer":
                        string[] combatSpells = { "Heal", "Nightsight", "Cunning", "Bless" };
                        string combatSpell = combatSpells[Utility.Random(combatSpells.Length)];
                        if (m_Bot.CanCastSpell(combatSpell, 15, TimeSpan.FromMinutes(2)))
                        {
                            if (Utility.RandomDouble() < 0.3)
                            {
                                m_Bot.PlaySound(0x5C);
                                m_Bot.Animate(17, 7, 1, true, false, 0);
                                Effects.SendLocationEffect(m_Bot.Location, m_Bot.Map, 0x374A, 10, 16);
                            }
                            else
                            {
                                m_Bot.PlaySound(0x1F2);
                                m_Bot.Animate(17, 7, 1, true, false, 0);
                                Effects.SendTargetEffect(m_Bot, 0x375A, 10, 10);
                                m_Bot.Say(SpellWords[combatSpell]);
                            }
                        }
                        break;
                    case "hunter":
                        if (m_Bot.m_SummonCount < 2 && m_Bot.CanCastSpell("SummonCreature", 20, TimeSpan.FromMinutes(2)))
                        {
                            TimberWolf wolf = new TimberWolf();
                            wolf.Controlled = true;
                            wolf.ControlMaster = m_Bot;
                            Point3D spawnLoc = m_Bot.GetSpawnLocation(2);
                            wolf.MoveToWorld(spawnLoc, m_Bot.Map);
                            m_Bot.m_SummonedCreature = wolf;
                            m_Bot.m_SummonCount++;
                            new DespawnTimer(wolf, m_Bot).Start();
                            m_Bot.PlaySound(0x1F2);
                            m_Bot.Animate(17, 7, 1, true, false, 0);
                            Effects.SendLocationEffect(spawnLoc, m_Bot.Map, 0x3728, 10, 10);
                            m_Bot.Say(SpellWords["SummonCreature"]);
                        }
                        break;
                }
            }
        }

        private class SkillTimer : Timer
        {
            private CityBots m_Bot;
            private int m_RepeatCount;

            public SkillTimer(CityBots bot) : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 30), TimeSpan.FromSeconds(2))
            {
                m_Bot = bot;
                m_RepeatCount = Utility.RandomMinMax(1, 5);
            }

            protected override void OnTick()
            {
                if (m_Bot == null || m_Bot.Deleted)
                    return;

                if (m_RepeatCount > 0)
                {
                    switch (m_Bot.m_BotClass.ToLower())
                    {
                        case "alchemist":
                            m_Bot.PlaySound(0x242);
                            m_RepeatCount--;
                            break;
                        case "tailor":
                            m_Bot.PlaySound(0x248);
                            m_RepeatCount--;
                            break;
                        case "blacksmith":
                            m_Bot.PlaySound(0x2A);
                            m_Bot.Animate(11, 5, 1, true, false, 0);
                            m_RepeatCount--;
                            break;
                    }
                }
                else
                {
                    m_RepeatCount = Utility.RandomMinMax(1, 5);
                    Delay = TimeSpan.FromSeconds(Utility.RandomMinMax(60, 300));
                }
            }
        }

        private class MovementTimer : Timer
        {
            private CityBots m_Bot;
            private bool m_IsRunning;
            private Point3D m_TargetLocation;
            private bool m_Moving;

            public MovementTimer(CityBots bot) : base(TimeSpan.FromSeconds(Utility.RandomMinMax(20, 40)), TimeSpan.FromSeconds(1))
            {
                m_Bot = bot;
                m_IsRunning = Utility.RandomBool();
                m_TargetLocation = m_Bot.m_MovingToAlternate ? m_Bot.m_AlternateLocation : m_Bot.m_PrimaryLocation;
                m_Moving = false; // Start ohne sofortige Bewegung
            }

            protected override void OnTick()
            {
                if (m_Bot == null || m_Bot.Deleted)
                    return;

                if (m_Moving)
                {
                    // Prüfe, ob der Bot das Ziel erreicht hat
                    if (m_Bot.Location == m_TargetLocation)
                    {
                        // Ziel erreicht, Koordinaten tauschen
                        Point3D temp = m_Bot.m_PrimaryLocation;
                        m_Bot.m_PrimaryLocation = m_Bot.m_AlternateLocation;
                        m_Bot.m_AlternateLocation = temp;
                        m_Bot.m_MovingToAlternate = !m_Bot.m_MovingToAlternate;
                        m_Moving = false;
                        Delay = TimeSpan.FromSeconds(Utility.RandomMinMax(20, 40)); // Pause von 20–40 Sekunden
                        m_Bot.CurrentSpeed = m_IsRunning ? 0.2 : 0.4; // Geschwindigkeit zurücksetzen
                        return;
                    }

                    // Fortfahren mit der Bewegung
                    m_Bot.PathTo(m_TargetLocation, m_IsRunning ? 0.2 : 0.4);
                    return;
                }

                // Neue Bewegung starten
                m_IsRunning = Utility.RandomBool();
                m_TargetLocation = m_Bot.m_MovingToAlternate ? m_Bot.m_AlternateLocation : GetRandomNearbyLocation(m_Bot.m_PrimaryLocation);
                m_Bot.CurrentSpeed = m_IsRunning ? 0.2 : 0.4; // Schnell (0.2) oder langsam (0.4)
                m_Bot.PathTo(m_TargetLocation, m_Bot.CurrentSpeed);
                m_Moving = true;
                Delay = TimeSpan.FromSeconds(1); // Tick alle 1 Sekunde, um den Fortschritt zu prüfen
            }

            private Point3D GetRandomNearbyLocation(Point3D origin)
            {
                int distance = Utility.RandomMinMax(1, 6);
                for (int i = 0; i < 10; i++)
                {
                    int x = origin.X + Utility.RandomMinMax(-distance, distance);
                    int y = origin.Y + Utility.RandomMinMax(-distance, distance);
                    int z = m_Bot.Map.GetAverageZ(x, y);
                    Point3D loc = new Point3D(x, y, z);
                    if (m_Bot.Map.CanSpawnMobile(loc))
                        return loc;
                }
                return origin; // Fallback auf Ursprung, wenn kein gültiger Punkt gefunden
            }
        }

        private void PathTo(Point3D destination, double speed)
        {
            // ServUO Pathfinding-Simulation (kein direkter PathFollower in Standard-ServUO, daher vereinfacht)
            Direction dir = GetDirectionTo(Location, destination);
            if (Map.CanFit(Location, 16, false, false) && Move(dir))
            {
                Animate(CurrentSpeed == 0.2 ? 1 : 0, 5, 1, true, false, 0);
            }
            else
            {
                // Wenn Bewegung nicht möglich, versuche direkt zum Ziel zu springen (Fallback)
                if (Map.CanSpawnMobile(destination))
                {
                    MoveToWorld(destination, Map);
                    Animate(CurrentSpeed == 0.2 ? 1 : 0, 5, 1, true, false, 0);
                }
            }
        }

        private Direction GetDirectionTo(Point3D from, Point3D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);

            if (adx >= ady * 2)
            {
                return dx > 0 ? Direction.East : Direction.West;
            }
            else if (ady >= adx * 2)
            {
                return dy > 0 ? Direction.South : Direction.North;
            }
            else if (dx > 0 && dy > 0)
            {
                return Direction.Down;
            }
            else if (dx > 0 && dy < 0)
            {
                return Direction.Right;
            }
            else if (dx < 0 && dy > 0)
            {
                return Direction.Left;
            }
            else
            {
                return Direction.Up;
            }
        }

        private class SpeechTimer : Timer
        {
            private CityBots m_Bot;

            public SpeechTimer(CityBots bot) : base(TimeSpan.FromMinutes(Utility.RandomMinMax(2, 4)), TimeSpan.FromMinutes(Utility.RandomMinMax(2, 4)))
            {
                m_Bot = bot;
            }

            protected override void OnTick()
            {
                if (m_Bot == null || m_Bot.Deleted)
                    return;

                m_Bot.SayRandomCityChat();
            }
        }

        private class DespawnTimer : Timer
        {
            private BaseCreature m_Summon;
            private CityBots m_Bot;

            public DespawnTimer(BaseCreature summon, CityBots bot) : base(TimeSpan.FromMinutes(2))
            {
                m_Summon = summon;
                m_Bot = bot;
            }

            protected override void OnTick()
            {
                if (m_Summon != null && !m_Summon.Deleted)
                {
                    m_Summon.Delete();
                }
                if (m_Bot != null && !m_Bot.Deleted)
                {
                    m_Bot.m_SummonCount--;
                    m_Bot.m_SummonedCreature = null;
                }
                Stop();
            }
        }

        private string ChooseHealSpell(Mobile target, string[] spells)
        {
            if (target.Poisoned)
                return "Cure";
            if (target.Hits < target.HitsMax * 0.5)
                return "Greater Heal";
            return spells[Utility.Random(spells.Length)];
        }

        private Mobile GetSmartPlayerTarget(int range, bool prioritizeInjured = false)
        {
            List<Mobile> candidates = new List<Mobile>();
            foreach (Mobile m in GetMobilesInRange(range))
            {
                if (m is PlayerMobile && m != this && m.Alive)
                    candidates.Add(m);
            }

            if (candidates.Count == 0)
                return null;

            if (prioritizeInjured)
            {
                var injured = candidates.Find(m => m.Hits < m.HitsMax * 0.5 || m.Poisoned);
                if (injured != null)
                    return injured;
            }

            return candidates[Utility.Random(candidates.Count)];
        }

        private Point3D GetSpawnLocation(int range)
        {
            for (int i = 0; i < 10; i++)
            {
                int x = Location.X + Utility.RandomMinMax(-range, range);
                int y = Location.Y + Utility.RandomMinMax(-range, range);
                int z = Map.GetAverageZ(x, y);
                Point3D loc = new Point3D(x, y, z);
                if (Map.CanSpawnMobile(loc))
                    return loc;
            }
            return Location;
        }

        public static void LoadCityBots()
        {
            string path = Path.Combine(Core.BaseDirectory, "Data/CityBots.cfg");
            if (!File.Exists(path))
            {
                return;
            }

            int spawnedCount = 0;

            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is CityBots)
                {
                    toDelete.Add(m);
                }
            }

            foreach (Mobile m in toDelete)
            {
                m.Delete();
            }

            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length < 8)
                    {
                        continue;
                    }

                    try
                    {
                        string name = parts[0].Trim();
                        string title = parts[1].Trim();
                        bool female = bool.Parse(parts[2].Trim());
                        bool hasMount = bool.Parse(parts[3].Trim());
                        string botClass = parts[4].Trim();
                        Point3D primaryLoc = ParsePoint3D(parts[5].Trim());
                        Point3D alternateLoc = ParsePoint3D(parts[6].Trim());
                        string mapName = parts[7].Trim();

                        Map map = null;
                        if (string.Equals(mapName, "Felucca", StringComparison.OrdinalIgnoreCase))
                            map = Map.Felucca;
                        else if (string.Equals(mapName, "Trammel", StringComparison.OrdinalIgnoreCase))
                            map = Map.Trammel;
                        else
                            map = Map.Parse(mapName);

                        if (map == null || map == Map.Internal)
                        {
                            continue;
                        }

                        if (!map.CanSpawnMobile(primaryLoc))
                        {
                            continue;
                        }

                        CityBots bot = new CityBots(name, title, female, hasMount, botClass, primaryLoc, alternateLoc);
                        bot.MoveToWorld(primaryLoc, map);
                        spawnedCount++;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void LoadCityChats()
        {
            string[] files = Directory.GetFiles(Path.Combine(Core.BaseDirectory, "Data"), "CityBotChat_*.cfg");
            foreach (string file in files)
            {
                string city = Path.GetFileNameWithoutExtension(file).Replace("CityBotChat_", "");
                List<string> chats = new List<string>();
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                            chats.Add(line.Trim());
                    }
                }
                m_CityChats[city] = chats;
            }
        }

        private static Point3D ParsePoint3D(string input)
        {
            string[] coords = input.Split(';');
            return new Point3D(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
        }

        public CityBots(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3);
            writer.Write(m_BotTitle);
            writer.Write(m_HasMount);
            writer.Write(m_BotClass);
            writer.Write(m_PrimaryLocation);
            writer.Write(m_AlternateLocation);
            writer.Write(m_Mana);
            writer.Write(m_MovingToAlternate);
            writer.Write(m_LastSpellCast);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_BotTitle = reader.ReadString();
            m_HasMount = reader.ReadBool();
            m_BotClass = reader.ReadString();
            m_PrimaryLocation = reader.ReadPoint3D();
            m_AlternateLocation = reader.ReadPoint3D();
            if (version >= 1)
                m_Mana = reader.ReadInt();
            else
                m_Mana = 100;
            if (version >= 2)
                m_MovingToAlternate = reader.ReadBool();
            else
                m_MovingToAlternate = Utility.RandomBool();
            if (version >= 3)
                m_LastSpellCast = reader.ReadDateTime();
            else
                m_LastSpellCast = DateTime.MinValue;

            m_SpellCooldowns = new Dictionary<string, DateTime>();
            m_SpellTimer = new SpellTimer(this);
            m_MovementTimer = new MovementTimer(this);
            m_SpeechTimer = new SpeechTimer(this);
            m_SkillTimer = new SkillTimer(this);
            m_SpellTimer.Start();
            m_MovementTimer.Start();
            m_SpeechTimer.Start();
            m_SkillTimer.Start();
        }
    }
}
