// RailGenerateCommandHandler.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using RailDesigner1.Utils; // For ErrorLogger
using System.Collections.Generic;

namespace RailDesigner1
{
    public class RailGenerateCommandHandler
    {
        public void Execute()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ErrorLogger.LogMessage("No active document found.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Prompt user to select a polyline for the railing path
            PromptEntityResult per = ed.GetEntity("\nSelect a polyline for the railing path: ");
            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo polyline selected.");
                ErrorLogger.LogMessage("No polyline selected by user.");
                return;
            }

            ObjectId polyId = per.ObjectId;

            // Placeholder for component blocks and design data
            Dictionary<string, ObjectId> componentBlocks = new Dictionary<string, ObjectId>(); // Populate this in a real scenario
            RailingDesign design = new RailingDesign(); // Assume this is defined elsewhere with user inputs

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    RailingGeometryGenerator generator = new RailingGeometryGenerator();
                    generator.GenerateRailingGeometry(db.TransactionManager, polyId, componentBlocks, design);
                    tr.Commit();
                    ed.WriteMessage("\nRailing geometry generated successfully.");
                    ErrorLogger.LogMessage("Railing geometry generated successfully for polyline ID: " + polyId.ToString());
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    ed.WriteMessage("\nError generating railing geometry: " + ex.Message);
                    ErrorLogger.LogMessage("Error in railing geometry generation: " + ex.ToString());
                }
            }
        }
    }
}