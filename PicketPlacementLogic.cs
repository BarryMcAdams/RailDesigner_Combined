using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application; // For AcApp
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput; // <<< ADDED THIS FOR Editor
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors; // For Color class
using System;
using System.Collections.Generic;
using System.Linq;

namespace RailDesigner1
{
    public static class PicketPlacementLogic
    {
        public static List<Point3d> CalculatePicketPositions(Polyline polyline, RailingDesign design, BlockTableRecord modelSpaceBtr, Transaction tr)
        {
            var positions = new List<Point3d>();
            // Use AcApp alias and ensure EditorInput namespace is used
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;

            const double targetPicketSpacing = 6.0;
            const double geometryEpsilon = 1e-6;

            ed.WriteMessage("\n--- Picket Generation Start (PicketPlacementLogic) ---");
            if (polyline == null) // Check polyline for null early
            {
                ed.WriteMessage("\nError: Polyline is null. Aborting picket placement.");
                return positions;
            }
            ed.WriteMessage($"\nPolyline has {polyline.NumberOfVertices} vertices.");
            ed.WriteMessage($"\nTotal Polyline Length: {polyline.Length:F4}");

            if (polyline.NumberOfVertices == 0)
            {
                ed.WriteMessage("\nError: Polyline has no vertices. Aborting picket placement.");
                return positions;
            }
            if (polyline.Length < geometryEpsilon)
            {
                ed.WriteMessage("\nWarning: Polyline length is effectively zero. May only place pickets at vertices if any exist.");
                // If polyline length is zero, but it has vertices (e.g. a single point polyline from bad data)
                // the loop for segments won't run. The end point logic might add something.
                if (polyline.NumberOfVertices > 0)
                {
                    positions.Add(polyline.StartPoint); // Add the only point
                    ed.WriteMessage($"\nAdded start vertex for zero-length polyline at ({polyline.StartPoint.X:F2},{polyline.StartPoint.Y:F2})");
                    // The duplicate removal will handle if it gets added again later.
                }
            }

            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                Point3d segmentStartPoint = polyline.GetPoint3dAt(i);
                Point3d segmentEndPoint = polyline.GetPoint3dAt(i + 1);

                positions.Add(segmentStartPoint);
                // ed.WriteMessage($"\nSegment {i + 1} processing: Added start vertex ({segmentStartPoint.X:F2},{segmentStartPoint.Y:F2})");

                double segmentStartDist = polyline.GetDistAtPoint(segmentStartPoint);
                double segmentEndDist = polyline.GetDistAtPoint(segmentEndPoint);
                double segmentLength = segmentEndDist - segmentStartDist;

                // ed.WriteMessage($"\n--- Processing Segment {i + 1} of {polyline.NumberOfVertices - 1} ---");
                // ed.WriteMessage($"Segment Start: ({segmentStartPoint.X:F4}, {segmentStartPoint.Y:F4}, {segmentStartPoint.Z:F4}) at total dist {segmentStartDist:F4}");
                // ed.WriteMessage($"Segment End: ({segmentEndPoint.X:F4}, {segmentEndPoint.Y:F4}, {segmentEndPoint.Z:F4}) at total dist {segmentEndDist:F4}");
                // ed.WriteMessage($"Segment Length: {segmentLength:F4}");

                if (segmentLength < geometryEpsilon)
                {
                    // ed.WriteMessage("\nSkipping zero-length segment.");
                    continue;
                }

                double currentDistOnSegment = targetPicketSpacing;
                while (currentDistOnSegment < segmentLength - geometryEpsilon)
                {
                    double distAlongPolyline = segmentStartDist + currentDistOnSegment;
                    Point3d picketPt = polyline.GetPointAtDist(distAlongPolyline);
                    positions.Add(picketPt);
                    // ed.WriteMessage($"Added intermediate picket at seg. dist {currentDistOnSegment:F4}, abs. dist {distAlongPolyline:F4} position ({picketPt.X:F4}, {picketPt.Y:F4})");
                    currentDistOnSegment += targetPicketSpacing;
                }
            }

