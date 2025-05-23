using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public class VendorLocation
    {
        public string VendorType { get; }
        public string SkillToPerform { get; }
        public Point3D Position { get; }

        public VendorLocation(string vendorType, string skillToPerform, Point3D position)
        {
            VendorType = vendorType;
            SkillToPerform = skillToPerform;
            Position = position;
        }
    }

    public static class VendorLocations
    {
        private static readonly Dictionary<string, List<VendorLocation>> _locations = new Dictionary<string, List<VendorLocation>>();

        public static void Initialize()
        {
            Load();
        }

        public static List<VendorLocation> GetLocations(string city)
        {
            if (_locations.TryGetValue(city, out var locations))
                return locations;
            return new List<VendorLocation>();
        }

        private static void Load()
        {
            _locations.Clear();
            string path = Path.Combine(Core.BaseDirectory, "Custom/VendorLocations.cfg");
            if (!File.Exists(path))
            {
                Console.WriteLine("VendorLocations.cfg not found at {0}.", path);
                return;
            }

            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                string currentCity = null;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    line = line.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.EndsWith(":"))
                    {
                        currentCity = line.Substring(0, line.Length - 1).Trim();
                        _locations[currentCity] = new List<VendorLocation>();
                        Console.WriteLine($"VendorLocations: Loaded city {currentCity} at line {lineNumber}.");
                        continue;
                    }

                    if (currentCity == null)
                    {
                        Console.WriteLine($"VendorLocations: No city defined at line {lineNumber}, skipping: {line}");
                        continue;
                    }

                    string[] parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5)
                    {
                        Console.WriteLine($"VendorLocations: Invalid format at line {lineNumber}: {line}");
                        continue;
                    }

                    try
                    {
                        string vendorType = parts[0].Trim();
                        string skillToPerform = parts[1].Trim();
                        int x = int.Parse(parts[2].Trim());
                        int y = int.Parse(parts[3].Trim());
                        int z = int.Parse(parts[4].Trim());
                        Point3D position = new Point3D(x, y, z);
                        _locations[currentCity].Add(new VendorLocation(vendorType, skillToPerform, position));
                        Console.WriteLine($"VendorLocations: Added {vendorType} at {position} for city {currentCity} at line {lineNumber}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"VendorLocations: Error parsing at line {lineNumber}: {ex.Message}");
                    }
                }
            }
        }
    }
}
