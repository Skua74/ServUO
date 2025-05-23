using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotMage : PlayerBot
    {
        [Constructable]
        public PlayerBotMage(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Mage;

            SetStr(61, 75);
            SetDex(81, 95);
            SetInt(86, 100);

            SetDamage(10, 23);

            SetSkill(SkillName.EvalInt, 100.0, 125);
            SetSkill(SkillName.Magery, 100, 125);
            SetSkill(SkillName.Meditation, 100, 125);
            SetSkill(SkillName.MagicResist, 100, 125);
            SetSkill(SkillName.Tactics, 100, 125);
            SetSkill(SkillName.Macing, 100, 125);

            Fame = 100;
            Karma = 100;

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new Robe(Utility.RandomNeutralHue()));
            AddItem(new WizardsHat(Utility.RandomNeutralHue()));
            AddItem(new Sandals());
        }

        public PlayerBotMage(Serial serial) : base(serial)
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
