using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using RailDesigner1; // Assuming RailingDesign is in this namespace

[TestClass]
public class PostPlacementLogicTests
{
    [TestMethod]
    public void CalculatePostPositions_SimplePolyline_ReturnsCorrectPositions()
    {
        // Arrange
        var polyline = new Polyline();
        polyline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
        polyline.AddVertexAt(1, new Point2d(100, 0), 0, 0, 0);
        var design = new RailingDesign { RailHeight = 48, TopCapHeight = 2, PostSize = "2x2", MountingType = "Plate" };
        var btr = new BlockTableRecord(); // Mock or stub, but in real test, might need AutoCAD context
        var tr = new Transaction(); // Similarly, stubbed for testing

        // Act
        var positions = PostPlacementLogic.CalculatePostPositions(polyline, design, btr, tr);

        // Assert
        Assert.AreEqual(3, positions.Count); // Expect positions at 0, 50, 100 (targetSpacing 50.0)
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(0, 0, 0))));
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(50, 0, 0))));
        Assert.IsTrue(positions.Any(p => p.IsEqualTo(new Point3d(100, 0, 0))));
    }

    // Add more tests as needed, e.g., for different mounting types or complex polylines
}