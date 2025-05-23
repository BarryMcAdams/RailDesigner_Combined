using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
// using RailDesigner1; // For RailingDesign - already in namespace
using RailDesigner1.Utils; // For SappoUtilities
using RailDesigner1.Placement; // For PostPlacementLogicWrapper and PlacementInfo
using RailDesigner1.Wrappers; // For RailCreatorPicketPlacementWrapper

namespace RailDesigner1
{
    // Define ComponentType enum here if not globally available or for focused testing
    // This should ideally be in a shared Definitions.cs or similar
    public enum ComponentType
    {
        Post,
        Picket,
        TopRail,
        BottomRail,
        IntermediateRail,
        HandRail,
        Mounting,
        UserDefined, // Fallback or for types not explicitly listed
        // Ensure all types used in GetOrCreateLayerForComponent are here
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }


    public class RailingGeometryGenerator
    {
        public void GenerateRailingGeometry(Autodesk.AutoCAD.DatabaseServices.TransactionManager tm, ObjectId polyId, 
                                            Dictionary<string, ObjectId> componentBlocks, 
                                            Dictionary<string, Dictionary<string, string>> componentAttributes, 
                                            RailingDesign design)
        {
            if (tm == null || polyId == ObjectId.Null || componentBlocks == null || componentAttributes == null || design == null)
            {
                Editor errEd = AcApp.DocumentManager.MdiActiveDocument?.Editor;
                errEd?.WriteMessage("\nError: Null or invalid input to GenerateRailingGeometry.");
                // Consider throwing a more specific custom exception if this method is part of an API
                throw new ArgumentNullException("Input parameters for RailingGeometryGenerator are null or invalid.");
            }

            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                // This case should ideally not be reached if called from an AutoCAD command context
                throw new InvalidOperationException("No active AutoCAD document.");
            }
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Main try-catch for the entire geometry generation process
            try
            {
                using (Transaction tr = tm.StartTransaction())
                {
                    Polyline pathPolyline = tr.GetObject(polyId, OpenMode.ForRead) as Polyline;
                    if (pathPolyline == null)
                    {
                        ed.WriteMessage("\nInvalid polyline selected for railing path. Aborting railing generation.");
                        return; // Exit if polyline is invalid
                    }

                    // Essential component check
                    if (!componentBlocks.ContainsKey("POST") || !componentAttributes.ContainsKey("POST") || 
                        !componentBlocks.ContainsKey("PICKET") || !componentAttributes.ContainsKey("PICKET"))
                    {
                        ed.WriteMessage("\nCritical Error: POST or PICKET component definitions (block and/or attributes) are missing. Aborting generation.");
                        return; // Exit if critical components are undefined
                    }

                    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    // --- Stage 1: Post Placement ---
                    ed.WriteMessage("\nStarting Post Placement...");
                    List<Point3d> actualPostPositions = new List<Point3d>();
                    try
                    {
                        ObjectId postBlockId = componentBlocks["POST"]; 
                        Dictionary<string, string> postAttrs = componentAttributes["POST"]; 

                        RailDesigner1.Placement.PostPlacementLogicWrapper postWrapper = new RailDesigner1.Placement.PostPlacementLogicWrapper();
                        List<PlacementInfo> postPlacements = postWrapper.CalculatePlacements(
                            pathPolyline, design, postBlockId, postAttrs, tr, db);

                        if (postPlacements != null && postPlacements.Count > 0)
                        {
                            foreach (var ppInfo in postPlacements)
                            {
                                InsertBlock(tr, modelSpace, ppInfo.BlockDefinitionId, ppInfo.Position, ppInfo.Orientation, ComponentType.Post, db, ed, ppInfo.Attributes);
                                actualPostPositions.Add(ppInfo.Position);
                            }
                            ed.WriteMessage($"\nSuccessfully placed {postPlacements.Count} posts.");
                        }
                        else
                        {
                            ed.WriteMessage("\nWarning: No valid post positions were calculated or post placement returned no data. Pickets might be affected.");
                            // Continue if posts are optional or if pickets don't solely depend on posts
                        }
                    }
                    catch (System.Exception postEx)
                    {
                        ed.WriteMessage($"\nError during Post Placement: {postEx.Message}\nStackTrace: {postEx.StackTrace}\nPicket placement might be affected or skipped.");
                        // Decide if to continue or abort. For now, we'll let it try pickets if actualPostPositions has any entries.
                    }
                    
                    // --- Stage 2: Picket Placement ---
                    ed.WriteMessage("\nStarting Picket Placement...");
                    try
                    {
                        ObjectId picketBlockId = componentBlocks.ContainsKey("PICKET") ? componentBlocks["PICKET"] : ObjectId.Null;
                        Dictionary<string, string> picketAttributes = componentAttributes.ContainsKey("PICKET") ? componentAttributes["PICKET"] : new Dictionary<string, string>();

                        if (picketBlockId != ObjectId.Null)
                        {
                            if(actualPostPositions.Count == 0 && design.PicketPlacementRequiresPosts) // Assuming a new design property
                            {
                                ed.WriteMessage("\nSkipping Picket Placement: No posts were placed, and picket placement depends on post positions.");
                            }
                            else
                            {
                                RailCreatorPicketPlacementWrapper picketWrapper = new RailCreatorPicketPlacementWrapper();
                                List<PlacementInfo> picketPlacements = picketWrapper.CalculatePicketPlacements(
                                    pathPolyline, design, picketAttributes, picketBlockId, actualPostPositions, tr, db, ed);

                                if (picketPlacements != null && picketPlacements.Count > 0)
                                {
                                    foreach (var placement in picketPlacements)
                                    {
                                        InsertBlock(tr, modelSpace, placement.BlockDefinitionId, placement.Position, placement.Orientation, ComponentType.Picket, db, ed, placement.Attributes);
                                    }
                                    ed.WriteMessage($"\nSuccessfully placed {picketPlacements.Count} pickets.");
                                }
                                else
                                {
                                    ed.WriteMessage("\nWarning: No valid picket positions were calculated or picket placement returned no data.");
                                }
                            }
                        }
                        else
                        {
                             ed.WriteMessage("\nSkipping Picket Placement: PICKET component block not defined.");
                        }
                    }
                    catch (System.Exception picketEx)
                    {
                        ed.WriteMessage($"\nError during Picket Placement: {picketEx.Message}\nStackTrace: {picketEx.StackTrace}");
                        // Continue to rail segments
                    }

                    // --- Stage 3: Rail Segment Generation ---
                    ed.WriteMessage("\nStarting Rail Segment Generation...");
                    try
                    {
                        var railDefinitions = new[] {
                            new { Type = ComponentType.TopRail, Name = "TOPRAIL", HeightProperty = nameof(RailingDesign.RailHeight) },
                            new { Type = ComponentType.BottomRail, Name = "BOTTOMRAIL", HeightProperty = nameof(RailingDesign.BottomClearance) },
                            // Add HandRail, IntermediateRail if defined and attributes exist
                        };

                        foreach (var railDef in railDefinitions)
                        {
                            if (componentAttributes.TryGetValue(railDef.Name, out Dictionary<string, string> currentRailAttributes))
                            {
                                ed.WriteMessage($"\nGenerating {railDef.Name}...");
                                double verticalOffset = 0;
                                if (railDef.HeightProperty == nameof(RailingDesign.RailHeight)) verticalOffset = design.RailHeight;
                                else if (railDef.HeightProperty == nameof(RailingDesign.BottomClearance)) verticalOffset = design.BottomClearance;
                                
                                GenerateRailSegments(tr, modelSpace, pathPolyline, railDef.Type, currentRailAttributes, db, ed, verticalOffset);
                            }
                            else
                            {
                                ed.WriteMessage($"\nSkipping {railDef.Name}: Component attributes not defined.");
                            }
                        }
                    }
                    catch (System.Exception railEx)
                    {
                         ed.WriteMessage($"\nError during Rail Segment Generation: {railEx.Message}\nStackTrace: {railEx.StackTrace}");
                         // Continue to mounts
                    }

                    // --- Stage 4: Mount Placement ---
                    // (Assuming MountPlacementLogic is not yet implemented or is simple)
                    ed.WriteMessage("\nStarting Mount Placement (if defined)...");
                    try
                    {
                        if (componentBlocks.ContainsKey("MOUNT") && componentAttributes.ContainsKey("MOUNT")) 
                        {
                            ObjectId mountBlockId = componentBlocks["MOUNT"];
                            Dictionary<string, string> mountAttributes = componentAttributes["MOUNT"];
                            // Placeholder: Actual mount placement logic would go here.
                            // For now, just indicating it would run.
                            ed.WriteMessage($"\nMount component '{mountAttributes.GetValueOrDefault("PARTNAME", "Mount")}' is defined. (Actual placement logic to be implemented).");
                            // Example: var mountPositions = MountPlacementLogic.CalculateMountPositions(...);
                            // foreach (var mountPos in mountPositions) { InsertBlock(...); }
                        }
                        else
                        {
                            ed.WriteMessage("\nSkipping Mount Placement: MOUNT component not defined.");
                        }
                    }
                    catch(System.Exception mountEx)
                    {
                        ed.WriteMessage($"\nError during Mount Placement: {mountEx.Message}\nStackTrace: {mountEx.StackTrace}");
                    }

                    tr.Commit(); 
                } // End Transaction
                ed.WriteMessage("\nRailing geometry generation process completed.");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acadEx) // Catch specific AutoCAD exceptions
            {
                ed.WriteMessage($"\nAutoCAD Runtime Error during railing generation: {acadEx.ErrorStatus} - {acadEx.Message}\nStackTrace: {acadEx.StackTrace}");
            }
            catch (System.Exception ex) // Catch general .NET exceptions
            {
                ed.WriteMessage($"\nUnexpected General Error during railing generation: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void GenerateRailSegments(Transaction tr, BlockTableRecord modelSpace, Polyline pathPolyline, 
                                          ComponentType railType, Dictionary<string, string> railAttributes, 
                                          Database db, Editor ed, double verticalOffset = 0.0)
        {
            // Ensure pathPolyline is not null (already checked in calling method, but good practice)
            if (pathPolyline == null)
            {
                ed.WriteMessage($"\nError in GenerateRailSegments: Path polyline is null for {railType}.");
                return; // Or throw ArgumentNullException
            }

            int numVertices = pathPolyline.NumberOfVertices;
            if (numVertices < 2)
            {
                ed.WriteMessage($"\nPolyline for {railType} must have at least 2 vertices.");
                return;
            }

            // Get layer for this rail type
            string layerName = RailDesigner1.Utils.SappoUtilities.GetOrCreateLayerForComponent(db, ed, railType);
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            ObjectId railLayerId = db.Clayer; 
            if (lt.Has(layerName))
            {
                railLayerId = lt[layerName];
            }
            else
            {
                ed.WriteMessage($"\nWarning: Layer '{layerName}' could not be found or created for {railType}. Using current layer.");
            }

            // Create polyline segments for the rail
            for (int i = 0; i < numVertices - 1; i++)
            {
                Point3d startPoint = pathPolyline.GetPoint3dAt(i);
                Point3d endPoint = pathPolyline.GetPoint3dAt(i + 1);
                double segmentLength = startPoint.DistanceTo(endPoint);

                Polyline railSegment = new Polyline(); // Create a new polyline for each segment
                railSegment.AddVertexAt(0, new Point2d(startPoint.X, startPoint.Y), 0, 0, 0);
                railSegment.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0);
                railSegment.Normal = Vector3d.ZAxis; 
                railSegment.Elevation = startPoint.Z + verticalOffset; // Apply vertical offset
                railSegment.LayerId = railLayerId;
                
                // XData Population for the rail segment
                var xDataAttributes = new Dictionary<string, object>
                {
                    { "COMPONENTTYPE", railType.ToString() },
                    { "PROFILE_NAME", railAttributes.GetValueOrDefault("PARTNAME", "DefaultProfile") }, // PARTNAME from component definition used as PROFILE_NAME
                    { "MATERIAL", railAttributes.GetValueOrDefault("MATERIAL", "DefaultMaterial") },
                    { "FINISH", railAttributes.GetValueOrDefault("FINISH", "DefaultFinish") },
                    { "WEIGHT_DENSITY", railAttributes.GetValueOrDefault("WEIGHT_DENSITY", "0.0") }, 
                    { "INSTALLED_LENGTH", segmentLength } 
                };
                AttachXData(tr, railSegment, xDataAttributes, db);

                modelSpace.AppendEntity(railSegment);
                tr.AddNewlyCreatedDBObject(railSegment, true);
            }
            ed.WriteMessage($"\nSuccessfully generated {railType} segments with vertical offset {verticalOffset}.");
        }

