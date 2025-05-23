using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotHunter : PlayerBot
    {
        [Constructable]
        public PlayerBotHunter(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Archer; // Archer-AI für Fernkampf

            SetStr(71, 85);
            SetDex(86, 100);
            SetInt(61, 75);

            SetDamage(10, 20);

            SetSkill(SkillName.Archery, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Anatomy, 65.0, 87.5);
            SetSkill(SkillName.Tracking, 65.0, 87.5);
            SetSkill(SkillName.Camping, 45.0, 67.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);

            Fame = 100;
            Karma = 100; // Neutral, naturverbunden

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new LeatherChest());
            AddItem(new LeatherLegs());
            AddItem(new LeatherArms());
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Bow());
            AddItem(new BaseQuiver());
        }

        public PlayerBotHunter(Serial serial) : base(serial)
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
