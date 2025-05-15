using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.IO;
using RailDesigner1; // Ensure this namespace matches BomExporter

using Moq;
namespace RailDesigner1.Tests
{
    [TestClass]
    public class BomExporterTests
    {
        private BomExporter exporter;

        [TestInitialize]
        public void Setup()
        {
            exporter = new BomExporter();
        }

        [TestMethod]
        public void TestPromptSelectEntities_MocksEditorInput_ReturnsSelectedObjects()
        {
            // Arrange: Use Moq to mock Editor and Document
            var mockEditor = new Mock<Editor>();
            var docMock = new Mock<Document>();
            docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
            Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document for test
        
            // Simulate a selection result
            var mockSelectionSet = new Mock<SelectionSet>();
            mockSelectionSet.Setup(s => s.GetObjectIds()).Returns(new ObjectId[] { ObjectId.Null }); // Mock object IDs for test
            mockEditor.Setup(e => e.GetSelection(It.IsAny<PromptSelectionOptions>())).Returns(new PromptSelectionResult(PromptStatus.OK, mockSelectionSet.Object));
        
            var exporter = new BomExporter();
        
            // Act
            var selectedObjects = exporter.PromptSelectEntities();
        
            // Assert
            Assert.IsNotNull(selectedObjects);
            Assert.AreEqual(1, selectedObjects.Length); // Expect one object based on mock
            // In a real test, assert based on expected IDs or other properties
        }

        [TestMethod]
        public void TestExtractBomData_WithBlockReference_ShouldReturnExpectedData()
        {
            // Arrange: Use in-memory database for isolated testing
            var db = new Database(false, true);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                var btr = new BlockTableRecord { Name = "TestBlock" };
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
        
                // Add attribute definitions with values
                var attDefComponentType = new AttributeDefinition(new Point3d(0, 0, 0), "COMPONENTTYPE", "Post", "", "COMPONENTTYPE");
                btr.Add(attDefComponentType);
                tr.AddNewlyCreatedDBObject(attDefComponentType, true);
        
                var attDefPartName = new AttributeDefinition(new Point3d(0, 0, 0), "PARTNAME", "SquarePost", "", "PARTNAME");
                btr.Add(attDefPartName);
                tr.AddNewlyCreatedDBObject(attDefPartName, true);
        
                var attDefDescription = new AttributeDefinition(new Point3d(0, 0, 0), "DESCRIPTION", "Standard post", "", "DESCRIPTION");
                btr.Add(attDefDescription);
                tr.AddNewlyCreatedDBObject(attDefDescription, true);
        
                // Add more attributes for completeness
                var attDefInstalledLength = new AttributeDefinition(new Point3d(0, 0, 0), "INSTALLED_LENGTH", "2.5", "", "INSTALLED_LENGTH");
                btr.Add(attDefInstalledLength);
                tr.AddNewlyCreatedDBObject(attDefInstalledLength, true);
        
                var br = new BlockReference(new Point3d(0, 0, 0), btr.ObjectId);
                var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                modelSpace.Add(br);
                tr.AddNewlyCreatedDBObject(br, true);
        
                tr.Commit();
            }
        
            var exporter = new BomExporter();
            var objectIds = new List<ObjectId> { br.ObjectId }; // Use the ObjectId from the test database
            var bomData = exporter.ExtractBomData(objectIds);
        
            // Assert
            Assert.AreEqual(1, bomData.Count);
            var data = bomData[0];
            Assert.AreEqual("Post", data["COMPONENTTYPE"]);
            Assert.AreEqual("SquarePost", data["PARTNAME"]);
            Assert.AreEqual("Standard post", data["DESCRIPTION"]);
            Assert.AreEqual(2.5, (double)data["INSTALLED_LENGTH"], 0.001);
            // Add assertions for other fields like WEIGHT, etc., based on calculation logic
        }

