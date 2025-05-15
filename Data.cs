using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;

namespace RailDesigner1 // Changed namespace to match the new project
{
    public class Topcap
    {
        public string TopcapName { get; set; }
        public string Description { get; set; }
        public double TopCapHeight { get; set; }
        public double TopCapWidth { get; set; }
        public double TopCapWall { get; set; }
        public string ImageLink { get; set; }
        public string DwgLink { get; set; }
    }

    public class RailingData
    {
        public List<RailingDesign> Designs { get; set; }
        public List<Topcap> Topcaps { get; set; }
    }

    public static class Data
    {
        private static string RailDesignsPath => ConfigurationManager.AppSettings["RailDesignsPath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RailDesigns.json");

        public static RailingData LoadRailingData()
        {
            try
            {
                string path = RailDesignsPath;
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<RailingData>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load RailDesigns.json: {ex.Message}", ex);
            }
        }

        public static void SaveCustomDesign(RailingDesign newDesign)
        {
            try
            {
                // Load existing data
                var data = LoadRailingData();

                // Check for duplicate RailName
                if (data.Designs.Any(d => d.DesignName.Equals(newDesign.DesignName, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception($"A design with the name '{newDesign.DesignName}' already exists.");
                }

                // Add the new design
                data.Designs.Add(newDesign);

                // Save back to JSON
                string path = RailDesignsPath;
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save custom design: {ex.Message}", ex);
            }
        }
    }
}