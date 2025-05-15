using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using RailDesigner1; // For RailingDesign, ComponentType, TransactionManagerWrapper, etc.
using RailDesigner1.Utils; // For SappoUtilities

namespace RailDesigner1
{
    public class RailingGeometryGenerator
    {
        public void GenerateRailingGeometry(Autodesk.AutoCAD.DatabaseServices.TransactionManager tm, ObjectId polyId, Dictionary<string, ObjectId> componentBlocks, RailingDesign design)
        {
            if (tm == null || polyId == ObjectId.Null || componentBlocks == null || design == null)
            {
                Editor errEd = AcApp.DocumentManager.MdiActiveDocument?.Editor;
                errEd?.WriteMessage("\nError: Null input to GenerateRailingGeometry.");
                throw new ArgumentNullException("Input parameters for RailingGeometryGenerator are null or invalid.");
            }

            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                throw new InvalidOperationException("No active AutoCAD document.");
            }
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                using (Transaction tr = tm.StartTransaction())
                {
                    Polyline pathPolyline = tr.GetObject(polyId, OpenMode.ForRead) as Polyline;
                    if (pathPolyline == null)
                    {
                        ed.WriteMessage("\nInvalid polyline. Aborting railing generation.");
                        return;
                    }

                    if (!componentBlocks.ContainsKey("Post") || !componentBlocks.ContainsKey("Picket") || !componentBlocks.ContainsKey("Rail"))
                    {
                        ed.WriteMessage("\nMissing required components (Post, Picket, or Rail blocks). Aborting generation.");
                        return;
                    }

                    ObjectId postBlockId = componentBlocks["Post"];
                    ObjectId picketBlockId = componentBlocks["Picket"];

                    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    var postPositions = PostPlacementLogicWrapper.CalculatePostPositions(pathPolyline, design.PostSpacing);
                    var postOrientations = PostPlacementLogicWrapper.CalculatePostOrientations(pathPolyline, postPositions);
                    if (postPositions != null && postOrientations != null && postPositions.Count == postOrientations.Count)
                    {
                        for (int i = 0; i < postPositions.Count; i++)
                        {
                            InsertBlock(tr, modelSpace, postBlockId, postPositions[i], postOrientations[i], ComponentType.Post, db, ed);
                        }
                        ed.WriteMessage($"\nPlaced {postPositions.Count} posts.");
                    }
                    else
                    {
                        ed.WriteMessage("\nWarning: Post position or orientation data is invalid or mismatched.");
                    }

                    var picketPositions = PicketPlacementLogic.CalculatePicketPositions(pathPolyline, design, modelSpace, tr);
                    var picketOrientations = PicketPlacementLogic.CalculatePicketOrientations(pathPolyline, picketPositions);
                    if (picketPositions != null && picketOrientations != null && picketPositions.Count == picketOrientations.Count)
                    {
                        for (int i = 0; i < picketPositions.Count; i++)
                        {
                            InsertBlock(tr, modelSpace, picketBlockId, picketPositions[i], picketOrientations[i], ComponentType.Picket, db, ed);
                        }
                        ed.WriteMessage($"\nPlaced {picketPositions.Count} pickets (using Block inserts).");
                    }
                    else
                    {
                        ed.WriteMessage("\nWarning: Picket position or orientation data is invalid or mismatched.");
                    }

                    GenerateRailSegments(tr, modelSpace, pathPolyline, componentBlocks, db, ed);

                    if (componentBlocks.ContainsKey("Mount"))
                    {
                        // Placeholder for MountPlacementLogic
                        var mountPositions = new List<Point3d>();
                        var mountOrientations = new List<Vector3d>();
                        if (mountPositions != null && mountOrientations != null && mountPositions.Count == mountOrientations.Count)
                        {
                            for (int i = 0; i < mountPositions.Count; i++)
                            {
                                InsertBlock(tr, modelSpace, componentBlocks["Mount"], mountPositions[i], mountOrientations[i], ComponentType.Mounting, db, ed);
                            }
                            ed.WriteMessage($"\nPlaced {mountPositions.Count} mounts.");
                        }
                        else
                        {
                            ed.WriteMessage("\nWarning: Mount position or orientation data is invalid or mismatched.");
                        }
                    }
                }
                ed.WriteMessage("\nRailing geometry generation process completed (inside RailingGeometryGenerator).");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acadEx)
            {
                ed.WriteMessage($"\nAutoCAD error during railing generation: {acadEx.ErrorStatus} - {acadEx.Message}\n{acadEx.StackTrace}");
                throw;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nUnexpected error during railing generation: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void GenerateRailSegments(Transaction tr, BlockTableRecord modelSpace, Polyline pathPolyline, Dictionary<string, ObjectId> componentBlocks, Database db, Editor ed)
        {
            if (pathPolyline == null)
            {
                throw new ArgumentNullException(nameof(pathPolyline), "Path polyline cannot be null for rail segments.");
            }

            int numVertices = pathPolyline.NumberOfVertices;
            if (numVertices < 2)
            {
                ed.WriteMessage("\nPolyline for rail segments must have at least 2 vertices.");
                return;
            }

            for (int i = 0; i < numVertices - 1; i++)
            {
                Point3d startPoint = pathPolyline.GetPoint3dAt(i);
                Point3d endPoint = pathPolyline.GetPoint3dAt(i + 1);

                Polyline railSegment = new Polyline();
                railSegment.AddVertexAt(0, new Point2d(startPoint.X, startPoint.Y), 0, 0, 0);
                railSegment.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0);

                railSegment.Elevation = startPoint.Z;
                railSegment.Normal = pathPolyline.Normal;

                string layerName = RailDesigner1.Utils.SappoUtilities.GetOrCreateLayerForComponent(db, ed, ComponentType.IntermediateRail);
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                ObjectId railLayerId = ObjectId.Null;
                if (lt.Has(layerName))
                {
                    railLayerId = lt[layerName];
                }
                else
                {
                    ed.WriteMessage($"\nWarning: Layer '{layerName}' could not be found or created for rail segment.");
                    railLayerId = db.Clayer;
                }
                railSegment.LayerId = railLayerId;

                if (componentBlocks != null && componentBlocks.ContainsKey("Rail"))
                {
                    Dictionary<string, object> railAttributes = new Dictionary<string, object>
                    {
                        { "RailType", "StandardSegment" },
                        { "Length", CalculateSegmentLength(startPoint, endPoint) }
                    };
                    AttachXData(tr, railSegment, railAttributes, db);
                }

                modelSpace.AppendEntity(railSegment);
                tr.AddNewlyCreatedDBObject(railSegment, true);
            }
        }

