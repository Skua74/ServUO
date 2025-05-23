using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class CityLifeNPC : BaseCreature
    {
        private Timer m_ActionTimer;
        private Timer m_SpellTimer;
        private BaseCreature m_SummonedCreature; // Speichert die aktuell beschworene Kreatur
        private readonly List<string> m_Phrases = new List<string>
        {
            "Such a nice day, isn't it?",
            "Where is the bank?",
            "This is such a lively City!",
            "Watch your steps!",
            "I heard there are some evil creatures in the woods!",
            "bank",
            "hlep",
            "wwwwwwwwww",
            "Cor Pro",
            "An Nox"
        };
        private static readonly string[] MaleNames = { "Finn", "Hans", "Ben", "David", "Jonas", "Karl", "Lars", "Otto" };
        private static readonly string[] FemaleNames = { "Greta", "Ida", "Anna", "Clara", "Emma", "Lena", "Maria", "Sofia" };

        [Constructable]
        public CityLifeNPC() : base(AIType.AI_Animal, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Body = Utility.RandomBool() ? 401 : 400; // Weiblich: 401, Männlich: 400
            Name = GetRandomName();
            Hue = Utility.RandomSkinHue();

            // Blauer Name wie bei Spielern
            NameHue = 0x35;

            // Zufällige Spieler-Rüstung
            EquipPlayerArmor();

            // Geschlechtsspezifische Frisur
            if (Female)
            {
                HairItemID = Utility.RandomList(0x203C, 0x203D, 0x2045);
            }
            else
            {
                HairItemID = Utility.RandomList(0x203B, 0x2044, 0x2046);
            }
            HairHue = Utility.RandomHairHue();

            // Reittier (20% Chance, nur Pferd)
            if (Utility.RandomDouble() < 0.2)
            {
                BaseMount mount = new Horse();
                mount.Rider = this;
                mount.Hue = Utility.RandomNeutralHue();
                mount.Controlled = true;
                mount.ControlMaster = this;
            }

            // Eigenschaften
            Blessed = true;
            CantWalk = false;
            Direction = (Direction)Utility.Random(8);

            // Magery für Zaubersprüche
            SetSkill(SkillName.Magery, 70.0, 100.0);
            SetSkill(SkillName.MagicResist, 50.0, 80.0);
            SetSkill(SkillName.Meditation, 50.0, 80.0);
            Mana = 100;

            // Timer für Aktionen und Zaubersprüche
            m_ActionTimer = new ActionTimer(this);
            m_ActionTimer.Start();
            m_SpellTimer = new SpellTimer(this);
            m_SpellTimer.Start();
        }

        public CityLifeNPC(Serial serial) : base(serial)
        {
        }

        private string GetRandomName()
        {
            return Female ? FemaleNames[Utility.Random(FemaleNames.Length)] : MaleNames[Utility.Random(MaleNames.Length)];
        }

        private void EquipPlayerArmor()
        {
            switch (Utility.Random(3))
            {
                case 0: // Leder-Rüstung
                    AddItem(new LeatherChest());
                    AddItem(new LeatherArms());
                    AddItem(new LeatherLegs());
                    AddItem(new LeatherGloves());
                    AddItem(new LeatherCap());
                    break;
                case 1: // Ketten-Rüstung
                    AddItem(new ChainChest());
                    AddItem(new ChainLegs());
                    AddItem(new PlateArms());
                    AddItem(new PlateGloves());
                    AddItem(new CloseHelm());
                    break;
                case 2: // Platten-Rüstung
                    AddItem(new PlateChest());
                    AddItem(new PlateArms());
                    AddItem(new PlateLegs());
                    AddItem(new PlateGloves());
                    AddItem(new PlateHelm());
                    break;
            }

            if (Utility.RandomBool())
            {
                AddItem(Utility.RandomBool() ? (Item)new Longsword() : (Item)new HeaterShield());
            }

            AddItem(new Boots(Utility.RandomNeutralHue()));

            if (Utility.RandomBool())
            {
                AddItem(new Cloak(Utility.RandomDyedHue()));
            }
            else
            {
                AddItem(new Robe(Utility.RandomDyedHue()));
            }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile.InRange(this, 3) && !e.Handled)
            {
                e.Handled = true;
                Say(m_Phrases[Utility.Random(m_Phrases.Count)]);
            }
        }

        private void KeepSummonNearby(BaseCreature summon)
        {
            if (summon != null && !summon.Deleted && summon.ControlMaster == this)
            {
                // Prüfe Entfernung
                if (GetDistanceToSqrt(summon) > 5)
                {
                    // Teleportiere Kreatur in die Nähe (zufällige Position innerhalb von 2 Tiles)
                    Point3D newLoc = GetSpawnPosition(2);
                    if (newLoc != Point3D.Zero)
                    {
                        summon.MoveToWorld(newLoc, Map);
                    }
                }
            }
        }

        private Point3D GetSpawnPosition(int range)
        {
            for (int i = 0; i < 10; i++)
            {
                int x = X + Utility.RandomMinMax(-range, range);
                int y = Y + Utility.RandomMinMax(-range, range);
                int z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(x, y, z))
                    return new Point3D(x, y, z);
            }
            return Point3D.Zero;
        }

        private void CastSpell()
        {
            if (Utility.RandomDouble() < 0.1) // 10% Chance, Zauber auszulösen
            {
                // 50% Chance, dass der Zauber fehlschlägt
                if (Utility.RandomDouble() < 0.5)
                {
                    PlaySound(0x5C); // Korrigierter Fizzle-Sound
                    Say("Fizzle!");
                    return;
                }

                switch (Utility.Random(14)) // 14 Zaubersprüche
                {
                    case 0: // Create Food
                        Say("In Mani Ylem!");
                        Item food = new BreadLoaf();
                        food.MoveToWorld(Location, Map);
                        PlaySound(0x1E2);
                        break;
                    case 1: // Heal
                        Say("In Mani!");
                        Hits += Utility.RandomMinMax(10, 20);
                        PlaySound(0x1F2);
                        FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                        break;
                    case 2: // Nightsight
                        Say("In Lor!");
                        PlaySound(0x1E3);
                        FixedParticles(0x376A, 9, 20, 9916, EffectLayer.Head);
                        break;
                    case 3: // Reactive Armor
                        Say("Flam Sanct!");
                        PlaySound(0x1ED);
                        FixedParticles(0x376A, 9, 20, 5032, EffectLayer.Waist);
                        SetSkill(SkillName.MagicResist, Skills[SkillName.MagicResist].Base + 10.0, 120.0);
                        break;
                    case 4: // Agility
                        Say("Rel Por!");
                        PlaySound(0x1E7);
                        FixedParticles(0x375A, 9, 20, 5021, EffectLayer.Waist);
                        Dex += Utility.RandomMinMax(5, 10);
                        break;
                    case 5: // Cure
                        Say("An Nox!");
                        Poison = null;
                        PlaySound(0x1E0);
                        FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                        break;
                    case 6: // Protection
                        Say("Uus Sanct!");
                        PlaySound(0x1ED);
                        FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);
                        SetSkill(SkillName.MagicResist, Skills[SkillName.MagicResist].Base + 5.0, 120.0);
                        break;
                    case 7: // Strength
                        Say("In Jux!");
                        PlaySound(0x1EE);
                        FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
                        Str += Utility.RandomMinMax(5, 10);
                        break;
                    case 8: // Bless
                        Say("Rel Sanct!");
                        PlaySound(0x1EA);
                        FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                        Str += Utility.RandomMinMax(3, 5);
                        Dex += Utility.RandomMinMax(3, 5);
                        Int += Utility.RandomMinMax(3, 5);
                        break;
                    case 9: // Summon Creature
                        Say("Kal Xen!");
                        if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                        {
                            m_SummonedCreature.Delete();
                        }
                        BaseCreature creature = Utility.RandomBool() ? (BaseCreature)new DireWolf() : new Panther();
                        creature.MoveToWorld(Location, Map);
                        creature.Controlled = true;
                        creature.ControlMaster = this;
                        creature.ControlOrder = OrderType.Follow;
                        creature.ControlTarget = this;
                        m_SummonedCreature = creature;
                        Timer.DelayCall(TimeSpan.FromMinutes(1), () =>
                        {
                            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                            {
                                m_SummonedCreature.Delete();
                                m_SummonedCreature = null;
                            }
                        });
                        Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () => KeepSummonNearby(creature));
                        PlaySound(0x1FE);
                        break;
                    case 10: // Summon Fire Elemental
                        Say("Flam Kal Des Ylem!");
                        if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                        {
                            m_SummonedCreature.Delete();
                        }
                        FireElemental fireElemental = new FireElemental();
                        fireElemental.MoveToWorld(Location, Map);
                        fireElemental.Controlled = true;
                        fireElemental.ControlMaster = this;
                        fireElemental.ControlOrder = OrderType.Follow;
                        fireElemental.ControlTarget = this;
                        m_SummonedCreature = fireElemental;
                        Timer.DelayCall(TimeSpan.FromMinutes(1), () =>
                        {
                            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                            {
                                m_SummonedCreature.Delete();
                                m_SummonedCreature = null;
                            }
                        });
                        Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () => KeepSummonNearby(fireElemental));
                        PlaySound(0x227);
                        break;
                    case 11: // Summon Earth Elemental
                        Say("In Ort Ylem!");
                        if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                        {
                            m_SummonedCreature.Delete();
                        }
                        EarthElemental earthElemental = new EarthElemental();
                        earthElemental.MoveToWorld(Location, Map);
                        earthElemental.Controlled = true;
                        earthElemental.ControlMaster = this;
                        earthElemental.ControlOrder = OrderType.Follow;
                        earthElemental.ControlTarget = this;
                        m_SummonedCreature = earthElemental;
                        Timer.DelayCall(TimeSpan.FromMinutes(1), () =>
                        {
                            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                            {
                                m_SummonedCreature.Delete();
                                m_SummonedCreature = null;
                            }
                        });
                        Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () => KeepSummonNearby(earthElemental));
                        PlaySound(0x227);
                        break;
                    case 12: // Summon Water Elemental
                        Say("In Aqua Ylem!");
                        if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                        {
                            m_SummonedCreature.Delete();
                        }
                        WaterElemental waterElemental = new WaterElemental();
                        waterElemental.MoveToWorld(Location, Map);
                        waterElemental.Controlled = true;
                        waterElemental.ControlMaster = this;
                        waterElemental.ControlOrder = OrderType.Follow;
                        waterElemental.ControlTarget = this;
                        m_SummonedCreature = waterElemental;
                        Timer.DelayCall(TimeSpan.FromMinutes(1), () =>
                        {
                            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                            {
                                m_SummonedCreature.Delete();
                                m_SummonedCreature = null;
                            }
                        });
                        Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () => KeepSummonNearby(waterElemental));
                        PlaySound(0x227);
                        break;
                    case 13: // Summon Air Elemental
                        Say("In Vas Ort Ylem!");
                        if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                        {
                            m_SummonedCreature.Delete();
                        }
                        AirElemental airElemental = new AirElemental();
                        airElemental.MoveToWorld(Location, Map);
                        airElemental.Controlled = true;
                        airElemental.ControlMaster = this;
                        airElemental.ControlOrder = OrderType.Follow;
                        airElemental.ControlTarget = this;
                        m_SummonedCreature = airElemental;
                        Timer.DelayCall(TimeSpan.FromMinutes(1), () =>
                        {
                            if (m_SummonedCreature != null && !m_SummonedCreature.Deleted)
                            {
                                m_SummonedCreature.Delete();
                                m_SummonedCreature = null;
                            }
                        });
                        Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () => KeepSummonNearby(airElemental));
                        PlaySound(0x227);
                        break;
                }
            }
        }

        private class ActionTimer : Timer
        {
            private readonly CityLifeNPC m_NPC;

            public ActionTimer(CityLifeNPC npc) : base(TimeSpan.FromSeconds(75), TimeSpan.FromSeconds(Utility.RandomMinMax(75, 150)))
            {
                m_NPC = npc;
            }

            protected override void OnTick()
            {
                if (m_NPC.Deleted || !m_NPC.Alive)
                {
                    Stop();
                    return;
                }

                switch (Utility.Random(4))
                {
                    case 0: // Zufälliges Herumlaufen
                        m_NPC.Direction = (Direction)Utility.Random(8);
                        m_NPC.Move(m_NPC.Direction);
                        break;
                    case 1: // Schnelles Laufen
                        m_NPC.Direction = (Direction)Utility.Random(8);
                        m_NPC.CurrentSpeed = 0.1;
                        m_NPC.Move(m_NPC.Direction);
                        m_NPC.CurrentSpeed = 0.2;
                        break;
                    case 2: // Zufällige Animation
                        m_NPC.Animate(Utility.RandomList(5, 6, 7), 5, 1, true, false, 0);
                        break;
                    case 3: // Zufälliger Spruch
                        m_NPC.Say(m_NPC.m_Phrases[Utility.Random(m_NPC.m_Phrases.Count)]);
                        break;
                }
            }
        }

        private class SpellTimer : Timer
        {
            private readonly CityLifeNPC m_NPC;

            public SpellTimer(CityLifeNPC npc) : base(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
            {
                m_NPC = npc;
            }

            protected override void OnTick()
            {
                if (m_NPC.Deleted || !m_NPC.Alive)
                {
                    Stop();
                    return;
                }

                m_NPC.CastSpell();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Version
            writer.Write(m_SummonedCreature);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SummonedCreature = reader.ReadMobile() as BaseCreature;

            m_ActionTimer = new ActionTimer(this);
            m_ActionTimer.Start();
            m_SpellTimer = new SpellTimer(this);
            m_SpellTimer.Start();
        }
    }
}