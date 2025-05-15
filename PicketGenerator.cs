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