// Plugin.cs
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
// using System.Windows.Forms; // Keep if you plan to use WinForms dialogs, otherwise optional for current stubs
using RailDesigner1.Utils;    // For Error logging
using System;                 // For System.Exception

// This attribute registers the commands in this class with AutoCAD.
// It should be here or in AssemblyInfo.cs, but not both for the same class.
[assembly: CommandClass(typeof(RailDesigner1.Plugin))]

namespace RailDesigner1
{
    // Ensure this enum definition is accessible, might need to be moved or referenced correctly if not directly in Plugin.cs scope
    public enum ComponentType // Centralized enum
    {
        Post,
        Picket,
        TopCap,
        BottomRail,
        IntermediateRail,
        BasePlate,
        Mounting,
        UserDefined // For generic user-defined blocks
    }

    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nRailDesigner1 Plugin Loaded. Commands: RAIL_GENERATE, RAIL_EXPORT_BOM, RAIL_DEFINE_COMPONENT.\n");
            // Initialize logger
            ErrorLogger.Initialize("RailDesigner1_Log.txt");
            ErrorLogger.LogMessage("Plugin Initialized.");
        }

        public void Terminate()
        {
            ErrorLogger.LogMessage("Plugin Terminating.");
            ErrorLogger.Close();
        }

        [CommandMethod("RAIL_GENERATE", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void RailGenerate()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ErrorLogger.LogError("RAIL_GENERATE: No active document.");
                return;
            }
            try
            {
                // Assuming RailGenerateCommandHandler is in RailDesigner1 namespace
                var handler = new RailGenerateCommandHandler();
                handler.Execute();
            }
            // --- FIX CS0104: Specify System.Exception ---
            catch (System.Exception ex)
            {
                string errorMsg = "Error during RAIL_GENERATE execution.";
                ErrorLogger.LogError(errorMsg, ex);
                doc.Editor.WriteMessage($"\n{errorMsg} See log for details. {ex.Message}\n");
            }
        }

        [CommandMethod("RAIL_DEFINE_COMPONENT", CommandFlags.Modal)]
        public static void DefineComponent()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ErrorLogger.LogError("RAIL_DEFINE_COMPONENT: No active document.");
                return;
            }

            try
            {
                // Assuming ComponentDefiner is in RailDesigner1 namespace
                var definer = new ComponentDefiner(doc.Database, doc.Editor);
                definer.DefineComponentLoop();
            }
            // --- FIX CS0104: Specify System.Exception ---
            catch (System.Exception ex)
            {
                string errorMsg = "Error during RAIL_DEFINE_COMPONENT execution.";
                ErrorLogger.LogError(errorMsg, ex);
                doc.Editor.WriteMessage($"\n{errorMsg} See log for details. {ex.Message}\n");
            }
        }

        [CommandMethod("RAIL_EXPORT_BOM", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public static void RailExportBom()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ErrorLogger.LogError("RAIL_EXPORT_BOM: No active document.");
                return;
            }
            try
            {
                // Assuming BomExporter is in RailDesigner1 namespace
                var exporter = new BomExporter();
                exporter.ExportBom();
            }
            // --- FIX CS0104: Specify System.Exception ---
            catch (System.Exception ex)
            {
                string errorMsg = "Error during RAIL_EXPORT_BOM execution.";
                ErrorLogger.LogError(errorMsg, ex);
                doc.Editor.WriteMessage($"\n{errorMsg} See log for details. {ex.Message}\n");
            }
        }
    }
}