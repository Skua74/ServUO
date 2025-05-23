using Server;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class PlayerBotBlacksmith : PlayerBot
    {
        private DateTime m_NextSkillTime; // Zeitpunkt der nächsten Skill-Ausführung
        private Timer m_SkillTimer; // Timer für Skill-Wiederholungen

        [Constructable]
        public PlayerBotBlacksmith(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Vendor; // Passend für Handwerker

            SetStr(81, 95);
            SetDex(61, 75);
            SetInt(61, 75);

            SetDamage(10, 20);

            SetSkill(SkillName.Blacksmith, 80.0, 100.0);
            SetSkill(SkillName.ArmsLore, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 45.0, 67.5);
            SetSkill(SkillName.Mining, 45.0, 67.5);

            Fame = 100;
            Karma = 100; // Neutral, Handwerker

            PackGold(20, 100);

            // Initialisiere Skill-Timer
            m_NextSkillTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
        }

        public override void OnThink()
        {
            base.OnThink();

            // Skill-Ausführung nur, wenn kein ControlMaster und nicht gelöscht
            if (ControlMaster != null || Deleted)
                return;

            // Prüfe, ob es Zeit für die Skill-Ausführung ist
            if (DateTime.UtcNow >= m_NextSkillTime && m_SkillTimer == null)
            {
                StartSkillExecution();
            }
        }

        private void StartSkillExecution()
        {
            int repeatCount = Utility.RandomMinMax(2, 5); // 2–5 Wiederholungen
            m_SkillTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.5), repeatCount, ExecuteSkill);
            m_NextSkillTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
        }

        private void ExecuteSkill()
        {
            if (ControlMaster != null || Deleted)
            {
                StopSkillTimer();
                return;
            }

            // Schmieden-Animation, Sound und Partikeleffekt
            Animate(9, 5, 1, true, false, 0); // Hämmern-Animation
            PlaySound(0x2A); // Schmieden-Sound
            Effects.SendLocationEffect(Location, Map, 0x374A, 10, 10); // Funken-Partikel
        }

        private void StopSkillTimer()
        {
            if (m_SkillTimer != null)
            {
                m_SkillTimer.Stop();
                m_SkillTimer = null;
            }
        }

        public override bool SetControlMaster(Mobile m)
        {
            bool result = base.SetControlMaster(m);
            if (m != null)
            {
                StopSkillTimer(); // Stoppe Skill-Ausführung, wenn gehired
            }
            return result;
        }

        public override void OnDelete()
        {
            StopSkillTimer();
            base.OnDelete();
        }

        protected override void InitOutfit()
        {
            AddItem(new FishermansTrousers());
            AddItem(new FullApron(Utility.RandomNeutralHue()));
            AddItem(new SmithHammer());
            AddItem(new Tongs());
        }

        public PlayerBotBlacksmith(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Version
            writer.Write(m_NextSkillTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (version >= 0)
            {
                m_NextSkillTime = reader.ReadDateTime();
            }
        }
    }
}
