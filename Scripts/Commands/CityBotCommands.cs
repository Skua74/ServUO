using Server;
using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public class CityBotCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("SpawnCityBots", AccessLevel.Administrator, new CommandEventHandler(SpawnCityBots_OnCommand));
            CommandSystem.Register("RemoveCityBots", AccessLevel.Administrator, new CommandEventHandler(RemoveCityBots_OnCommand));
        }

        [Usage("SpawnCityBots")]
        [Description("Spawns all CityBots defined in Data/CityBots.cfg.")]
        private static void SpawnCityBots_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            try
            {
                CityBots.LoadCityBots();
                from.SendMessage("CityBots loading completed. Check server console for details.");
            }
            catch (Exception ex)
            {
                from.SendMessage("An error occurred while spawning CityBots: {0}", ex.Message);
            }
        }

        [Usage("RemoveCityBots")]
        [Description("Removes all CityBots from the world.")]
        private static void RemoveCityBots_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            int count = 0;
            List<Mobile> toDelete = new List<Mobile>();

            // Sammle alle CityBots-Instanzen in einer Liste
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is CityBots)
                {
                    toDelete.Add(m);
                }
            }

            // Lösche die gesammelten CityBots
            foreach (Mobile m in toDelete)
            {
                m.Delete();
                count++;
            }

            from.SendMessage("{0} CityBots have been removed from the world.", count);
        }
    }
}
