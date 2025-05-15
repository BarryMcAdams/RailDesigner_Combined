# PicketGenerator.cs
```csharp
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;

namespace RailCreator
{
    public static class PicketGenerator
    {
        public static void PlacePickets(Polyline polyline, RailingDesign design, List<Point3d> postPositions, BlockTableRecord btr, Transaction tr)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Create or get the "Pickets" layer with light grey color (RGB 204,204,204)
            string layerName = "Pickets";
            ObjectId layerId;
            using (var layerTable = (LayerTable)tr.GetObject(btr.Database.LayerTableId, OpenMode.ForWrite))
            {
                if (layerTable.Has(layerName))
                {
                    layerId = layerTable[layerName];
                }
                else
                {
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromRgb(204, 204, 204)
                    };
                    layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
            }

            if (design.PicketType == "Vertical" &&
                !design.PicketSize.ToLower().Contains("glass") &&
                !design.PicketSize.ToLower().Contains("mesh") &&
                !design.PicketSize.ToLower().Contains("perf") &&
                !design.PicketType.ToLower().Contains("deco"))
            {
                // Parse postWidth from design.PostSize
                double postWidth = 0.5;
                try
                {
                    string[] postSizeParts = design.PostSize.Split('x');
                    if (postSizeParts.Length >= 1)
                    {
                        postWidth = double.Parse(postSizeParts[0].Trim());
                        ed.WriteMessage($"\nPostSize: {design.PostSize}, Post width: {postWidth}");
                    }
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nError parsing PostSize '{design.PostSize}': {ex.Message}");
                }

                // Parse picketWidth from design.PicketSize
                double picketWidth = 0.0;
                try
                {
                    if (design.PicketSize.Contains("x"))
                    {
                        string[] sizeParts = design.PicketSize.Split('x');
                        picketWidth = double.Parse(sizeParts[0].Trim());
                    }
                    else
                    {
                        string[] sizeParts = design.PicketSize.Split(' ');
                        picketWidth = double.Parse(sizeParts[0].Trim());
                    }
                    ed.WriteMessage($"\nPicketSize: {design.PicketSize}, Picket width: {picketWidth}");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nError parsing PicketSize '{design.PicketSize}': {ex.Message}");
                    return;
                }

                // Process each segment between posts
                for (int i = 0; i < postPositions.Count - 1; i++)
                {
                    var startPost = postPositions[i];
                    var endPost = postPositions[i + 1];
                    double segmentLength = startPost.DistanceTo(endPost);
                    double insideDistance = segmentLength - postWidth;
                    if (insideDistance <= 0) continue;

                    // Calculate number of pickets for equal spacing, maximizing S < 4"
                    double maxClearSpacing = 4.0;
                    int numPickets = 1;
                    double clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1);
                    while (clearSpacing >= maxClearSpacing && numPickets < (int)(insideDistance / picketWidth))
                    {
                        numPickets++;
                        clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1);
                    }
                    if (clearSpacing >= maxClearSpacing)
                    {
                        numPickets++;
                        clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1);
                    }
                    double onCenterSpacing = clearSpacing + picketWidth;

                    ed.WriteMessage($"\nSegment {i}: Length={segmentLength:F2}\", Inside Distance={insideDistance:F2}\", Pickets={numPickets}, Clear Space={clearSpacing:F4}\", On-Center Spacing={onCenterSpacing:F4}\"");

                    // Place pickets with equal clear spacing
                    for (int j = 0; j < numPickets; j++)
                    {
                        double distance = clearSpacing + j * onCenterSpacing + picketWidth / 2;
                        double paramStart = polyline.GetParameterAtPoint(startPost);
                        double paramEnd = polyline.GetParameterAtPoint(endPost);
                        double totalParam = paramEnd - paramStart;
                        double fraction = (distance + postWidth / 2) / segmentLength;
                        double param = paramStart + fraction * totalParam;
                        var position = polyline.GetPointAtParameter(param);

                        // Calculate tangent for rotation
                        var tangent = polyline.GetFirstDerivative(param);
                        double angle = Math.Atan2(tangent.Y, tangent.X);

                        // Draw picket based on type
                        if (design.PicketSize.ToLower().Contains("round"))
                        {
                            var picket = new Circle(new Point3d(position.X, position.Y, 0), Vector3d.ZAxis, picketWidth / 2);
                            picket.LayerId = layerId;
                            btr.AppendEntity(picket);
                            tr.AddNewlyCreatedDBObject(picket, true);
                        }
                        else if (design.PicketSize.ToLower().Contains("sq") || design.PicketSize.Contains("x"))
                        {
                            double halfSide = picketWidth / 2;
                            var picket = new Polyline();
                            picket.AddVertexAt(0, new Point2d(-halfSide, -halfSide), 0, 0, 0);
                            picket.AddVertexAt(1, new Point2d(halfSide, -halfSide), 0, 0, 0);
                            picket.AddVertexAt(2, new Point2d(halfSide, halfSide), 0, 0, 0);
                            picket.AddVertexAt(3, new Point2d(-halfSide, halfSide), 0, 0, 0);
                            picket.Closed = true;

                            picket.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0)));
                            picket.TransformBy(Matrix3d.Displacement(position.GetAsVector()));

                            picket.LayerId = layerId;
                            btr.AppendEntity(picket);
                            tr.AddNewlyCreatedDBObject(picket, true);
                        }
                        else
                        {
                            ed.WriteMessage($"\nUnknown PicketSize format: {design.PicketSize}");
                        }
                    }
                }
            }
            else if (design.PicketType == "Horizontal")
            {
                int numHorizontalPickets = 5;
                double heightInterval = (design.RailHeight - design.TopCapHeight) / (numHorizontalPickets + 1);

                for (int i = 0; i < postPositions.Count - 1; i++)
                {
                    var startPost = postPositions[i];
                    var endPost = postPositions[i + 1];

                    for (int j = 1; j <= numHorizontalPickets; j++)
                    {
                        double simulatedHeight = heightInterval * j;
                        var picket = new Line(
                            new Point3d(startPost.X, startPost.Y + simulatedHeight, 0),
                            new Point3d(endPost.X, endPost.Y + simulatedHeight, 0)
                        );
                        picket.LayerId = layerId;
                        btr.AppendEntity(picket);
                        tr.AddNewlyCreatedDBObject(picket, true);
                    }
                }
            }
        }

        public static void PlaceSpecialPickets(Polyline polyline, RailingDesign design, List<Point3d> postPositions, BlockTableRecord btr, Transaction tr)
        {
            // Create or get the "Pickets" layer with light grey color (RGB 204,204,204)
            string layerName = "Pickets";
            ObjectId layerId;
            using (var layerTable = (LayerTable)tr.GetObject(btr.Database.LayerTableId, OpenMode.ForWrite))
            {
                if (layerTable.Has(layerName))
                {
                    layerId = layerTable[layerName];
                }
                else
                {
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromRgb(204, 204, 204)
                    };
                    layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
            }

            for (int i = 0; i < postPositions.Count - 1; i++)
            {
                var startPost = postPositions[i];
                var endPost = postPositions[i + 1];

                var panel = new Polyline();
                panel.AddVertexAt(0, new Point2d(startPost.X, startPost.Y), 0, 0, 0);
                panel.AddVertexAt(1, new Point2d(endPost.X, endPost.Y), 0, 0, 0);
                panel.AddVertexAt(2, new Point2d(endPost.X, endPost.Y), 0, 0, 0);
                panel.AddVertexAt(3, new Point2d(startPost.X, startPost.Y), 0, 0, 0);
                panel.Closed = true;
                panel.Elevation = 3.0;
                panel.TransformBy(Matrix3d.Scaling(design.RailHeight - design.TopCapHeight - 3.0, startPost));
                panel.LayerId = layerId;

                btr.AppendEntity(panel);
                tr.AddNewlyCreatedDBObject(panel, true);
            }
        }

        public static void PlaceDecorativePickets(Polyline polyline, RailingDesign design, List<Point3d> postPositions, BlockTableRecord btr, Transaction tr)
        {
            // Create or get the "Pickets" layer with light grey color (RGB 204,204,204)
            string layerName = "Pickets";
            ObjectId layerId;
            using (var layerTable = (LayerTable)tr.GetObject(btr.Database.LayerTableId, OpenMode.ForWrite))
            {
                if (layerTable.Has(layerName))
                {
                    layerId = layerTable[layerName];
                }
                else
                {
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromRgb(204, 204, 204)
                    };
                    layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
            }

            for (int i = 0; i < postPositions.Count - 1; i++)
            {
                var startPost = postPositions[i];
                var endPost = postPositions[i + 1];

                var midPoint = new Point3d((startPost.X + endPost.X) / 2, (startPost.Y + endPost.Y) / 2, (startPost.Z + endPost.Z) / 2);

                double width = design.DecorativeWidth ?? 2.0;
                var decoPicket = new Polyline();
                decoPicket.AddVertexAt(0, new Point2d(midPoint.X - width / 2, midPoint.Y - 0.5), 0, 0, 0);
                decoPicket.AddVertexAt(1, new Point2d(midPoint.X + width / 2, midPoint.Y - 0.5), 0, 0, 0);
                decoPicket.AddVertexAt(2, new Point2d(midPoint.X + width / 2, midPoint.Y + 0.5), 0, 0, 0);
                decoPicket.AddVertexAt(3, new Point2d(midPoint.X - width / 2, midPoint.Y + 0.5), 0, 0, 0);
                decoPicket.Closed = true;
                decoPicket.Elevation = (design.RailHeight - design.TopCapHeight) / 2;
                decoPicket.LayerId = layerId;

                btr.AppendEntity(decoPicket);
                tr.AddNewlyCreatedDBObject(decoPicket, true);

                double segmentLength = startPost.DistanceTo(endPost);
                int numPicketsPerSide = (int)Math.Ceiling((segmentLength - width) / (2 * 4.0));
                double spacing = (segmentLength - width) / (2 * (numPicketsPerSide + 1));

                for (int j = 1; j <= numPicketsPerSide; j++)
                {
                    double distanceBefore = spacing * j;
                    var positionBefore = polyline.GetPointAtDist(polyline.GetDistAtPoint(startPost) + distanceBefore);
                    if (design.PicketSize.ToLower().Contains("round"))
                    {
                        double diameter = double.Parse(design.PicketSize.Split(' ')[0]);
                        var picket = new Circle(new Point3d(positionBefore.X, positionBefore.Y, 0), Vector3d.ZAxis, diameter / 2);
                        picket.LayerId = layerId;
                        btr.AppendEntity(picket);
                        tr.AddNewlyCreatedDBObject(picket, true);
                    }
                    else if (design.PicketSize.ToLower().Contains("sq") || design.PicketSize.Contains("x"))
                    {
                        double side = double.Parse(design.PicketSize.Split(' ')[0]);
                        var picket = new Polyline();
                        picket.AddVertexAt(0, new Point2d(positionBefore.X - side / 2, positionBefore.Y - side / 2), 0, 0, 0);
                        picket.AddVertexAt(1, new Point2d(positionBefore.X + side / 2, positionBefore.Y - side / 2), 0, 0, 0);
                        picket.AddVertexAt(2, new Point2d(positionBefore.X + side / 2, positionBefore.Y + side / 2), 0, 0, 0);
                        picket.AddVertexAt(3, new Point2d(positionBefore.X - side / 2, positionBefore.Y + side / 2), 0, 0, 0);
                        picket.Closed = true;
                        picket.LayerId = layerId;
                        btr.AppendEntity(picket);
                        tr.AddNewlyCreatedDBObject(picket, true);
                    }
                }

                for (int j = 1; j <= numPicketsPerSide; j++)
                {
                    double distanceAfter = polyline.GetDistAtPoint(startPost) + (segmentLength - spacing * j);
                    var positionAfter = polyline.GetPointAtDist(distanceAfter);
                    if (design.PicketSize.ToLower().Contains("round"))
                    {
                        double diameter = double.Parse(design.PicketSize.Split(' ')[0]);
                        var picket = new Circle(new Point3d(positionAfter.X, positionAfter.Y, 0), Vector3d.ZAxis, diameter / 2);
                        picket.LayerId = layerId;
                        btr.AppendEntity(picket);
                        tr.AddNewlyCreatedDBObject(picket, true);
                    }
                    else if (design.PicketSize.ToLower().Contains("sq") || design.PicketSize.Contains("x"))
                    {
                        double side = double.Parse(design.PicketSize.Split(' ')[0]);
                        var picket = new Polyline();
                        picket.AddVertexAt(0, new Point2d(positionAfter.X - side / 2, positionAfter.Y - side / 2), 0, 0, 0);
                        picket.AddVertexAt(1, new Point2d(positionAfter.X + side / 2, positionAfter.Y - side / 2), 0, 0, 0);
                        picket.AddVertexAt(2, new Point2d(positionAfter.X + side / 2, positionAfter.Y + side / 2), 0, 0, 0);
                        picket.AddVertexAt(3, new Point2d(positionAfter.X - side / 2, positionAfter.Y + side / 2), 0, 0, 0);
                        picket.Closed = true;
                        picket.LayerId = layerId;
                        btr.AppendEntity(picket);
                        tr.AddNewlyCreatedDBObject(picket, true);
                    }
                }
            }
        }
    }
}
```