        private double CalculateSegmentLength(Point3d start, Point3d end)
        {
            return start.DistanceTo(end);
        }

        private void AttachXData(Transaction tr, Entity entity, Dictionary<string, object> attributes, Database dbForRegApp)
        {
            const string AppName = "RAIL_DESIGN_APP";
            RegAppTable rat = (RegAppTable)tr.GetObject(dbForRegApp.RegAppTableId, OpenMode.ForRead);
            if (!rat.Has(AppName))
            {
                rat.UpgradeOpen();
                RegAppTableRecord ratr = new RegAppTableRecord { Name = AppName };
                rat.Add(ratr);
                tr.AddNewlyCreatedDBObject(ratr, true);
                rat.DowngradeOpen();
            }

            ResultBuffer rb = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName));
            foreach (var attr in attributes)
            {
                if (attr.Value is string strVal)
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{strVal}"));
                else if (attr.Value is double dblVal)
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, dblVal));
                else if (attr.Value is int intVal)
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, intVal));
                else
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key}:{attr.Value?.ToString() ?? "null"}"));
            }
            entity.XData = rb;
        }

        private void InsertBlock(Transaction tr, BlockTableRecord modelSpace, ObjectId blockId, Point3d position, Vector3d orientation, ComponentType componentType, Database db, Editor ed)
        {
            string layerName = RailDesigner1.Utils.SappoUtilities.GetOrCreateLayerForComponent(db, ed, componentType);
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            ObjectId layerId = ObjectId.Null;
            if (lt.Has(layerName))
            {
                layerId = lt[layerName];
            }
            else
            {
                ed.WriteMessage($"\nWarning: Layer '{layerName}' could not be found or created for component type '{componentType}'.");
                layerId = db.Clayer;
            }

            BlockReference br = new BlockReference(position, blockId)
            {
                LayerId = layerId
            };

            double angle = 0.0;
            if (orientation.Length > Tolerance.Global.EqualPoint && !orientation.IsParallelTo(Vector3d.ZAxis, Tolerance.Global))
            {
                Vector2d orientationXY = new Vector2d(orientation.X, orientation.Y);
                if (orientationXY.Length > Tolerance.Global.EqualPoint)
                {
                    angle = orientationXY.Angle;
                }
            }
            br.Rotation = angle;

            modelSpace.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
        }
    }
}
