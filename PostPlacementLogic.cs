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