# PicketPlacementLogic.cs
```csharp
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
```

# PicketPlacementLogicWrapper.cs
```csharp
// This file is deprecated and no longer in use. Remove it if possible.
namespace RailDesigner1 { }
```

# PostPlacementLogic.cs
```csharp
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Colors;

namespace RailDesigner1
{
    public static class PostPlacementLogic
    {
        public static List<Point3d> CalculatePostPositions(Polyline polyline, RailingDesign design, BlockTableRecord btr, Transaction tr)
        {
            var positions = new List<Point3d>();
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Target spacing between posts
            const double targetSpacing = 50.0;
            const double geometryEpsilon = 1e-6; // Tolerance for floating point comparisons in geometry/distances
            const double ceilingEpsilon = 1e-9; // Tolerance specifically for ceiling calculation near integers

            ed.WriteMessage("\n--- Post Generation Start ---");
            ed.WriteMessage($"\nPolyline has {polyline.NumberOfVertices} vertices.");
            ed.WriteMessage($"\nTotal Polyline Length: {polyline.Length:F4}");

            // Step 1: Add the first vertex position
            if (polyline.NumberOfVertices > 0)
            {
                positions.Add(polyline.GetPoint3dAt(0));
                ed.WriteMessage($"\nAdded start vertex at ({polyline.GetPoint3dAt(0).X:F4}, {polyline.GetPoint3dAt(0).Y:F4}, {polyline.GetPoint3dAt(0).Z:F4}) (Dist 0)");
            }

            // Step 2: Add intermediate posts and subsequent vertices
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var startPoint = polyline.GetPoint3dAt(i);
                var endPoint = polyline.GetPoint3dAt(i + 1);
                double startDist = polyline.GetDistAtPoint(startPoint);
                double endDist = polyline.GetDistAtPoint(endPoint);
                double segmentLength = endDist - startDist;

                ed.WriteMessage($"\n--- Processing Segment {i + 1} ---");
                ed.WriteMessage($"Segment Start Point: ({startPoint.X:F4}, {startPoint.Y:F4}, {startPoint.Z:F4}) at total dist {startDist:F4}");
                ed.WriteMessage($"Segment End Point: ({endPoint.X:F4}, {endPoint.Y:F4}, {endPoint.Z:F4}) at total dist {endDist:F4}");
                ed.WriteMessage($"Segment Length: {segmentLength:F4}");

                // Handle zero-length segments
                if (segmentLength < geometryEpsilon)
                {
                    ed.WriteMessage("Skipping zero-length segment.");
                    continue;
                }

                // Calculate the number of intervals for this segment
                double ratio = segmentLength / targetSpacing;
                int numIntervals = (int)Math.Ceiling(ratio - ceilingEpsilon);

                if (numIntervals == 0) numIntervals = 1;
                double actualSpacing = segmentLength / numIntervals;

                ed.WriteMessage($"Target Spacing: {targetSpacing:F4}");
                ed.WriteMessage($"Division Ratio (Length/Target): {ratio:F12}");
                ed.WriteMessage($"Calculated # Intervals (Ceil(Ratio - Epsilon)): {numIntervals}");
                ed.WriteMessage($"Calculated Actual Spacing for this segment: {actualSpacing:F4}");

                // Add intermediate posts within this segment
                for (int j = 1; j < numIntervals; j++)
                {
                    double distAlongPolyline = startDist + j * actualSpacing;
                    var point = polyline.GetPointAtDist(distAlongPolyline);
                    positions.Add(point);
                    ed.WriteMessage($"Added intermediate post {j} at ({point.X:F4}, {point.Y:F4}, {point.Z:F4}) at total dist {distAlongPolyline:F4}");
                }

                // Add the end point of this segment
                positions.Add(endPoint);
                ed.WriteMessage($"Added segment end point at ({endPoint.X:F4}, {endPoint.Y:F4}, {endPoint.Z:F4}) at total dist {endDist:F4}");
            }

            // Remove duplicates and sort
            var finalPositions = positions
                .OrderBy(p => polyline.GetDistAtPoint(polyline.GetClosestPointTo(p, false)))
                .GroupBy(p => $"{p.X:F6},{p.Y:F6},{p.Z:F6}")
                .Select(g => g.First())
                .ToList();

            ed.WriteMessage("\n--- Final Post Positions (after duplicate removal and sorting) ---");
            ed.WriteMessage($"Total final posts: {finalPositions.Count}");
            for (int k = 0; k < finalPositions.Count; k++)
            {
                var pos = finalPositions[k];
                double distAlong = polyline.GetDistAtPoint(polyline.GetClosestPointTo(pos, false));
                ed.WriteMessage($"Post {k + 1}: ({pos.X:F4}, {pos.Y:F4}, {pos.Z:F4}) at total dist {distAlong:F4}");
            }
            ed.WriteMessage("\n--- Drawing Posts ---");

            // Create or get the "Post" layer and set its color to red
            string layerName = "Post";
            ObjectId layerId;
            using (var layerTable = (LayerTable)tr.GetObject(btr.Database.LayerTableId, OpenMode.ForWrite))
            {
                if (layerTable.Has(layerName))
                {
                    layerId = layerTable[layerName];
                }
                else
                {
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromRgb(255, 0, 0) // Set layer color to red using RGB (255,0,0)
                    };
                    layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
            }

            // Draw posts on the "Post" layer
            foreach (var position in finalPositions)
            {
                // Calculate post length based on design
                double postLength = design.RailHeight;
                // Fixed CS1061: Changed 'MountingType' to 'MountType' to match the property defined in RailingDesign.cs
                switch (design.MountType)
                {
                    case "Core-Drilled":
                        postLength = design.RailHeight - design.TopCapHeight + 3.5;
                        break;
                    case "Plate":
                        postLength = design.RailHeight - design.TopCapHeight - 0.375;
                        break;
                    case "Side-Mounted":
                        postLength = design.RailHeight + 2.0;
                        break;
                }

                // Calculate post rotation based on polyline tangent
                double param = polyline.GetParameterAtPoint(polyline.GetClosestPointTo(position, false));
                param = Math.Max(polyline.StartParam, Math.Min(polyline.EndParam, param));

                Vector3d tangent = polyline.GetFirstDerivative(param);

                if (tangent.Length < geometryEpsilon)
                {
                    double pointDist = polyline.GetDistAtPoint(polyline.GetClosestPointTo(position, false));
                    int segmentIndex = -1;

                    for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                    {
                        double segStartDist = polyline.GetDistAtPoint(polyline.GetPoint3dAt(i));
                        double segEndDist = polyline.GetDistAtPoint(polyline.GetPoint3dAt(i + 1));
                        if (pointDist >= segStartDist - geometryEpsilon && pointDist <= segEndDist + geometryEpsilon)
                        {
                            segmentIndex = i;
                            break;
                        }
                    }

                    if (segmentIndex != -1)
                    {
                        var p1 = polyline.GetPoint3dAt(segmentIndex);
                        var p2 = polyline.GetPoint3dAt(segmentIndex + 1);
                        tangent = (p2 - p1).GetNormal();
                    }
                    else if (polyline.NumberOfVertices > 1)
                    {
                        if (position.IsEqualTo(polyline.GetPoint3dAt(0), new Tolerance(geometryEpsilon, geometryEpsilon)))
                        {
                            var p1 = polyline.GetPoint3dAt(0);
                            var p2 = polyline.GetPoint3dAt(1);
                            tangent = (p2 - p1).GetNormal();
                        }
                        else if (position.IsEqualTo(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1), new Tolerance(geometryEpsilon, geometryEpsilon)))
                        {
                            var p1 = polyline.GetPoint3dAt(polyline.NumberOfVertices - 2);
                            var p2 = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);
                            tangent = (p2 - p1).GetNormal();
                        }
                        else
                        {
                            tangent = Vector3d.XAxis;
                        }
                    }
                    else
                    {
                        tangent = Vector3d.XAxis; // Default for single point polyline
                    }
                }
                else
                {
                    tangent = tangent.GetNormal();
                }

                double angle = Math.Atan2(tangent.Y, tangent.X);

                // Get post size from design
                double postWidth = 2.0;
                double postLengthDim = 2.0;
                string[] postSizeParts = design.PostSize.Split('x');
                if (postSizeParts.Length >= 2)
                {
                    if (double.TryParse(postSizeParts[0].Trim(), out double w)) postWidth = w;
                    string secondPart = postSizeParts[1].Trim();
                    int spaceIndex = secondPart.IndexOf(' ');
                    if (spaceIndex > 0) secondPart = secondPart.Substring(0, spaceIndex);
                    if (double.TryParse(secondPart, out double ld)) postLengthDim = ld;
                }

                // Create post geometry (a square/rectangle polyline)
                var post = new Polyline();
                post.AddVertexAt(0, new Point2d(-postWidth / 2, -postLengthDim / 2), 0, 0, 0);
                post.AddVertexAt(1, new Point2d(postWidth / 2, -postLengthDim / 2), 0, 0, 0);
                post.AddVertexAt(2, new Point2d(postWidth / 2, postLengthDim / 2), 0, 0, 0);
                post.AddVertexAt(3, new Point2d(-postWidth / 2, postLengthDim / 2), 0, 0, 0);
                post.Closed = true;

                // Set the post to the "Post" layer
                post.LayerId = layerId;

                // Transform post: rotate, then move to position
                post.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0)));
                post.TransformBy(Matrix3d.Displacement(position.GetAsVector()));

                // Add post to the database
                btr.AppendEntity(post);
                tr.AddNewlyCreatedDBObject(post, true);
            }
            ed.WriteMessage("\n--- Post Generation End ---");

            return finalPositions;
        }
    }
}
```