        // CalculateSegmentLength is no longer strictly needed here as length is calculated per segment in GenerateRailSegments
        // private double CalculateSegmentLength(Point3d start, Point3d end) { return start.DistanceTo(end); }


        private void AttachXData(Transaction tr, Entity entity, Dictionary<string, object> attributes, Database dbForRegApp)
        {
            const string AppName = "RAIL_DESIGN_APP"; 
            RegAppTable rat = (RegAppTable)tr.GetObject(dbForRegApp.RegAppTableId, OpenMode.ForRead);
            if (!rat.Has(AppName))
            {
                try
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord { Name = AppName };
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nError registering XData application name '{AppName}': {ex.Message}");
                    return; 
                }
                finally
                {
                    if (rat.IsWriteEnabled) rat.DowngradeOpen();
                }
            }

            // Build the XData result buffer
            ResultBuffer rb = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName));
            foreach (var attr in attributes)
            {
                if (attr.Value is string strVal)
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{strVal}"));
                else if (attr.Value is double dblVal)
                    // Storing key as part of value for simplicity in XData string; consider TypedValue.Real for actual double
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{dblVal.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
                else if (attr.Value is int intVal)
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{intVal.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
                else 
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{attr.Value?.ToString() ?? "null"}"));
            }
            entity.XData = rb;
        }

        private void InsertBlock(Transaction tr, BlockTableRecord modelSpace, ObjectId blockId, Point3d position, 
                                 Vector3d orientation, ComponentType componentType, Database db, Editor ed, 
                                 Dictionary<string, string> attributesToSet)
        {
            // Validate BlockId
            if (blockId == ObjectId.Null || blockId.IsErased || !blockId.IsValid)
            {
                ed.WriteMessage($"\nError inserting block for {componentType}: BlockId is null, erased, or invalid.");
                return;
            }
            
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (!bt.Has(blockId))
            {
                ed.WriteMessage($"\nError inserting block for {componentType}: BlockId {blockId} does not exist in the BlockTable.");
                return;
            }

            // Get layer for this component type
            string layerName = RailDesigner1.Utils.SappoUtilities.GetOrCreateLayerForComponent(db, ed, componentType);
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            ObjectId layerId = db.Clayer; // Default to current layer

            if (lt.Has(layerName))
            {
                layerId = lt[layerName];
            }
            else
            {
                ed.WriteMessage($"\nWarning: Layer '{layerName}' could not be found or created for {componentType}. Using current layer.");
            }

            // Create and configure the BlockReference
            BlockReference br = new BlockReference(position, blockId) { LayerId = layerId };

            double angle = 0.0;
            if (orientation.Length > Tolerance.Global.EqualPoint && !orientation.IsParallelTo(Vector3d.ZAxis, Tolerance.Global.EqualPoint))
            {
                Vector2d orientationXY = new Vector2d(orientation.X, orientation.Y);
                if (orientationXY.Length > Tolerance.Global.EqualPoint)
                {
                    angle = orientationXY.AngleTo(Vector2d.XAxis); // Angle with respect to X-axis for rotation
                }
            }
            br.Rotation = angle;

            modelSpace.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);

            // Add and set attributes for the BlockReference
            BlockTableRecord blockDef = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
            if (blockDef == null)
            {
                ed.WriteMessage($"\nError inserting block for {componentType}: Could not open block definition (BlockId: {blockId}). Attributes cannot be set.");
                return;
            }

            if (blockDef.HasAttributeDefinitions)
            {
                foreach (ObjectId idInBTR in blockDef) 
                {
                    if (idInBTR.ObjectClass.DxfName.Equals("ATTDEF", StringComparison.OrdinalIgnoreCase))
                    {
                        AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(idInBTR, OpenMode.ForRead);
                        if (!attDef.Constant) // Process only non-constant attributes
                        {
                            using (AttributeReference attRef = new AttributeReference())
                            {
                                attRef.SetAttributeFromBlock(attDef, br.BlockTransform); // Copy properties from definition
                                string attTag = attDef.Tag.ToUpperInvariant(); 
                                if (attributesToSet != null && attributesToSet.TryGetValue(attTag, out string value))
                                {
                                    attRef.TextString = value; // Set specific value if provided
                                }
                                // If not in attributesToSet, attRef retains default value from attDef.TextString (applied by SetAttributeFromBlock)
                                
                                br.AttributeCollection.AppendAttribute(attRef);
                                tr.AddNewlyCreatedDBObject(attRef, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
