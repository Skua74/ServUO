using Server;
using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public class PlayerBotCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("SpawnPlayerBots", AccessLevel.Administrator, new CommandEventHandler(SpawnPlayerBots_OnCommand));
            CommandSystem.Register("RemovePlayerBots", AccessLevel.Administrator, new CommandEventHandler(RemovePlayerBots_OnCommand));
        }

        [Usage("SpawnPlayerBots")]
        [Description("Spawns all PlayerBots defined in Custom/PlayerBots.cfg.")]
        private static void SpawnPlayerBots_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            try
            {
                LoadPlayerBots();
                from.SendMessage("PlayerBots loading completed. Check server console for details.");
            }
            catch (Exception ex)
            {
                from.SendMessage("An error occurred while spawning PlayerBots: {0}", ex.Message);
            }
        }

        [Usage("RemovePlayerBots")]
        [Description("Removes all PlayerBots from the world.")]
        private static void RemovePlayerBots_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string path = Path.Combine(Core.BaseDirectory, "Custom/PlayerBots.cfg");
            if (!File.Exists(path))
            {
                from.SendMessage("PlayerBots.cfg not found at {0}.", path);
                return;
            }

            int deletedCount = 0;
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(',');
                    if (parts.Length < 8)
                    {
                        from.SendMessage("Invalid format in PlayerBots.cfg at line {0}. Expected 8 fields.", lineNumber);
                        continue;
                    }

                    try
                    {
                        string name = parts[0].Trim();
                        string botClass = parts[4].Trim();
                        string mapName = parts.Length > 8 ? parts[8].Trim() : parts[7].Trim();

                        Map map = null;
                        if (string.Equals(mapName, "Felucca", StringComparison.OrdinalIgnoreCase))
                            map = Map.Felucca;
                        else if (string.Equals(mapName, "Trammel", StringComparison.OrdinalIgnoreCase))
                            map = Map.Trammel;
                        else
                            map = Map.Parse(mapName);

                        if (map == null || map == Map.Internal)
                        {
                            from.SendMessage("Invalid map in PlayerBots.cfg at line {0}: {1}", lineNumber, mapName);
                            continue;
                        }

                        foreach (Mobile m in World.Mobiles.Values)
                        {
                            if ((m is PlayerBotMage && botClass.Equals("Mage", StringComparison.OrdinalIgnoreCase)) ||
                                (m is PlayerBotPaladin && botClass.Equals("Paladin", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (m.Name == name && m.Map == map)
                                {
                                    m.Delete();
                                    deletedCount++;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        from.SendMessage("Error in PlayerBots.cfg at line {0}: {1}", lineNumber, ex.Message);
                    }
                }
            }

            from.SendMessage("{0} PlayerBots have been removed from the world.", deletedCount);
        }

        private static void LoadPlayerBots()
        {
            string path = Path.Combine(Core.BaseDirectory, "Custom/PlayerBots.cfg");
            if (!File.Exists(path))
            {
                Console.WriteLine("PlayerBots.cfg not found at {0}.", path);
                return;
            }

            int spawnedCount = 0;
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length < 10)
                    {
                        Console.WriteLine($"Invalid format in PlayerBots.cfg at line {0}. Expected 10 fields.", lineNumber);
                        continue;
                    }

                    try
                    {
                        string name = parts[0].Trim();
                        string title = parts[1].Trim();
                        bool female = bool.Parse(parts[2].Trim());
                        bool hasMount = bool.Parse(parts[3].Trim());
                        string botClass = parts[4].Trim();
                        int x = int.Parse(parts[5].Trim());
                        int y = int.Parse(parts[6].Trim());
                        int z = int.Parse(parts[7].Trim());
                        Point3D location = new Point3D(x, y, z);
                        string mapName = parts[8].Trim();
                        string city = parts[9].Trim();

                        Map map = null;
                        if (string.Equals(mapName, "Felucca", StringComparison.OrdinalIgnoreCase))
                            map = Map.Felucca;
                        else if (string.Equals(mapName, "Trammel", StringComparison.OrdinalIgnoreCase))
                            map = Map.Trammel;
                        else
                            map = Map.Parse(mapName);

                        if (map == null || map == Map.Internal)
                        {
                            Console.WriteLine("Invalid map in PlayerBots.cfg at line {0}: {1}", lineNumber, mapName);
                            continue;
                        }

                        if (!map.CanSpawnMobile(location))
                        {
                            Console.WriteLine("Cannot spawn at location {0} in PlayerBots.cfg at line {1}", location, lineNumber);
                            continue;
                        }

                        Mobile bot = null;
                        if (botClass.Equals("Mage", StringComparison.OrdinalIgnoreCase))
                        {
                            bot = new PlayerBotMage(name, title, female, hasMount, location, map, city);
                        }
                        else if (botClass.Equals("Paladin", StringComparison.OrdinalIgnoreCase))
                        {
                            bot = new PlayerBotPaladin(name, title, female, hasMount, location, map, city);
                        }

                        if (bot != null)
                        {
                            bot.MoveToWorld(location, map);
                            spawnedCount++;
                        }
                        else
                        {
                            Console.WriteLine("Unknown bot class in PlayerBots.cfg at line {0}: {1}", lineNumber, botClass);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in PlayerBots.cfg at line {0}: {1}", lineNumber, ex.Message);
                    }
                }
            }
            Console.WriteLine("{0} PlayerBots spawned.", spawnedCount);
        }
    }
}
