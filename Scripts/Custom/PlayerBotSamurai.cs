using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotSamurai : PlayerBot
    {
        [Constructable]
        public PlayerBotSamurai(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Samurai; // Samurai nutzen Nahkampf-AI

            SetStr(85, 100);
            SetDex(81, 95);
            SetInt(61, 75);

            SetDamage(10, 23);

            SetSkill(SkillName.Swords, 80.0, 100.0);
            SetSkill(SkillName.Bushido, 85.0, 100.0);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Anatomy, 65.0, 87.5);
            SetSkill(SkillName.Parry, 45.0, 60.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);

            Fame = 100;
            Karma = 200; // Samurai sind ehrenhaft

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new SamuraiHelm());
            AddItem(new SamuraiTabi());
            AddItem(new LeatherDo());
            AddItem(new LeatherHiroSode());
            AddItem(new LeatherHaidate());
            AddItem(new Katana());
        }

        public PlayerBotSamurai(Serial serial) : base(serial)
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