            if (polyline.NumberOfVertices > 0)
            {
                Point3d lastPolyVertex = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);
                positions.Add(lastPolyVertex);
                // ed.WriteMessage($"\nAdded polyline end vertex ({lastPolyVertex.X:F2},{lastPolyVertex.Y:F2})");
            }

            var finalPositions = positions
                .Select(p => new { OriginalPoint = p, Dist = polyline.GetDistAtPoint(polyline.GetClosestPointTo(p, Vector3d.ZAxis, false)) })
                .OrderBy(pd => pd.Dist)
                .GroupBy(pd => $"{pd.OriginalPoint.X:F6},{pd.OriginalPoint.Y:F6},{pd.OriginalPoint.Z:F6}")
                .Select(g => g.First().OriginalPoint)
                .ToList();

            ed.WriteMessage($"\n--- Picket Positions Calculated (PicketPlacementLogic): {finalPositions.Count} ---");
            // for (int k = 0; k < finalPositions.Count; k++)
            // {
            //     var pos = finalPositions[k];
            //     double distAlong = polyline.GetDistAtPoint(polyline.GetClosestPointTo(pos, Vector3d.ZAxis, false));
            //     ed.WriteMessage($"Picket {k + 1}: ({pos.X:F4}, {pos.Y:F4}, {pos.Z:F4}) at total dist {distAlong:F4}");
            // }

            if (!finalPositions.Any())
            {
                ed.WriteMessage("\nNo final picket positions. Skipping drawing pickets.");
                return finalPositions;
            }

            // ed.WriteMessage("\n--- Drawing Pickets (PicketPlacementLogic) ---");

            string layerName = "Picket"; // Ensure this name is desired. Maybe "L-RAIL-PICKET-GEOM" for more detail
            ObjectId layerId = ObjectId.Null;

