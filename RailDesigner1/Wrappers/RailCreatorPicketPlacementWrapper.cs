using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using RailDesigner1.Placement; // For PlacementInfo
using RailCreator; // For original PicketGenerator and RailingDesign

// Assuming ComponentType enum is defined in a shared location, e.g., RailDesigner1.Placement or a common Definitions file.
// Example:
// namespace RailDesigner1 { public enum ComponentType { Post, Picket, Rail, Mounting, UserDefined, IntermediateRail } }


namespace RailDesigner1.Wrappers
{
    public class RailCreatorPicketPlacementWrapper
    {
        public List<PlacementInfo> CalculatePicketPlacements(
            Polyline pathPolyline,
            RailDesigner1.RailingDesign designParams, // RailDesigner1 version of RailingDesign
            Dictionary<string, string> picketAttributes, // Attributes of the defined PICKET component
            ObjectId picketBlockId, // ObjectId of the "PICKET" BlockTableRecord for RailDesigner1
            List<Point3d> postPositions, // Calculated post positions
            Transaction tr,
            Database db,
            Editor ed)
        {
            var placements = new List<PlacementInfo>();
            string tempLayerName = "TEMP_PICKET_GEN_" + Guid.NewGuid().ToString("N");
            ObjectId tempLayerId = ObjectId.Null;
            ObjectId originalLayerId = db.Clayer; // Store original current layer
            LayerTableRecord tempLayer = null;

            try
            {
                // 1. Create a temporary layer
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                if (lt.Has(tempLayerName)) 
                {
                    ed.WriteMessage($"\nTemporary layer {tempLayerName} already exists. This should ideally not happen. Attempting to use it or create another variant if necessary.");
                    // For simplicity, if it exists, we might try to use it, though ideally, it should be unique.
                    // Or, append another GUID / counter. For this implementation, we'll assume collision is rare
                    // and proceed. If Add() fails, it will be caught.
                }
                
                tempLayer = new LayerTableRecord
                {
                    Name = tempLayerName,
                    IsPlotted = false // Don't plot this layer
                };
                tempLayerId = lt.Add(tempLayer);
                tr.AddNewlyCreatedDBObject(tempLayer, true);
                db.Clayer = tempLayerId; // Set temporary layer as current

                // 2. Adapt RailingDesign
                RailCreator.RailingDesign rooDesign = AdaptRailingDesign(designParams, picketAttributes, ed);

                // 3. Call original PicketGenerator
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                
                // Call the main picket placement logic
                // This method is expected to draw entities (lines, circles, etc.) on the current layer (our tempLayerId)
                RailCreator.PicketGenerator.PlacePickets(pathPolyline, rooDesign, postPositions, modelSpace, tr);

                // Optional: Handle other types if necessary based on rooDesign.PicketType or other properties
                // These are commented out as per the prompt's structure, assuming PlacePickets is the primary one.
                // if (rooDesign.PicketType.ToLower().Contains("special")) 
                // {
                //     RailCreator.PicketGenerator.PlaceSpecialPickets(pathPolyline, rooDesign, postPositions, modelSpace, tr);
                // }
                // if (rooDesign.PicketType.ToLower().Contains("deco"))
                // {
                //     RailCreator.PicketGenerator.PlaceDecorativePickets(pathPolyline, rooDesign, postPositions, modelSpace, tr);
                // }

                // 4. Extract PlacementInfo from entities drawn on the temporary layer
                var newlyDrawnObjectIds = new List<ObjectId>();
                foreach (ObjectId objId in modelSpace) 
                {
                    Entity ent = tr.GetObject(objId, OpenMode.ForRead, false, false) as Entity; // Open brief, no error if not an entity
                    if (ent != null && ent.LayerId == tempLayerId)
                    {
                        newlyDrawnObjectIds.Add(objId);
                    }
                }

                foreach (ObjectId drawnObjId in newlyDrawnObjectIds)
                {
                    Entity drawnEnt = tr.GetObject(drawnObjId, OpenMode.ForRead) as Entity; 
                    if (drawnEnt != null)
                    {
                        Point3d position = Point3d.Origin;
                        Vector3d orientation = Vector3d.XAxis; // Default

                        if (drawnEnt is Line line)
                        {
                            position = line.StartPoint + (line.EndPoint - line.StartPoint) / 2.0;
                            orientation = (line.EndPoint - line.StartPoint).GetNormal();
                        }
                        else if (drawnEnt is Circle circle)
                        {
                            position = circle.Center;
                            Point3d closestPtOnPoly = pathPolyline.GetClosestPointTo(position, false);
                            double param = pathPolyline.GetParameterAtPoint(closestPtOnPoly);
                            orientation = pathPolyline.GetFirstDerivative(param).GetNormal();
                        }
                        else if (drawnEnt is Polyline pl) 
                        {
                            if (pl.NumberOfVertices > 0) 
                            {
                                // Use geometric center for polylines representing pickets
                                position = pl.GeometricExtents.MinPoint + (pl.GeometricExtents.MaxPoint - pl.GeometricExtents.MinPoint) / 2.0;
                                Point3d closestPtOnPoly = pathPolyline.GetClosestPointTo(position, false);
                                double param = pathPolyline.GetParameterAtPoint(closestPtOnPoly);
                                orientation = pathPolyline.GetFirstDerivative(param).GetNormal();
                            }
                        }
                        // TODO: Extend for other entity types drawn by PicketGenerator if necessary (e.g., Arcs, Blocks)

                        placements.Add(new PlacementInfo
                        {
                            Type = ComponentType.Picket, 
                            BlockDefinitionId = picketBlockId,
                            Position = position,
                            Orientation = orientation,
                            Attributes = new Dictionary<string, string>(picketAttributes) // Pass a copy of original attributes
                        });
                    }
                }

                // 5. Erase drawn entities and delete temporary layer
                foreach (ObjectId objIdToErase in newlyDrawnObjectIds)
                {
                    Entity entToErase = tr.GetObject(objIdToErase, OpenMode.ForWrite);
                    entToErase.Erase();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in RailCreatorPicketPlacementWrapper: {ex.Message}\n{ex.StackTrace}");
            }
            finally // Ensure cleanup happens
            {
                 // Revert current layer
                if (db.Clayer == tempLayerId && tempLayerId != ObjectId.Null) {
                    db.Clayer = originalLayerId;
                }

                // Delete the temporary layer itself
                if (tempLayerId != ObjectId.Null && !tempLayerId.IsErased) {
                    // We need to re-fetch the layer if the transaction was aborted or committed by an inner operation.
                    // However, the 'tr' here is the one passed in, so it should still be valid if the caller manages it.
                    try {
                        // Check if 'tempLayer' is null or disposed, if so, get it from tempLayerId
                        if (tempLayer == null || tempLayer.IsDisposed) {
                             tempLayer = tr.GetObject(tempLayerId, OpenMode.ForRead, false, true) as LayerTableRecord;
                        }

                        if (tempLayer != null && !tempLayer.IsErased) {
                            // It's possible the transaction was committed or aborted, so we might need a new one for cleanup.
                            // For this subtask, we assume 'tr' is still active or the caller handles transaction state.
                            // If 'tr' is not active, GetObject will fail.
                            if (!tempLayer.IsWriteEnabled) tempLayer.UpgradeOpen();
                            tempLayer.Erase();
                        }
                    } catch (System.Exception cleanupEx) {
                         ed.WriteMessage($"\nError during temporary layer cleanup: {cleanupEx.Message}");
                    }
                }
            }
            return placements;
        }

        private RailCreator.RailingDesign AdaptRailingDesign(
            RailDesigner1.RailingDesign designParams, 
            Dictionary<string, string> picketAttributes,
            Editor ed)
        {
            RailCreator.RailingDesign rooDesign = new RailCreator.RailingDesign();
            
            rooDesign.RailHeight = designParams.RailHeight;
            // Assuming RailDesigner1.RailingDesign has TopCapHeight, or it's handled if null in RailCreator
            rooDesign.TopCapHeight = designParams.TopCapHeight; 

            rooDesign.PicketType = picketAttributes.GetValueOrDefault("PICKETTYPE", designParams.PicketType ?? "Vertical");
            rooDesign.PicketSize = picketAttributes.GetValueOrDefault("PICKETSIZE", designParams.PicketSize ?? "0.75x0.75");
            
            // RailCreator.PicketGenerator uses PostSize for certain calculations (e.g. available space)
            rooDesign.PostSize = designParams.PostSize ?? "2x2"; 

            string decoWidthStr = picketAttributes.GetValueOrDefault("DECORATIVEWIDTH", null);
            if (double.TryParse(decoWidthStr, out double decoWidthParsed))
            {
                 rooDesign.DecorativeWidth = decoWidthParsed;
            }
            else if (designParams.DecorativeWidth.HasValue)
            {
                 rooDesign.DecorativeWidth = designParams.DecorativeWidth.Value;
            }
            // else rooDesign.DecorativeWidth will be its default (e.g., 0.0)

            // ed.WriteMessage($"\nAdapted RooDesign: PicketType='{rooDesign.PicketType}', PicketSize='{rooDesign.PicketSize}', PostSize='{rooDesign.PostSize}', DecoWidth='{rooDesign.DecorativeWidth}'");
            return rooDesign;
        }
    }

    // The prompt mentions DictionaryExtensions.GetValueOrDefault can be added if not globally available.
    // It's good practice to have it in a shared utility class.
    // For this subtask, if it's not assumed to be available elsewhere, it could be added here (potentially commented out).
    // public static class DictionaryExtensions
    // {
    //     public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    //     {
    //         return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
    //     }
    // }
}
```
