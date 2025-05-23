using Server;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class PlayerBotThief : PlayerBot
    {
        [Constructable]
        public PlayerBotThief(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base(name, title, female, hasMount, location, map, city)
        {
            AI = AIType.AI_Thief; // Thief-AI für Stealth und Diebstahl

            SetStr(61, 75);
            SetDex(91, 105);
            SetInt(71, 85);

            SetDamage(8, 18); // Etwas niedrigerer Schaden, da Fokus auf Heimlichkeit

            SetSkill(SkillName.Stealing, 80.0, 100.0);
            SetSkill(SkillName.Stealth, 80.0, 100.0);
            SetSkill(SkillName.Hiding, 80.0, 100.0);
            SetSkill(SkillName.Lockpicking, 65.0, 87.5);
            SetSkill(SkillName.Snooping, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 45.0, 67.5);
            SetSkill(SkillName.Fencing, 65.0, 87.5);

            Fame = 50; // Niedrige Fame, da Diebe weniger bekannt sind
            Karma = -100; // Leicht negativ, da Diebstahl unmoralisch ist

            PackGold(20, 100);
        }

        protected override void InitOutfit()
        {
            AddItem(new Cloak(Utility.RandomNeutralHue()));
            AddItem(new LeatherChest());
            AddItem(new LeatherLegs());
            AddItem(new Boots());
            AddItem(new Dagger());
        }

        public PlayerBotThief(Serial serial) : base(serial)
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