        [TestMethod]
        public void TestGenerateCsv_WithValidData_ShouldWriteCorrectFormat()
        {
            // Arrange
            var bomData = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>()
                {
                    { "COMPONENTTYPE", "Post" },
                    { "PARTNAME", "SquarePost" },
                    { "DESCRIPTION", "Standard post" },
                    { "QUANTITY", 1 },
                    { "INSTALLED_LENGTH", 2.5 },
                    { "INSTALLED_HEIGHT", 3.0 },
                    { "WIDTH", 0.1 },
                    { "HEIGHT", 0.1 },
                    { "MATERIAL", "Steel" },
                    { "FINISH", "Galvanized" },
                    { "WEIGHT", 5.0 },
                    { "SPECIAL_NOTES", "None" },
                    { "USER_ATTRIBUTE_1", "Attr1" },
                    { "USER_ATTRIBUTE_2", "Attr2" }
                }
            };
            string filePath = Path.Combine(Path.GetTempPath(), "test_bom.csv");
        
            // Act
            var exporter = new BomExporter();
            exporter.GenerateCsv(bomData, filePath); // Call with filePath as per new implementation
        
            // Assert
            Assert.IsTrue(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual("COMPONENTTYPE,PARTNAME,DESCRIPTION,QUANTITY,INSTALLED_LENGTH,INSTALLED_HEIGHT,WIDTH,HEIGHT,MATERIAL,FINISH,WEIGHT,SPECIAL_NOTES,USER_ATTRIBUTE_1,USER_ATTRIBUTE_2", lines[0]);
            Assert.AreEqual("Post,SquarePost,\"Standard post\",1,2.5,3.0,0.1,0.1,Steel,Galvanized,5.0,None,Attr1,Attr2", lines[1]);
            File.Delete(filePath); // Clean up
        }

