using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using RailDesigner1; // For RailingGeometryGenerator, RailingDesign, TransactionManagerWrapper, etc.

namespace RailDesigner1
{
    public class RailGenerateCommand
    {
        [CommandMethod("RAIL_GENERATE")]
        public void RailGenerate()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                doc.Editor.WriteMessage("\nNo active document.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityResult per = ed.GetEntity("\nSelect a polyline for the railing path: ");
            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo polyline selected.");
                return;
            }

            ObjectId polyId = per.ObjectId;

            // Placeholder for component blocks and design data  
            Dictionary<string, ObjectId> componentBlocks = new Dictionary<string, ObjectId>(); // Populate this in a real scenario  
            RailingDesign design = new RailingDesign(); // Assume this is defined elsewhere  

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RailingGeometryGenerator generator = new RailingGeometryGenerator();
                generator.GenerateRailingGeometry(db.TransactionManager, polyId, componentBlocks, design);
                tr.Commit();
            }
        }
    }
}
