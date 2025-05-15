using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace RailCreator
{
    public static class Generator
    {
        public static void GenerateRailing(RailingDesign design)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var polyline = SelectPolyline(ed);
            if (polyline == null)
            {
                ed.WriteMessage("\nPolyline selection canceled.");
                return;
            }

            // Step 1: Place posts
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var postPositions = PostGenerator.CalculatePostPositions(polyline, design, btr, tr);

                // Step 2: Draw TopCap and Bottom Rail
                RailGenerator.DrawRails(polyline, design, btr, tr);

                // Step 3: Place Pickets
                if (design.PicketType == "Vertical" || design.PicketType == "Horizontal")
                {
                    PicketGenerator.PlacePickets(polyline, design, postPositions, btr, tr);
                }
                else if (design.PicketType == "GLASS" || design.PicketType == "MESH" || design.PicketType == "PERF")
                {
                    PicketGenerator.PlaceSpecialPickets(polyline, design, postPositions, btr, tr);
                }
                else if (design.PicketType == "DECO")
                {
                    PicketGenerator.PlaceDecorativePickets(polyline, design, postPositions, btr, tr);
                }

                tr.Commit();
            }

            ed.WriteMessage("\nRailing generated successfully.");
        }

        private static Polyline SelectPolyline(Editor ed)
        {
            var options = new PromptEntityOptions("\nSelect a 2D polyline: ");
            options.SetRejectMessage("\nSelected object must be a 2D polyline.");
            options.AddAllowedClass(typeof(Polyline), true);

            var result = ed.GetEntity(options);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }

            using (var tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                var polyline = (Polyline)tr.GetObject(result.ObjectId, OpenMode.ForRead);
                if (polyline.Elevation != 0 || polyline.Normal != Vector3d.ZAxis)
                {
                    ed.WriteMessage("\nSelected polyline must be in the XY plane.");
                    return null;
                }

                tr.Commit();
                return polyline;
            }
        }
    }
}