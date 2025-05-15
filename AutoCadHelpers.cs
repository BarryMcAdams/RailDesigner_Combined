using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using System;

namespace RailDesigner1
{
    public static class AutoCadHelpers
    {
        public static BlockTable GetBlockTable(Transaction tr)
        {
            if (tr == null)
                throw new ArgumentNullException(nameof(tr));

            var db = HostApplicationServices.WorkingDatabase;
            return (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        }

        public static LayerTable GetLayerTable(Transaction tr)
        {
            if (tr == null)
                throw new ArgumentNullException(nameof(tr));

            var db = HostApplicationServices.WorkingDatabase;
            return (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        }

        // Additional helpers can be added as needed
        public static ObjectId GetOrCreateLayer(Transaction tr, string layerName, Database db)
        {
            ObjectId layerId = ObjectId.Null;
            // Ensure db is not null
            if (db == null)
            {
                 // This could happen if called with a null db.
                 // Consider throwing an ArgumentNullException or getting db from HostApplicationServices.
                 // For now, let's assume db is valid. If not, GetObject below will fail.
                 // One option: if(db == null) db = HostApplicationServices.WorkingDatabase;
                 // but better if the caller always provides a valid db.
                 throw new System.ArgumentNullException(nameof(db), "Database cannot be null in GetOrCreateLayer.");
            }


            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
            {
                layerId = lt[layerName];
            }
            else
            {
                try
                {
                    lt.UpgradeOpen();
                    LayerTableRecord newLayer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 7) // Default white
                    };
                    layerId = lt.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }
                // catch specific exceptions if necessary
                finally // Ensure we downgrade if we upgraded and it's still write-enabled
                {
                    if (lt.IsWriteEnabled) // Check before downgrading
                    {
                        lt.DowngradeOpen();
                    }
                }
            }
            return layerId;
        }
    }
}