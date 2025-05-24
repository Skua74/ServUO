using Server;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class PlayerBotTailor : PlayerBot
    {
        private DateTime m_NextSkillTime; // Zeitpunkt der nächsten Skill-Ausführung
        private Timer m_SkillTimer; // Timer für Skill-Wiederholungen
        private int m_minSkillIntervall = 30;
        private int m_maxSkillIntervall = 60;

        [Constructable]
        public PlayerBotTailor(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Vendor; // Passend für Handwerker

            SetStr(61, 75);
            SetDex(71, 85);
            SetInt(71, 85);

            SetDamage(8, 15);

            SetSkill(SkillName.Tailoring, 80.0, 100.0);
            SetSkill(SkillName.ArmsLore, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 45.0, 67.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);

            Fame = 100;
            Karma = 100; // Neutral, Handwerker

            PackGold(20, 100);

            // Initialisiere Skill-Timer
            m_NextSkillTime =
                DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(m_minSkillIntervall, m_maxSkillIntervall));
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
            int repeatCount = Utility.RandomMinMax(3, 8); // 2–5 Wiederholungen
            m_SkillTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.5), repeatCount, () =>
            {
                ExecuteSkill();
                if (--repeatCount <= 0)
                {
                    StopSkillTimer();
                }
            });
            m_SkillTimer.Priority = TimerPriority.FiftyMS; // Präziser Timer
            m_NextSkillTime =
                DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(m_minSkillIntervall, m_maxSkillIntervall));
        }

        private void ExecuteSkill()
        {
            if (ControlMaster != null || Deleted || Map == null)
            {
                StopSkillTimer();
                return;
            }
            // Nähen-Animation und Sound (alternative IDs)
            Animate(33, 5, 1, true, false, 0); // Generische Arbeits-Animation
            PlaySound(0x248); // Nadelgeräusch
            // Kein Partikeleffekt für Tailoring
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
                StopSkillTimer();
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
            AddItem(new FancyShirt(Utility.RandomNeutralHue()));
            AddItem(new LongPants(Utility.RandomNeutralHue()));
        }

        public PlayerBotTailor(Serial serial) : base(serial)
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
                // Stelle sicher, dass m_NextSkillTime nicht in der Vergangenheit liegt
                if (m_NextSkillTime < DateTime.UtcNow)
                {
                    m_NextSkillTime =
                        DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(m_minSkillIntervall, m_maxSkillIntervall));

                }
            }
        }
    }
}
