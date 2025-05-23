using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms; // For SaveFileDialog
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry; // For Extents3d
using Autodesk.AutoCAD.Runtime; // Added for CommandMethod

// Assuming RailDesigner1 namespace for consistency
// ComponentType enum is now expected to be in RailDesigner1 namespace from Utils/CommonDefinitions.cs
namespace RailDesigner1
{
    // Removed local ComponentType enum definition.
    // Removed local DictionaryExtensions class definition. It's now in Utils/CommonDefinitions.cs

    public class BomEntry
    {
        public string ComponentType { get; set; }
        public string PartNameOrProfile { get; set; } // PARTNAME for blocks, ProfileName for rails
        public string Description { get; set; }
        public double Quantity { get; set; } // Sum for identical items
        public double InstalledLength { get; set; } // Total length for rails, or per-item height for posts/pickets
        public string Material { get; set; }
        public string Finish { get; set; }
        public double Weight { get; set; } // Calculated
        public string SpecialNotes { get; set; }
        public string UserAttribute1 { get; set; }
        public string UserAttribute2 { get; set; }
        public string Key { get; private set; } // Used for aggregation

        public BomEntry(string componentType, string partNameOrProfile, string description, string material, string finish, string notes, string ua1, string ua2)
        {
            ComponentType = componentType ?? "Unknown"; 
            PartNameOrProfile = partNameOrProfile ?? "N/A"; 
            Description = description ?? "";
            Material = material ?? "N/A"; 
            Finish = finish ?? "N/A";   
            SpecialNotes = notes ?? "";
            UserAttribute1 = ua1 ?? "";
            UserAttribute2 = ua2 ?? "";
            Quantity = 0;
            InstalledLength = 0;
            Weight = 0;
            Key = $"{ComponentType.ToUpperInvariant()}_{PartNameOrProfile.ToUpperInvariant()}_{Material.ToUpperInvariant()}_{Finish.ToUpperInvariant()}";
        }
    }

    public class BomExporter
    {
        private const string RegAppName = "RAIL_DESIGN_APP"; // Matches RailingGeometryGenerator

