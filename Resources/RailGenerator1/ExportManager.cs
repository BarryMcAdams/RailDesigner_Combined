using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;  // Added for file I/O operations
using System.Linq; // Added for Select/QuoteCSVField
using System.Text; // Added for QuoteCSVField logic if more complex quoting needed

// Explicitly reference AutoCAD Application to avoid ambiguity with System.Windows.Forms.Application
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;


namespace RailGenerator1
{
    public class ExportManager
    {
        // Method to extract BOM data from RailingAssemblyBlock
        public static Dictionary<string, object> ExtractBOMData()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument; // Corrected: Use alias
            if (doc == null) return null; // Handle case where no document is active
            var ed = doc.Editor;
            var db = doc.Database;

            // Prompt user to select the RailingAssemblyBlock
            var peo = new PromptEntityOptions("\nSelect RailingAssemblyBlock: ");
            peo.SetRejectMessage("\nSelection must be a Block Reference.");
            peo.AddAllowedClass(typeof(BlockReference), true); // Ensure only BlockReferences are selected
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo block selected. Operation cancelled.");
                return null; // Or throw an exception, but return null for simplicity
            }

            Dictionary<string, object> bomData = null;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var br = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (br == null)
                    {
                        ed.WriteMessage("\nFailed to read selected block reference.");
                        tr.Commit(); // Commit read-only transaction
                        return null;
                    }

                    // Optional: Check if the block is named "RailingAssemblyBlock" or similar
                    // if (!br.Name.StartsWith("RailingAssemblyBlock")) // Example check
                    // {
                    //     ed.WriteMessage($"\nSelected block '{br.Name}' is not the expected Railing Assembly Block type.");
                    //     tr.Commit();
                    //     return null;
                    // }

                    // Extract attributes
                    var attributes = new Dictionary<string, object>();
                    if (br.AttributeCollection != null && br.AttributeCollection.Count > 0)
                    {
                        foreach (ObjectId attrId in br.AttributeCollection)
                        {
                            if (!attrId.IsErased && !attrId.IsNull)
                            {
                                var attrRef = tr.GetObject(attrId, OpenMode.ForRead) as AttributeReference;
                                if (attrRef != null)
                                {
                                    string tag = attrRef.Tag;
                                    object value = attrRef.TextString; // Or handle based on tag type
                                    attributes[tag] = value;
                                }
                            }
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nSelected block has no attributes.");
                        // Decide if this is an error or just an empty data case
                    }

                    // Extract XData (Example: Looking for specific AppName)
                    string appName = "RAIL_GENERATOR"; // Example AppName
                    var xdata = br.GetXDataForApplication(appName);
                    if (xdata != null)
                    {
                        var xdataDict = new Dictionary<string, string>();
                        var values = xdata.AsArray();
                        // Simple key-value pair parsing assumption
                        for (int i = 1; i < values.Length; i += 2) // Start from 1 to skip AppName TypedValue(1001)
                        {
                            if (i + 1 < values.Length && values[i].TypeCode == (int)DxfCode.ExtendedDataAsciiString && values[i + 1].TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                            {
                                string key = values[i].Value.ToString();
                                string value = values[i + 1].Value.ToString();
                                xdataDict[key] = value;
                            }
                        }
                        // Add relevant XData to attributes or handle separately
                        if (xdataDict.Count > 0)
                        {
                            attributes["XDATA_" + appName] = string.Join(";", xdataDict.Select(kvp => $"{kvp.Key}={kvp.Value}")); // Example serialization
                        }
                    }

                    // Combine attributes and potentially XData into bomData
                    bomData = new Dictionary<string, object>(attributes);
                    // Add any additional computed fields if needed

                    tr.Commit(); // Commit the transaction
                }
                catch (System.Exception ex) // Qualify System.Exception if Autodesk.AutoCAD.Runtime is used extensively
                {
                    ed.WriteMessage($"\nError extracting BOM data: {ex.Message}");
                    tr.Abort(); // Rollback on error
                    return null;
                }
            }
            return bomData;
        }


        [CommandMethod("RAIL_EXPORT_DATA")]
        public static void ExportDataToCSV()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument; // Corrected: Use alias
            if (doc == null)
            {
                AcadApplication.ShowAlertDialog("No active document."); // Corrected: Use alias
                return;
            }
            var ed = doc.Editor;

            var bomData = ExtractBOMData();
            if (bomData == null || bomData.Count == 0)
            {
                ed.WriteMessage("\nNo BOM data extracted or data is empty. Export cancelled.");
                return;
            }

            // Prompt for file path to save CSV, mitigating SAPPO :UserInputError
            var pfo = new PromptSaveFileOptions("Specify save location for CSV file:")
            {
                Filter = "CSV files (*.csv)|*.csv",
                DialogCaption = "Save BOM Data As CSV"
            };

            var pfr = ed.GetFileNameForSave(pfo);
            if (pfr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nExport cancelled by user.");
                return;
            }
            string filePath = pfr.StringResult;

            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write header row with proper CSV quoting
                    var keys = new List<string>(bomData.Keys);
                    writer.WriteLine(string.Join(",", keys.Select(k => QuoteCSVField(k))));

                    // Write data row with proper CSV quoting
                    var values = keys.Select(key => bomData[key]?.ToString() ?? string.Empty).ToArray();
                    writer.WriteLine(string.Join(",", values.Select(v => QuoteCSVField(v))));
                }
                ed.WriteMessage($"\nCSV exported successfully to {filePath}");
            }
            catch (IOException ex) // Handle SAPPO :FileIOException with error messaging
            {
                ed.WriteMessage($"\nError saving file: {ex.Message}. SAPPO :Problem :FileIOException mitigated.");
                AcadApplication.ShowAlertDialog($"Error writing to file: {ex.Message}"); // Corrected: Use alias
            }
            // CORRECTION: Explicitly use System.Exception here to resolve ambiguity CS0104
            catch (System.Exception ex) // Catch other unexpected errors
            {
                ed.WriteMessage($"\nAn unexpected error occurred during export: {ex.Message}");
                AcadApplication.ShowAlertDialog($"An unexpected error occurred: {ex.Message}"); // Corrected: Use alias
            }
        }

        // Helper method to quote CSV fields, ensuring proper formatting and handling special characters
        private static string QuoteCSVField(string field)
        {
            if (field == null) return "\"\""; // Represent null as empty quoted string
            // Check if quoting is necessary
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                // Escape existing double quotes by doubling them
                field = field.Replace("\"", "\"\"");
                // Enclose the entire field in double quotes
                return $"\"{field}\"";
            }
            // No quoting needed
            return field;
        }

        // Other methods can be added here
    }
}