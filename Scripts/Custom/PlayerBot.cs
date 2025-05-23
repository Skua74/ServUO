using Server;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public abstract class PlayerBot : BaseHire
    {
        private bool m_HasMount;
        private Horse m_Mount;
        private Point3D m_SpawnPoint;

        [Constructable]
        public PlayerBot(string name, string title, bool female, bool hasMount, Point3D location, Map map)
            : base()
        {
            Name = name;
            Title = title;
            Female = female;
            m_HasMount = hasMount;
            Home = location;
            m_SpawnPoint = Home; 
            RangeHome = 0; // Bot bleibt an der Spawn-Position
            MoveToWorld(location, map);
            InitBody();
            InitOutfit();
            if (m_HasMount)
                AddMount();
        }

        public override void OnThink()
        {
            // Überschreibe OnThink, um Bewegung zu verhindern
        }

        protected virtual void InitBody()
        {
            Body = Female ? 0x191 : 0x190;
            Hue = Utility.RandomSkinHue();
            HairItemID = Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2046, 0x2047);
            HairHue = Utility.RandomHairHue();
        }

        protected virtual void InitOutfit()
        {
            // Basis-Klasse definiert keine Standard-Ausrüstung
        }

        protected void AddMount()
        {
            Horse horse = new Horse();
            horse.Controlled = true;
            horse.ControlMaster = this;
            horse.MoveToWorld(Location, Map);
            m_Mount = horse;
            horse.Rider = this;
        }

        // SetControlMaster ist in BaseCreature standardmäßig nicht
        // virtual, musste geändert werden, um sie hier überschreiben zu können. 
        public override bool SetControlMaster(Mobile m)
        {
            bool result = base.SetControlMaster(m);
            Home = m_SpawnPoint;

            if (m == null && result) // Entlassen
            {
                Say("Farewell, my friend.");
                if (Home != Point3D.Zero)
                {
                    MoveToWorld(Home, Map); // Zurück zur ursprünglichen Position
                }
            }

            return result;
        }

        public override void OnDelete()
        {
            if (m_Mount != null && !m_Mount.Deleted)
            {
                m_Mount.Delete();
            }
            base.OnDelete();
        }

        public PlayerBot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2); // Version
            writer.Write(m_HasMount);
            writer.Write(m_Mount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    m_HasMount = reader.ReadBool();
                    m_Mount = reader.ReadMobile() as Horse;
                    break;
                case 1:
                    m_HasMount = reader.ReadBool();
                    break;
                case 0:
                    // Keine zusätzlichen Daten
                    break;
                default:
                    // Ungültige Version, markiere als ungültig
                    throw new Exception($"Invalid version {version} for PlayerBot");
            }
        }
    }
}