        [TestMethod]
        public void TestExportBom_WithNoObjects_ShouldHandleGracefully()
        {
            // Arrange: Mock editor and document
            var mockEditor = new Mock<Editor>();
            var docMock = new Mock<Document>();
            docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
            Application.DocumentManager.MdiActiveDocument = docMock.Object;
        
            // Set up mocks for prompts
            mockEditor.Setup(e => e.GetString(It.IsAny<PromptStringOptions>())).Returns(new PromptResult(PromptStatus.OK, "CSV"));
            mockEditor.Setup(e => e.GetString(It.Is<PromptStringOptions>(p => p.Message.Contains("file path")))).Returns(new PromptResult(PromptStatus.OK, Path.Combine(Path.GetTempPath(), "test_bom.csv")));
            mockEditor.Setup(e => e.GetSelection(It.IsAny<PromptSelectionOptions>())).Returns(new PromptSelectionResult(PromptStatus.OK, new SelectionSet(new ObjectId[0])));
        
            // Act
            BomExporter.ExportBom(new ObjectIdCollection()); // Call static method
        
            // Assert: Verify no exception and error message is displayed
            mockEditor.Verify(e => e.WriteMessage("\nNo entities selected. Export aborted."), Times.Once);
        }
    }
    
    [TestMethod]
    public void TestExtractBomData_WithPolyline_ShouldReturnExpectedData()
    {
        // Arrange: Use in-memory database
        var db = new Database(false, true);
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
            var pline = new Polyline();
            pline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
            modelSpace.Add(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
            tr.Commit();
        }
    
        var exporter = new BomExporter();
        var objectIds = new List<ObjectId> { pline.ObjectId };
        var bomData = exporter.ExtractBomData(objectIds);
    
        // Assert
        Assert.AreEqual(1, bomData.Count);
        var data = bomData[0];
        Assert.AreEqual("Polyline", data["COMPONENTTYPE"]);
        Assert.AreEqual(10.0, (double)data["INSTALLED_LENGTH"], 0.001); // Length calculated from polyline
    }
    
    [TestMethod]
    public void TestExportBom_WithInvalidFormat_ShouldDisplayError()
    {
        // Arrange: Mock editor to return invalid format
        var mockEditor = new Mock<Editor>();
        var docMock = new Mock<Document>();
        docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
        Application.DocumentManager.MdiActiveDocument = docMock.Object;
    
        mockEditor.Setup(e => e.GetString(It.Is<PromptStringOptions>(p => p.Message.Contains("export format")))).Returns(new PromptResult(PromptStatus.OK, "TXT"));
    
        // Act
        BomExporter.ExportBom(new ObjectIdCollection()); // Call static method
    
        // Assert: Verify error message for unsupported format
        mockEditor.Verify(e => e.WriteMessage("\nUnsupported export format. Only CSV is supported."), Times.Once);
    }
}
// New test for user prompt on export format
[TestMethod]
public void ExportBom_PromptsForExportFormat()
{
    // Arrange: Mock the Editor to expect a GetString prompt for format
    var mockEditor = new Mock<Editor>();
    var docMock = new Mock<Document>();
    docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
    Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document

    // Set up mock to return a specific format input
    mockEditor.Setup(e => e.GetString(It.IsAny<PromptStringOptions>())).Returns(new PromptResult(PromptStatus.OK, "CSV"));

[TestMethod]
public void ExportBom_UnsupportedFormat_DisplaysErrorMessage()
{
    // Arrange
    mockEditor.Setup(e => e.GetString(It.IsAny<PromptStringOptions>())).Returns(new PromptResult(PromptStatus.OK, "TXT"));

    // Act
    exporter.ExportBom();

    // Assert
    mockEditor.Verify(e => e.WriteMessage("\nUnsupported export format. Only CSV is supported."), Times.Once);
}

[TestMethod]
public void ExportBom_InvalidFilePath_DisplaysErrorMessage()
{
    // Arrange
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV"))
        .Returns(new PromptResult(PromptStatus.OK, "invalid_path")); // Invalid path

    // Act
    exporter.ExportBom();

    // Assert
    mockEditor.Verify(e => e.WriteMessage("\nInvalid file path. Must be a valid .csv file path."), Times.Once);
}

[TestMethod]
public void ExportBom_NoEntitiesSelected_DisplaysMessage()
{
    // Arrange
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV"))
        .Returns(new PromptResult(PromptStatus.OK, "C:\\valid\\path.csv"));
    mockEditor.Setup(e => e.GetSelection(It.IsAny<PromptSelectionOptions>())).Returns(new PromptResult(PromptStatus.OK, new SelectionSet(new ObjectId[0])));

    // Act
    exporter.ExportBom();

    // Assert
    mockEditor.Verify(e => e.WriteMessage("\nNo entities selected."), Times.Once);
}

[TestMethod]
public void ExportBom_DataInconsistency_MissingAttributes_HandlesGracefully()
{
    // Arrange
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV"))
        .Returns(new PromptResult(PromptStatus.OK, "C:\\valid\\path.csv"));
    var mockSelection = new Mock<SelectionSet>();
    var objectIds = new ObjectId[] { ObjectId.Null };
    mockSelection.Setup(s => s.GetObjectIds()).Returns(objectIds);
    mockEditor.Setup(e => e.GetSelection(It.IsAny<PromptSelectionOptions>())).Returns(new PromptResult(PromptStatus.OK, mockSelection.Object));
    var mockBr = new Mock<BlockReference>();
    mockBr.Setup(b => b.Name).Returns("TestBlock");
    mockBr.Setup(b => b.AttributeCollection).Returns(new ObjectIdCollection()); // No attributes
    mockTransaction.Setup(t => t.GetObject(It.IsAny<ObjectId>(), OpenMode.ForRead)).Returns(mockBr.Object);

    // Act & Assert: Should not throw, but data might be incomplete; verify CSV output or message
    // Note: May need to mock file system or capture output
    Assert.IsNotNull(exporter.ExportBom()); // Assuming it returns or we check side effects
    // Add assertion for expected behavior, e.g., log or handle missing data
}

[TestMethod]
public void ExportBom_SuccessfulExport_CreatesCsvWithFormattedData()
{
    // Arrange
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV"))
        .Returns(new PromptResult(PromptStatus.OK, "C:\\test_bom.csv"));
    var mockSelection = new Mock<SelectionSet>();
    var objectIds = new ObjectId[] { ObjectId.Null };
    mockSelection.Setup(s => s.GetObjectIds()).Returns(objectIds);
    mockEditor.Setup(e => e.GetSelection(It.IsAny<PromptSelectionOptions>())).Returns(new PromptResult(PromptStatus.OK, mockSelection.Object));
    var mockBr = new Mock<BlockReference>();
    mockBr.Setup(b => b.Name).Returns("TestBlock");
    var mockAttRef = new Mock<AttributeReference>();
    mockAttRef.Setup(a => a.Tag).Returns("TAG1");
    mockAttRef.Setup(a => a.TextString).Returns("Value,With,Comma"); // Test formatting for commas
    var attCollection = new ObjectIdCollection(new ObjectId[] { mockAttRef.Object.ObjectId });
    mockBr.Setup(b => b.AttributeCollection).Returns(attCollection);
    mockTransaction.Setup(t => t.GetObject(It.IsAny<ObjectId>(), OpenMode.ForRead)).Returns(mockBr.Object);

    // Act
    exporter.ExportBom();

    // Assert
    // Check if file is created and content is properly formatted (e.g., quoted values)
    Assert.IsTrue(File.Exists("C:\\test_bom.csv")); // Note: In real tests, use a temp file path
    var content = File.ReadAllText("C:\\test_bom.csv");
    Assert.IsTrue(content.Contains("\"TAG1\",\"Value,With,Comma\"")); // Verify quoting for CSV safety
    File.Delete("C:\\test_bom.csv"); // Clean up
    mockEditor.Verify(e => e.WriteMessage(It.Is<string>(msg => msg.Contains("BOM exported successfully"))), Times.Once);
}
    var exporter = new BomExporter(); // Instantiate the exporter

    // Act: Call ExportBom, but since it uses the mocked editor, it should not throw
    exporter.ExportBom(); // This will use the mocked editor

    // Assert: Verify that GetString was called with the correct message for format
    mockEditor.Verify(e => e.GetString(It.Is<PromptStringOptions>(p => p.Message == "\nEnter export format (e.g., CSV): ")), Times.Once);
}

