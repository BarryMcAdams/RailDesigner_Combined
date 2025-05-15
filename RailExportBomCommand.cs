using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace RailDesigner1
{
    public class RailExportBomCommand
    {
        [CommandMethod("RAIL_EXPORT_BOM")]
        public void RailExportBom()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                doc.Editor.WriteMessage("\nNo active document.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                BomExporter bomExporter = new BomExporter();
                bomExporter.ExportBom();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError executing RAIL_EXPORT_BOM: {ex.Message}");
                // Optionally log the error more formally
            }
        }
    }
}