# PostPlacementLogicWrapper.cs
```csharp
// PostPlacementLogicWrapper.cs
// This wrapper class provides an interface to the PostGenerator logic without modifying the original code.
// It calls methods from PostGenerator.cs to calculate post positions.
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using System; // Added for Exception
// using RailCreator; // Assuming RailingDesign is defined elsewhere or needs to be stubbed

namespace RailDesigner1
{
    public static class PostPlacementLogicWrapper
    {
        public static List<Point3d> CalculatePostPositions(Polyline pathPolyline, double postSpacing)
        {
            var positions = new List<Point3d>();
            if (pathPolyline == null || pathPolyline.NumberOfVertices == 0 || postSpacing <= 1e-6) return positions;

            positions.Add(pathPolyline.StartPoint);
            double currentDist = postSpacing;
            while (currentDist < pathPolyline.Length - 1e-6)
            {
                positions.Add(pathPolyline.GetPointAtDist(currentDist));
                currentDist += postSpacing;
            }
            if (positions.Count == 0 || pathPolyline.EndPoint.DistanceTo(positions.Last()) > 1e-4)
            {
                 positions.Add(pathPolyline.EndPoint);
            }
            return positions.Distinct().ToList();
        }

        public static List<Vector3d> CalculatePostOrientations(Polyline pathPolyline, List<Point3d> postPositions)
        {
            var orientations = new List<Vector3d>();
            if (pathPolyline == null || postPositions == null || postPositions.Count == 0) return orientations;
            foreach (var pos in postPositions)
            {
                try
                {
                    double param = pathPolyline.GetParameterAtPoint(pathPolyline.GetClosestPointTo(pos, Vector3d.ZAxis, false));
                    Vector3d tangent = pathPolyline.GetFirstDerivative(param);
                    if (tangent.DotProduct(tangent) < 1e-12) orientations.Add(Vector3d.XAxis);
                    else orientations.Add(tangent.GetNormal());
                }
                catch { orientations.Add(Vector3d.XAxis); }
            }
            return orientations;
        }
    }
}
```

