// MountPlacementLogicWrapper.cs
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace RailDesigner1
{
    public class MountPlacementLogicWrapper
    {
        private MountPlacementLogic mountLogic = new MountPlacementLogic();

        public List<Point3d> CalculateMountPositions(Polyline path, double spacing)
        {
            // Delegate to core logic, ensuring input validation
            if (path == null || spacing <= 0)
            {
                throw new ArgumentException("Invalid path or spacing provided.");
            }
            return mountLogic.ComputePositions(path, spacing);
        }

        public List<Vector3d> CalculateMountOrientations(Polyline path, List<Point3d> positions)
        {
            // Delegate to core logic, handle cases where positions might be empty or invalid
            if (path == null || positions == null)
            {
                throw new ArgumentException("Invalid path or positions provided.");
            }
            return mountLogic.ComputeOrientations(path, positions);
        }
    }
}