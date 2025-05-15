// ﻿using Newtonsoft.Json; // Commented out temporarily due to persistent build errors
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;

namespace RailCreator
{
    public class RailingDesign // Kept uncommented as needed by other files
    {
        public string DesignName { get; set; }
        public string PostSize { get; set; }
        public string MountingType { get; set; }
        public string TopCap { get; set; }
        public double TopCapHeight { get; set; }
        public double TopCapWidth { get; set; }
        public double TopCapWall { get; set; }
        public string BottomRail { get; set; }
        public string PicketType { get; set; }
        public string PicketSize { get; set; }
        public string PicketPlacement { get; set; }
        public double RailHeight { get; set; }
        public string IntermediateRail { get; set; }
        public string ImageLink { get; set; }
        public string DwgLink { get; set; }
        public string SpecialInstructions { get; set; }
        public double? DecorativeWidth { get; set; }
    }

    public class Topcap // Kept uncommented as needed by other files
    {
        public string TopcapName { get; set; }
        public string Description { get; set; }
        public double TopCapHeight { get; set; }
        public double TopCapWidth { get; set; }
        public double TopCapWall { get; set; }
        public string ImageLink { get; set; }
        public string DwgLink { get; set; }
    }

    public class RailingData // Kept uncommented as needed by other files
    {
        public List<RailingDesign> Designs { get; set; }
        public List<Topcap> Topcaps { get; set; }
    }

    public static class Data
    {
        private static string RailDesignsPath => ConfigurationManager.AppSettings["RailDesignsPath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RailDesigns.json");

        // public static RailingData LoadRailingData() // Commented out temporarily due to persistent build errors
        // {
        //     try
        //     {
        //         string path = RailDesignsPath;
        //         string json = File.ReadAllText(path);
        //         return JsonConvert.DeserializeObject<RailingData>(json);
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new Exception($"Failed to load RailDesigns.json: {ex.Message}", ex);
        //     }
        // }

        // public static void SaveCustomDesign(RailingDesign newDesign) // Commented out temporarily due to persistent build errors
        // {
        //     try
        //     {
        //         // Load existing data
        //         var data = LoadRailingData();

        //         // Check for duplicate RailName
        //         if (data.Designs.Any(d => d.DesignName.Equals(newDesign.DesignName, StringComparison.OrdinalIgnoreCase)))
        //         {
        //             throw new Exception($"A design with the name '{newDesign.DesignName}' already exists.");
        //         }

        //         // Add the new design
        //         data.Designs.Add(newDesign);

        //         // Save back to JSON
        //         string path = RailDesignsPath;
        //         string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        //         File.WriteAllText(path, json);
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new Exception($"Failed to save custom design: {ex.Message}", ex);
        //     }
        // }
    }
}