# RailExportBomCommand.cs
```csharp
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
```

# RailGenerateCommand.cs
```csharp
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using RailDesigner1; // For RailingGeometryGenerator, RailingDesign, TransactionManagerWrapper, etc.
using RailDesigner1.Utils; // For SappoUtilities

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
                ed.WriteMessage("\nNo active document.");
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
```

# RailGenerateCommandHandler.cs
```csharp
// RailGenerateCommandHandler.cs
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using RailDesigner1.Utils; // For ErrorLogger

namespace RailDesigner1
{
    public class RailGenerateCommandHandler
    {
        public void Execute()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // TODO: Implement railing generation logic
            doc.Editor.WriteMessage("\nRailGenerateCommandHandler.Execute() called (stub implementation).\nPlease implement actual logic.\n");
            ErrorLogger.LogMessage("RailGenerateCommandHandler.Execute() called.");
        }
    }
}
```

# RailingDesign.cs
```csharp
﻿using System; // For Nullable if used more explicitly

namespace RailDesigner1 // Ensure this namespace matches your project's root namespace
{
    /// <summary>
    /// Represents the design parameters for a railing.
    /// This class acts as a data transfer object (DTO) or a model
    /// to hold all configurable aspects of the railing design.
    /// </summary>
    public class RailingDesign
    {
        // General Railing Properties
        public double RailHeight { get; set; }           // Overall height of the railing from the base path to the top.
        public double BottomClearance { get; set; }      // Clearance from the base path to the bottom of the lowest rail/picket.

        // Post Properties
        public string PostSize { get; set; }             // e.g., "2x2", "3 round" (Width/Depth or Diameter)
        public double PostSpacing { get; set; }          // Center-to-center spacing for posts.
        public string PostMaterial { get; set; }         // e.g., "Steel", "Aluminum", "Wood"
        public string PostCapType { get; set; }          // e.g., "Flat", "Pyramid", "Ball"

        // Picket Properties
        public string PicketType { get; set; }           // e.g., "Vertical", "Horizontal", "GlassPanel", "Mesh", "Decorative"
        public string PicketSize { get; set; }           // e.g., "0.75x0.75", "1.5x0.5", "0.75 round" (Width/Depth or Diameter for vertical; Thickness for panels)
        public double PicketSpacing { get; set; }        // For vertical pickets: desired clear spacing or center-to-center (clarify usage).
                                                         // For horizontal pickets: vertical spacing between them.
        public string PicketMaterial { get; set; }       // e.g., "Steel", "Aluminum"
        public int NumberOfHorizontalPickets { get; set; } // If PicketType is "Horizontal"

        // Top Rail / Handrail Properties
        public string TopRailProfile { get; set; }       // e.g., "2x1 Rect", "1.5 Pipe"
        public double TopRailHeight { get; set; }        // Specific height of the top surface of the top rail (can be same as RailHeight).
                                                         // Often RailHeight defines the guardrail height, and TopRailHeight might be slightly different for ergonomics.
        public double TopCapHeight { get; set; }         // Used in some calculations; might be part of top rail or a separate cap.
                                                         // This property seems to be used for available height calculations for pickets/panels.
                                                         // Clarify if this is distinct from TopRailProfile's height.
        public string TopRailMaterial { get; set; }

        // Bottom Rail Properties (Optional)
        public bool HasBottomRail { get; set; }
        public string BottomRailProfile { get; set; }    // e.g., "1.5x1.5"
        public string BottomRailMaterial { get; set; }

        // Mount Properties
        public string MountType { get; set; }            // e.g., "Surface", "Fascia"
        public double MountSpacing { get; set; }         // Spacing for mounts if independent of posts.
        public string MountSize { get; set; }            // Dimensions of the mount.

        // Decorative Elements (if any)
        public double? DecorativeWidth { get; set; }     // Width of a central decorative panel/element (nullable if optional).
        public string DecorativeElementType { get; set; }

        // Material and Finish
        public string DefaultMaterial { get; set; }      // Default material if not specified per component.
        public string FinishColor { get; set; }          // e.g., "RAL9005 Black", "PowderCoat Bronze"

        /// <summary>
        /// Initializes a new instance of the <see cref="RailingDesign"/> class
        /// with default values.
        /// </summary>
        public RailingDesign()
        {
            // Initialize with sensible defaults
            RailHeight = 36.0;         // inches
            BottomClearance = 2.0;     // inches

            PostSize = "2x2";
            PostSpacing = 72.0;        // inches
            PostMaterial = "Aluminum";
            PostCapType = "Flat";

            PicketType = "Vertical";
            PicketSize = "0.75x0.75";  // inches
            PicketSpacing = 4.0;       // Clear spacing for vertical pickets, or vertical spacing for horizontal
            PicketMaterial = "Aluminum";
            NumberOfHorizontalPickets = 5;

            TopRailProfile = "2x1 Rect";
            TopRailHeight = 36.0;      // inches
            TopCapHeight = 1.5;        // This might be the thickness/height of the top rail/cap itself
            TopRailMaterial = "Aluminum";

            HasBottomRail = true;
            BottomRailProfile = "1.5x1.5";
            BottomRailMaterial = "Aluminum";

            MountType = "Surface";
            MountSpacing = PostSpacing; // Default mounts to align with posts or use a dedicated spacing
            MountSize = "4x4x0.25";     // inches

            DecorativeWidth = null;    // No decorative panel by default
            DecorativeElementType = "None";

            DefaultMaterial = "Aluminum";
            FinishColor = "Black Matte";
        }

        // You can add methods here for validation or derived properties if needed.
        // For example:
        // public double GetAvailablePicketHeight()
        // {
        //     double height = RailHeight - TopCapHeight; // Adjust based on TopRailProfile, etc.
        //     if (HasBottomRail)
        //     {
        //         // Subtract bottom rail height and bottom clearance
        //         // height -= (ProfileHeight(BottomRailProfile) + BottomClearance);
        //     } else {
        //         height -= BottomClearance;
        //     }
        //     return height;
        // }
    }
}
```

# RailingGeometryGenerator.cs
```csharp
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