        [CommandMethod("EXPORT_BOM")]
        public void ExportBom()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Application.ShowAlertDialog("No active document. Please open a drawing to export BOM.");
                return;
            }
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect railing components for BOM: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nBOM export cancelled by user or no entities selected.");
                return;
            }

            SelectionSet selSet = psr.Value;
            if (selSet == null || selSet.Count == 0)
            {
                ed.WriteMessage("\nNo entities selected for BOM export.");
                return;
            }

            List<ObjectId> selectedObjectIds = selSet.GetObjectIds().ToList();
            Dictionary<string, BomEntry> aggregatedBom = new Dictionary<string, BomEntry>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId objId in selectedObjectIds)
                {
                    if (objId.IsErased || !objId.IsValid) continue;

                    Entity ent = tr.GetObject(objId, OpenMode.ForRead, false, true) as Entity;
                    if (ent == null) continue;

                    if (ent is BlockReference br)
                    {
                        ProcessBlockReference(br, aggregatedBom, tr);
                    }
                    else if (ent is Polyline pl)
                    {
                        ProcessPolyline(pl, aggregatedBom, tr);
                    }
                }
                tr.Commit(); 
            }
            
            List<BomEntry> bomEntriesList = aggregatedBom.Values.OrderBy(e => e.ComponentType).ThenBy(e => e.PartNameOrProfile).ToList();
            GenerateAndSaveCsv(bomEntriesList, ed);
        }

        private void ProcessBlockReference(BlockReference br, Dictionary<string, BomEntry> aggregatedBom, Transaction tr)
        {
            string componentType = GetAttributeValue(br, "COMPONENTTYPE", tr);
            if (string.IsNullOrEmpty(componentType)) componentType = br.Name; 

            string partName = GetAttributeValue(br, "PARTNAME", tr);
            if (string.IsNullOrEmpty(partName)) partName = br.Name; 

            string description = GetAttributeValue(br, "DESCRIPTION", tr) ?? "";
            string material = GetAttributeValue(br, "MATERIAL", tr) ?? "N/A";
            string finish = GetAttributeValue(br, "FINISH", tr) ?? "N/A";
            string specialNotes = GetAttributeValue(br, "SPECIAL_NOTES", tr) ?? "";
            string ua1 = GetAttributeValue(br, "USER_ATTRIBUTE_1", tr) ?? "";
            string ua2 = GetAttributeValue(br, "USER_ATTRIBUTE_2", tr) ?? "";

            BomEntry entryDetails = new BomEntry(componentType, partName, description, material, finish, specialNotes, ua1, ua2);

            double itemHeight = ParseDouble(GetAttributeValue(br, "RailingHeight", tr));
            if(itemHeight == 0.0 && (componentType.Equals("Post", StringComparison.OrdinalIgnoreCase) || componentType.Equals("Picket", StringComparison.OrdinalIgnoreCase))) {
                 try {
                    Extents3d? bounds = br.GeometricExtents;
                    if (bounds.HasValue) {
                        itemHeight = bounds.Value.MaxPoint.Z - bounds.Value.MinPoint.Z;
                    }
                 } catch { /* GeometricExtents might fail for some entities if not generated */ }
            }
            
            double weightDensity = ParseDouble(GetAttributeValue(br, "WEIGHT_DENSITY", tr));

            if (aggregatedBom.TryGetValue(entryDetails.Key, out BomEntry existingEntry))
            {
                existingEntry.Quantity += 1;
                // For blocks, InstalledLength might represent the height of one item, or sum of lengths if they are cut from stock.
                // Current interpretation: sum of individual item heights/lengths.
                if (itemHeight > 0) existingEntry.InstalledLength += itemHeight; 
                existingEntry.Weight += weightDensity * itemHeight; 
            }
            else
            {
                entryDetails.Quantity = 1;
                if (itemHeight > 0) entryDetails.InstalledLength = itemHeight;
                entryDetails.Weight = weightDensity * itemHeight;
                aggregatedBom.Add(entryDetails.Key, entryDetails);
            }
        }

        private void ProcessPolyline(Polyline pl, Dictionary<string, BomEntry> aggregatedBom, Transaction tr)
        {
            string componentTypeStr = GetXDataValue(pl, RegAppName, "COMPONENTTYPE");
            if (string.IsNullOrEmpty(componentTypeStr))
            {
                string layerNameUpper = pl.Layer.ToUpperInvariant();
                if (layerNameUpper.Contains("TOPRAIL")) componentTypeStr = "TopRail";
                else if (layerNameUpper.Contains("BOTTOMRAIL")) componentTypeStr = "BottomRail";
                else if (layerNameUpper.Contains("HANDRAIL")) componentTypeStr = "HandRail";
                else if (layerNameUpper.Contains("INTERMEDIATERAIL")) componentTypeStr = "IntermediateRail";
                else return; 
            }

            string profileName = GetXDataValue(pl, RegAppName, "PROFILE_NAME") ?? "DefaultProfile";
            string material = GetXDataValue(pl, RegAppName, "MATERIAL") ?? "DefaultMaterial";
            string finish = GetXDataValue(pl, RegAppName, "FINISH") ?? "DefaultFinish";
            string description = GetXDataValue(pl, RegAppName, "DESCRIPTION") ?? $"Rail segment: {profileName}";
            string specialNotes = GetXDataValue(pl, RegAppName, "SPECIAL_NOTES") ?? "";
            string ua1 = GetXDataValue(pl, RegAppName, "USER_ATTRIBUTE_1") ?? "";
            string ua2 = GetXDataValue(pl, RegAppName, "USER_ATTRIBUTE_2") ?? "";
            
            double weightDensity = ParseDouble(GetXDataValue(pl, RegAppName, "WEIGHT_DENSITY"));
            double currentSegmentLength = pl.Length;

            BomEntry entryDetails = new BomEntry(componentTypeStr, profileName, description, material, finish, specialNotes, ua1, ua2);

            if (aggregatedBom.TryGetValue(entryDetails.Key, out BomEntry existingEntry))
            {
                existingEntry.InstalledLength += currentSegmentLength;
                existingEntry.Weight += weightDensity * currentSegmentLength; 
                // Quantity for aggregated rail profiles remains 1
            }
            else
            {
                entryDetails.Quantity = 1; 
                entryDetails.InstalledLength = currentSegmentLength;
                entryDetails.Weight = weightDensity * currentSegmentLength;
                aggregatedBom.Add(entryDetails.Key, entryDetails);
            }
        }
        
        private string GetAttributeValue(BlockReference br, string tag, Transaction tr)
        {
            foreach (ObjectId attId in br.AttributeCollection)
            {
                if (attId.IsErased || !attId.IsValid) continue;
                AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead, false, true) as AttributeReference;
                if (attRef != null && attRef.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    return attRef.TextString;
                }
            }
            return null; 
        }

        private string GetXDataValue(Entity ent, string regAppName, string key)
        {
            ResultBuffer rb = ent.GetXData(regAppName);
            if (rb == null) return null;

            var values = rb.AsArray();
            for (int i = 1; i < values.Length; i++) 
            {
                if (values[i].TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                {
                    string valStr = values[i].Value.ToString();
                    if (valStr.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        return valStr.Substring(key.Length + 1);
                    }
                }
            }
            return null;
        }

        private void GenerateAndSaveCsv(List<BomEntry> bomEntries, Editor ed)
        {
            if (bomEntries == null || bomEntries.Count == 0)
            {
                ed.WriteMessage("\nNo data to export for BOM.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"COMPONENTTYPE\",\"PARTNAME/ProfileName\",\"DESCRIPTION\",\"QUANTITY\",\"INSTALLED_LENGTH\",\"MATERIAL\",\"FINISH\",\"WEIGHT\",\"SPECIAL_NOTES\",\"USER_ATTRIBUTE_1\",\"USER_ATTRIBUTE_2\"");

            foreach (BomEntry entry in bomEntries)
            {
                sb.AppendFormat("\"{0}\",\"{1}\",\"{2}\",{3},{4:F2},\"{5}\",\"{6}\",{7:F2},\"{8}\",\"{9}\",\"{10}\"\n",
                    EscapeCsvField(entry.ComponentType),
                    EscapeCsvField(entry.PartNameOrProfile),
                    EscapeCsvField(entry.Description),
                    entry.Quantity,
                    entry.InstalledLength, 
                    EscapeCsvField(entry.Material),
                    EscapeCsvField(entry.Finish),
                    entry.Weight, 
                    EscapeCsvField(entry.SpecialNotes),
                    EscapeCsvField(entry.UserAttribute1),
                    EscapeCsvField(entry.UserAttribute2)
                );
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Save BOM Export",
                FileName = $"RailingBOM_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) 
            };
            
            DialogResult dialogResult = DialogResult.Cancel; // Default to cancel
            // Ensure STA thread for SaveFileDialog
            Application.Invoke(new Action(() => dialogResult = sfd.ShowDialog()));


            if (dialogResult == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    ed.WriteMessage($"\nBOM exported successfully to: {sfd.FileName}");
                }
                catch (System.Exception ex)
                {
                    string errorMsg = $"Error saving BOM file: {ex.Message}";
                    ed.WriteMessage($"\n{errorMsg}");
                    Application.ShowAlertDialog(errorMsg);
                }
            }
            else
            {
                ed.WriteMessage("\nBOM export cancelled by user.");
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field; 
        }
        
        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out result))
            {
                return result;
            }
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nWarning: Could not parse '{value}' as double. Using 0.0.");
            return 0.0;
        }
    }
}
```
