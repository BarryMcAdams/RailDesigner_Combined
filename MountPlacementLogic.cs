// MountPlacementLogic.cs
using Autodesk.AutoCAD.ApplicationServices; // For AcApp in messages if added
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application; // For AcApp
using Autodesk.AutoCAD.DatabaseServices; // For Polyline
using Autodesk.AutoCAD.Geometry;
using System; // For ArgumentException
using System.Collections.Generic;
using System.Linq; // For Distinct()

namespace RailDesigner1
{
    public static class MountPlacementLogic
    {
        public static List<Point3d> CalculateMountPositions(Polyline polyline, double spacing)
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nREAL: MountPlacementLogic.CalculateMountPositions with spacing {spacing}.");
            List<Point3d> positions = new List<Point3d>();

            if (polyline == null || polyline.NumberOfVertices < 1) // Check for vertices
            {
                // Optionally, log or throw, but returning empty list is often safer for chain calls
                // AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\nMountPlacementLogic: Polyline is null or empty.");
                return positions;
            }
            if (spacing <= 1e-6) // Check for valid spacing
            {
                // AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nMountPlacementLogic: Invalid mount spacing: {spacing}.");
                if (polyline.NumberOfVertices > 0) positions.Add(polyline.StartPoint); // Add at least start point if spacing is bad
                return positions;
            }

            double length = polyline.Length; // Use polyline.Length
            if (length < 1e-6 && polyline.NumberOfVertices > 0) // Polyline with zero length (e.g. single point)
            {
                positions.Add(polyline.StartPoint);
                return positions;
            }
            
            // Add start point
            positions.Add(polyline.StartPoint);

            double currentDist = spacing;
            while (currentDist < length - 1e-6) // Iterate along the polyline by spacing
            {
                positions.Add(polyline.GetPointAtDist(currentDist));
                currentDist += spacing;
            }

            // Ensure end point is added if not already very close
            if (length > 0 && (positions.Count == 0 || polyline.EndPoint.DistanceTo(positions.Last()) > 1e-4))
            {
                positions.Add(polyline.EndPoint);
            }

            return positions.Distinct().ToList(); // Remove duplicates
        }

        public static List<Vector3d> CalculateMountOrientations(Polyline polyline, List<Point3d> positions)
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\nREAL: MountPlacementLogic.CalculateMountOrientations called.");
            List<Vector3d> orientations = new List<Vector3d>();
            if (polyline == null || positions == null || positions.Count == 0)
            {
                return orientations;
            }

            foreach (var pos in positions)
            {
                try
                {
                    // Ensure the point is actually on the polyline for GetParameterAtPoint
                    Point3d closestPointOnPoly = polyline.GetClosestPointTo(pos, Vector3d.ZAxis, false);
                    double param = polyline.GetParameterAtPoint(closestPointOnPoly);
                    Vector3d tangent = polyline.GetFirstDerivative(param);

                    if (tangent.LengthSq < 1e-12) // Check squared length to avoid Sqrt and handle zero vector
                    {
                        orientations.Add(Vector3d.XAxis); // Fallback for zero tangent
                        AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning: Zero tangent at mount position {pos}. Defaulting orientation.");
                    }
                    else
                    {
                        orientations.Add(tangent.GetNormal()); // Normalize the tangent
                    }
                }
                catch (System.Exception ex)
                {
                    AcApp.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nError calculating orientation for mount at {pos}: {ex.Message}. Defaulting.");
                    orientations.Add(Vector3d.XAxis); // Fallback on any error
                }
            }
            return orientations;
        }
    }
}