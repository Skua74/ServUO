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
        private string m_City;
        private Point3D m_SpawnPoint;
        private DateTime m_NextMoveTime; // Zeitpunkt der nächsten Bewegung
        private Point3D m_TargetPoint; // Zielpunkt der Bewegung
        private bool m_IsMovingToTarget; // Bewegt sich zum Ziel?
        private bool m_IsWaiting; // Wartet am Ziel?
        private Timer m_WaitTimer; // Timer für die Wartezeit
        private Timer m_MoveTimer; // Timer für fließende Bewegung
        private Queue<Direction> m_MovePath; // Pfad als Richtungen

        [Constructable]
        public PlayerBot(string name, string title, bool female, bool hasMount, Point3D location, Map map, string city)
            : base()
        {
            Name = name;
            Title = title;
            Female = female;
            m_HasMount = hasMount;
            m_City = city;
            Home = location;
            m_SpawnPoint = Home;
            RangeHome = 0; // Bot bleibt an der Spawn-Position, außer bei Bewegung
            MoveToWorld(location, map);
            InitBody();
            InitOutfit();
            if (m_HasMount)
                AddMount();

            // Initialisiere Bewegungstimer
            m_NextMoveTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(60, 120));
            m_IsMovingToTarget = false;
            m_IsWaiting = false;
            m_MovePath = new Queue<Direction>();
        }

        public override void OnThink()
        {
            base.OnThink();

            // Automatische Bewegung nur, wenn kein ControlMaster (nicht gehired) und nicht gelöscht
            if (ControlMaster != null || Deleted)
                return;

            // Prüfe, ob es Zeit für eine Bewegung ist
            if (!m_IsMovingToTarget && !m_IsWaiting && DateTime.UtcNow >= m_NextMoveTime && m_MovePath.Count == 0)
            {
                StartMovement();
            }
        }

        private void StartMovement()
        {
            // Wähle eine Richtung und Entfernung (4–10 Felder)
            Direction dir = (Direction)Utility.Random(8); // North, Right, East, etc.
            int distance = Utility.RandomMinMax(4, 10);
            Point3D target = Location;
            List<Point3D> path = new List<Point3D>();

            // Berechne Zielpunkt und Pfad
            for (int i = 0; i < distance; i++)
            {
                target = GetPointInDirection(target, dir);
                path.Add(target);
            }

            // Prüfe, ob der Weg frei ist
            bool pathValid = true;
            foreach (Point3D point in path)
            {
                if (Map == null || !Map.CanSpawnMobile(point) || !Map.LineOfSight(this, point))
                {
                    pathValid = false;
                    break;
                }
            }

            if (pathValid)
            {
                m_TargetPoint = target;
                m_IsMovingToTarget = true;
                // Fülle den Pfad mit Richtungen
                for (int i = 0; i < distance; i++)
                    m_MovePath.Enqueue(dir);
                StartMoveTimer();
            }
            else
            {
                // Weg blockiert, versuche später erneut
                m_NextMoveTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(60, 120));
            }
        }

        private Point3D GetPointInDirection(Point3D from, Direction dir)
        {
            int x = from.X;
            int y = from.Y;
            int z = from.Z;

            switch (dir)
            {
                case Direction.North: y--; break;
                case Direction.Right: x++; y--; break;
                case Direction.East: x++; break;
                case Direction.Down: x++; y++; break;
                case Direction.South: y++; break;
                case Direction.Left: x--; y++; break;
                case Direction.West: x--; break;
                case Direction.Up: x--; y--; break;
            }

            return new Point3D(x, y, Map != null ? Map.GetAverageZ(x, y) : z);
        }

        private void StartMoveTimer()
        {
            if (m_MoveTimer != null)
            {
                m_MoveTimer.Stop();
                m_MoveTimer = null;
            }
            m_MoveTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMilliseconds(80), ProcessMoveStep);
        }

        private void ProcessMoveStep()
        {
            // Abbrechen, wenn der Bot gehired wurde
            if (ControlMaster != null || Deleted)
            {
                StopMoveTimer();
                m_MovePath.Clear();
                m_IsMovingToTarget = false;
                m_IsWaiting = false;
                m_TargetPoint = Point3D.Zero;
                return;
            }

            if (m_MovePath.Count == 0)
            {
                // Ziel erreicht oder Rückweg
                if (m_IsMovingToTarget)
                {
                    m_IsMovingToTarget = false;
                    m_IsWaiting = true;
                    StopMoveTimer();
                    m_WaitTimer = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 20)), EndWaiting);
                }
                else
                {
                    // Rückweg abgeschlossen
                    StopMoveTimer();
                    m_TargetPoint = Point3D.Zero;
                }
                return;
            }

            Direction dir = m_MovePath.Dequeue();
            CantWalk = false; // Stelle sicher, dass der Bot laufen kann
            if (Move(dir))
            {
                // Laufanimation nur für nicht-berittene Bots
                if (!m_HasMount)
                    Animate(34, 5, 1, true, false, 0); // Laufanimation (Humanoid)
            }
            else
            {
                // Bewegung fehlgeschlagen (z. B. Hindernis), Pfad abbrechen
                m_MovePath.Clear();
                if (m_IsMovingToTarget)
                {
                    m_IsMovingToTarget = false;
                    m_IsWaiting = true;
                    StopMoveTimer();
                    m_WaitTimer = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 20)), EndWaiting);
                }
                else
                {
                    // Rückweg blockiert, direkt zum Spawnpunkt zurücksetzen
                    StopMoveTimer();
                    if (Map != null && Map.CanSpawnMobile(m_SpawnPoint))
                        MoveToWorld(m_SpawnPoint, Map);
                    m_TargetPoint = Point3D.Zero;
                }
            }
        }

        private void EndWaiting()
        {
            // Abbrechen, wenn der Bot gehired wurde
            if (ControlMaster != null || Deleted)
            {
                m_IsWaiting = false;
                m_WaitTimer = null;
                m_MovePath.Clear();
                m_TargetPoint = Point3D.Zero;
                return;
            }

            m_IsWaiting = false;
            m_WaitTimer = null;

            // Starte Rückweg zum Spawnpunkt
            List<Direction> returnPath = new List<Direction>();
            List<Point3D> pathPoints = new List<Point3D>();
            Point3D current = Location;
            int maxSteps = 10; // Begrenze Schritte, um Schleifen zu vermeiden

            // Berechne Rückweg
            while (current != m_SpawnPoint && maxSteps > 0)
            {
                Direction dir = GetDirectionTo(m_SpawnPoint);
                if (dir == Direction.Mask)
                    break;
                Point3D nextPoint = GetPointInDirection(current, dir);
                if (Map != null && Map.CanSpawnMobile(nextPoint) && Map.LineOfSight(this, nextPoint))
                {
                    returnPath.Add(dir);
                    pathPoints.Add(nextPoint);
                    current = nextPoint;
                }
                else
                    break;
                maxSteps--;
            }

            // Wenn ein gültiger Pfad gefunden wurde
            if (returnPath.Count > 0)
            {
                foreach (Direction dir in returnPath)
                    m_MovePath.Enqueue(dir);
                StartMoveTimer();
            }
            else
            {
                // Kein gültiger Pfad, direkt zum Spawnpunkt zurücksetzen
                if (Map != null && Map.CanSpawnMobile(m_SpawnPoint))
                    MoveToWorld(m_SpawnPoint, Map);
                m_TargetPoint = Point3D.Zero;
            }

            m_NextMoveTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(60, 120));
        }

        private void StopMoveTimer()
        {
            if (m_MoveTimer != null)
            {
                m_MoveTimer.Stop();
                m_MoveTimer = null;
            }
        }

        public override bool SetControlMaster(Mobile m)
        {
            bool result = base.SetControlMaster(m);
            Home = m_SpawnPoint;

            // Wenn gehired, stoppe automatische Bewegung
            if (m != null)
            {
                StopMoveTimer();
                m_MovePath.Clear();
                m_IsMovingToTarget = false;
                m_IsWaiting = false;
                if (m_WaitTimer != null)
                {
                    m_WaitTimer.Stop();
                    m_WaitTimer = null;
                }
                m_TargetPoint = Point3D.Zero;
            }
            // Wenn entlassen, zurück zum Spawnpunkt
            else if (result)
            {
                Say("Farewell, my friend.");
                if (Home != Point3D.Zero && Map != null && Map.CanSpawnMobile(Home))
                {
                    MoveToWorld(Home, Map); // Zurück zur ursprünglichen Position
                }
            }

            return result;
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

        public override void OnDelete()
        {
            if (m_Mount != null && !m_Mount.Deleted)
            {
                m_Mount.Delete();
            }
            if (m_WaitTimer != null)
            {
                m_WaitTimer.Stop();
                m_WaitTimer = null;
            }
            if (m_MoveTimer != null)
            {
                m_MoveTimer.Stop();
                m_MoveTimer = null;
            }
            base.OnDelete();
        }

        public PlayerBot(Serial serial) : base(serial)
        {
            m_MovePath = new Queue<Direction>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)3); // Version
            writer.Write(m_HasMount);
            writer.Write(m_Mount);
            writer.Write(m_City);
            writer.Write(m_SpawnPoint);
            writer.Write(m_NextMoveTime);
            writer.Write(m_TargetPoint);
            writer.Write(m_IsMovingToTarget);
            writer.Write(m_IsWaiting);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_HasMount = reader.ReadBool();
                    m_Mount = reader.ReadMobile() as Horse;
                    m_City = reader.ReadString();
                    m_SpawnPoint = reader.ReadPoint3D();
                    m_NextMoveTime = reader.ReadDateTime();
                    m_TargetPoint = reader.ReadPoint3D();
                    m_IsMovingToTarget = reader.ReadBool();
                    m_IsWaiting = reader.ReadBool();
                    break;
                case 2:
                    m_HasMount = reader.ReadBool();
                    m_Mount = reader.ReadMobile() as Horse;
                    break;
                case 1:
                    m_HasMount = reader.ReadBool();
                    break;
                case 0:
                    break;
                default:
                    throw new Exception($"Invalid version {version} for PlayerBot");
            }

            m_MovePath = new Queue<Direction>();
        }
    }
}