// New test for user prompt on file path
[TestMethod]
public void ExportBom_PromptsForFilePath()
{
    // Arrange: Mock the Editor to expect a GetString prompt for file path
    var mockEditor = new Mock<Editor>();
    var docMock = new Mock<Document>();
    docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
    Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document

    // Set up mock to return a specific file path input
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV")) // First call for format
        .Returns(new PromptResult(PromptStatus.OK, "C:\\test\\bom.csv")); // Second call for path

    var exporter = new BomExporter();

    // Act
    exporter.ExportBom();

    // Assert: Verify that GetString was called with the correct message for file path
    mockEditor.Verify(e => e.GetString(It.Is<PromptStringOptions>(p => p.Message == "\nEnter file path for BOM export (e.g., C:\\Path\\To\\File.csv): ")), Times.Once);
}

// New test for error handling on invalid file path
[TestMethod]
public void ExportBom_InvalidFilePath_ErrorHandled()
{
    // Arrange: Mock the Editor to return an invalid file path
    var mockEditor = new Mock<Editor>();
    var docMock = new Mock<Document>();
    docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
    Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document

    // Set up mock to return inputs that lead to invalid path
    mockEditor.SetupSequence(e => e.GetString(It.IsAny<PromptStringOptions>()))
        .Returns(new PromptResult(PromptStatus.OK, "CSV")) // Format prompt
        .Returns(new PromptResult(PromptStatus.OK, "invalid_path")); // Invalid file path input

    // Since WriteMessage is void, we can verify it was called
    mockEditor.Setup(e => e.WriteMessage(It.IsAny<string>())).Verifiable();

    var exporter = new BomExporter();

    // Act
    exporter.ExportBom();

    // Assert: Verify that an error message was written for invalid file path
    mockEditor.Verify(e => e.WriteMessage("\nInvalid file path. Must be a valid .csv file path."), Times.Once);
    // Also ensure no export happened, but since GenerateCsv isn't called, we can verify that if needed
[TestMethod]
        public void ExportBom_StubImplementation_WritesMessageToEditor()
        {
            // Arrange: Mock the Editor to capture written messages
            var mockEditor = new Mock<Editor>();
            var docMock = new Mock<Document>();
            docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
            Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document

            // Act
            var exporter = new BomExporter();
            exporter.ExportBom();

            // Assert: Verify that the specific stub message was written to the editor
            mockEditor.Verify(e => e.WriteMessage("\nBomExporter.ExportBom() called (stub implementation).\nPlease implement actual logic.\n"), Times.Once);
        }
}