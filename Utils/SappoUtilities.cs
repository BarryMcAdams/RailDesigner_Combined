// RailDesigner1.Utils.SappoUtilities.cs
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;

namespace RailDesigner1.Utils
{
    public static class SappoUtilities
    {
        public static string GetOrCreateLayerForComponent(Database db, Editor ed, ComponentType componentType)
        {
            string layerName = $"L-RAIL-{componentType.ToString().ToUpper()}";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = layerName;
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7); // White color, for example
                    ed.WriteMessage($"\nLayer '{layerName}' created.");
                }
                else
                {
                    ed.WriteMessage($"\nLayer '{layerName}' already exists.");
                }
                tr.Commit();
                return layerName;
            }
        }
    }
}