            // Ensure modelSpaceBtr is not null and database is accessible
            if (modelSpaceBtr == null)
            {
                ed.WriteMessage("\nError: ModelSpace BlockTableRecord is null in CalculatePicketPositions. Cannot create layer or pickets.");
                return finalPositions; // Or throw an exception
            }
            Database db = modelSpaceBtr.Database; // Get Database from modelSpaceBtr

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
                    LayerTableRecord layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromRgb(0, 0, 255)
                    };
                    layerId = lt.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                finally
                {
                    if (lt.IsWriteEnabled) lt.DowngradeOpen();
                }
            }

            foreach (var position in finalPositions)
            {
                double picketWidth = 1.5;
                double picketHeight = design.RailHeight;

                if (!string.IsNullOrEmpty(design.PicketSize))
                {
                    string[] picketSizeParts = design.PicketSize.Replace(" ", "").Split(new char[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
                    if (picketSizeParts.Length >= 1)
                    {
                        double.TryParse(picketSizeParts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out picketWidth);
                        if (picketSizeParts.Length >= 2)
                        {
                            double.TryParse(picketSizeParts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out picketHeight);
                        }
                        else
                        {
                            picketHeight = design.RailHeight;
                        }
                    }
                    if (picketWidth < geometryEpsilon) picketWidth = 1.5;
                    if (picketHeight < geometryEpsilon) picketHeight = design.RailHeight;
                }

                double param = polyline.GetParameterAtPoint(polyline.GetClosestPointTo(position, Vector3d.ZAxis, false));
                Vector3d tangent;
                try
                {
                    tangent = polyline.GetFirstDerivative(param);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acadEx)
                {
                    ed.WriteMessage($"\nWarning: Could not get tangent for picket at {position}. {acadEx.Message}. Defaulting.");
                    int closestVertexIndex = (int)Math.Round(param);
                    if (closestVertexIndex < polyline.NumberOfVertices - 1 && closestVertexIndex >= 0)
                    {
                        tangent = polyline.GetPoint3dAt(closestVertexIndex + 1) - polyline.GetPoint3dAt(closestVertexIndex);
                    }
                    else if (closestVertexIndex > 0 && closestVertexIndex < polyline.NumberOfVertices)
                    {
                        tangent = polyline.GetPoint3dAt(closestVertexIndex) - polyline.GetPoint3dAt(closestVertexIndex - 1);
                    }
                    else
                    {
                        tangent = Vector3d.XAxis;
                    }
                }

                // Use tangent.Length * tangent.Length for squared length
                if (tangent.DotProduct(tangent) < geometryEpsilon * geometryEpsilon)
                {
                    int paramAsInt = (int)Math.Floor(param + geometryEpsilon);
                    if (paramAsInt < polyline.NumberOfVertices - 1 && paramAsInt >= 0)
                    {
                        tangent = polyline.GetPoint3dAt(paramAsInt + 1) - polyline.GetPoint3dAt(paramAsInt);
                    }
                    else if (paramAsInt > 0 && paramAsInt < polyline.NumberOfVertices)
                    {
                        tangent = polyline.GetPoint3dAt(paramAsInt) - polyline.GetPoint3dAt(paramAsInt - 1);
                    }
                    else
                    {
                        tangent = Vector3d.XAxis;
                        // ed.WriteMessage($"\nWarning: Zero-length tangent (after fallback) for picket at {position}. Defaulting.");
                    }

                    if (tangent.DotProduct(tangent) < geometryEpsilon * geometryEpsilon) tangent = Vector3d.XAxis;
                }

                if (tangent.Length < geometryEpsilon) tangent = Vector3d.XAxis; // Final check before GetNormal
                tangent = tangent.GetNormal();
                double angle = Math.Atan2(tangent.Y, tangent.X);

                var picketGeo = new Polyline(4);
                picketGeo.AddVertexAt(0, new Point2d(-picketWidth / 2.0, 0), 0, 0, 0);
                picketGeo.AddVertexAt(1, new Point2d(picketWidth / 2.0, 0), 0, 0, 0);
                picketGeo.AddVertexAt(2, new Point2d(picketWidth / 2.0, picketHeight), 0, 0, 0);
                picketGeo.AddVertexAt(3, new Point2d(-picketWidth / 2.0, picketHeight), 0, 0, 0);
                picketGeo.Closed = true;
                picketGeo.Normal = Vector3d.ZAxis;
                picketGeo.Elevation = 0;

                picketGeo.LayerId = layerId;

                // Use Z from the calculated 'position' which should be on the polyline
                Matrix3d transform = Matrix3d.Displacement(new Vector3d(position.X, position.Y, position.Z)) *
                                     Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);

                picketGeo.TransformBy(transform);

                modelSpaceBtr.AppendEntity(picketGeo);
                tr.AddNewlyCreatedDBObject(picketGeo, true);
            }
            // ed.WriteMessage("\n--- Picket Drawing End (PicketPlacementLogic) ---");

            return finalPositions;
        }

        public static List<Vector3d> CalculatePicketOrientations(Polyline pathPolyline, List<Point3d> picketPositions)
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor; // Use AcApp alias
            // ed.WriteMessage("\nREAL: PicketPlacementLogic.CalculatePicketOrientations called.");
            var orientations = new List<Vector3d>();
            if (pathPolyline == null || picketPositions == null || picketPositions.Count == 0) return orientations;

            foreach (var pos in picketPositions)
            {
                try
                {
                    double param = pathPolyline.GetParameterAtPoint(pathPolyline.GetClosestPointTo(pos, Vector3d.ZAxis, false));
                    Vector3d tangent = pathPolyline.GetFirstDerivative(param);
                    // Use tangent.Length * tangent.Length (or DotProduct) for squared length comparison
                    if (tangent.DotProduct(tangent) < 1e-12)
                    {
                        orientations.Add(Vector3d.XAxis);
                        // ed.WriteMessage($"\nWarning: Zero tangent at picket position {pos} in CalculatePicketOrientations. Defaulting.");
                    }
                    else
                    {
                        orientations.Add(tangent.GetNormal());
                    }
                }
                catch (System.Exception)
                {
                    // ed.WriteMessage($"\nError in CalculatePicketOrientations for picket at {pos}: {ex.Message}. Defaulting.");
                    orientations.Add(Vector3d.XAxis);
                }
            }
            return orientations;
        }
    }
}