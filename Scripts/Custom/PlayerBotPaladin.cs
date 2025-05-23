using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotPaladin : PlayerBot
    {
        [Constructable]
        public PlayerBotPaladin(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Paladin;

            SetStr(86, 100);
            SetDex(81, 95);
            SetInt(61, 75);

            SetDamage(10, 23);

            SetSkill(SkillName.Swords, 66.0, 97.5);
            SetSkill(SkillName.Anatomy, 65.0, 87.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);
            SetSkill(SkillName.Healing, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 15.0, 37.5);
            SetSkill(SkillName.Parry, 45.0, 60.5);
            SetSkill(SkillName.Chivalry, 85, 100);

            Fame = 100;
            Karma = 250;

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());
            AddItem(new PlateHelm());
            AddItem(new MetalKiteShield());
            AddItem(new Longsword());
        }

        public PlayerBotPaladin(Serial serial) : base(serial)
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
            // Für alte HirePaladin-basierte Objekte, verlasse auf BaseHire-Deserialisierung
        }
    }
}
