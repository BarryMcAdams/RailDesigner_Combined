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