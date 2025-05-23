using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotHealer : PlayerBot
    {
        [Constructable]
        public PlayerBotHealer(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Healer; // Healer-AI für Heilung und Unterstützung

            SetStr(61, 75);
            SetDex(71, 85);
            SetInt(86, 100);

            SetDamage(8, 15); // Niedriger Schaden, da Fokus auf Heilung

            SetSkill(SkillName.Healing, 80.0, 100.0);
            SetSkill(SkillName.Anatomy, 80.0, 100.0);
            SetSkill(SkillName.Magery, 65.0, 87.5);
            SetSkill(SkillName.Meditation, 65.0, 87.5);
            SetSkill(SkillName.MagicResist, 65.0, 87.5);
            SetSkill(SkillName.SpiritSpeak, 45.0, 67.5);

            Fame = 100;
            Karma = 250; // Positiv, da Heiler altruistisch sind

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new Robe(Utility.RandomBlueHue()));
            AddItem(new Sandals());
            AddItem(new QuarterStaff());
            AddItem(new Bandage(50)); // Heilungsverbände
        }

        public PlayerBotHealer(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            // Keine zusätzlichen Daten für Version 0
        }
    }
}
