using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using RailDesigner1; // Assuming RailingDesign is in this namespace

[TestClass]
public class PicketPlacementLogicTests
{
    [TestMethod]
    public void CalculatePicketPositions_SimplePolyline_ReturnsCorrectPositions()
    {
        // Arrange
        var polyline = new Polyline();
        polyline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
        polyline.AddVertexAt(1, new Point2d(12, 0), 0, 0, 0); // 12 inches, target spacing 6.0, should have 3 positions
        var design = new RailingDesign { RailHeight = 48, PicketSize = "1.5x48" };
        var btr = new BlockTableRecord(); // Stub for testing
        var tr = new Transaction(); // Stub for testing

        // Act
        var positions = PicketPlacementLogic.CalculatePicketPositions(polyline, design, btr, tr);

        // Assert
        Assert.AreEqual(3, positions.Count); // Expect positions at 0, 6, 12
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(0, 0, 0))));
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(6, 0, 0))));
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(12, 0, 0))));
    }

    // Add more tests as needed
}