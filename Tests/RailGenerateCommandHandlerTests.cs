using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Moq;
using RailDesigner1;

namespace RailDesigner1.Tests
{
    [TestClass]
    public class RailGenerateCommandHandlerTests
    {
        [TestMethod]
        public void Execute_StubImplementation_WritesMessageToEditor()
        {
            // Arrange: Mock the Editor to capture written messages
            var mockEditor = new Mock<Editor>();
            var docMock = new Mock<Document>();
            docMock.Setup(d => d.Editor).Returns(mockEditor.Object);
            Application.DocumentManager.MdiActiveDocument = docMock.Object; // Set mock document

            // Act
            var handler = new RailGenerateCommandHandler();
            handler.Execute();

            // Assert: Verify that the specific stub message was written to the editor
            mockEditor.Verify(e => e.WriteMessage("\nRailGenerateCommandHandler.Execute() called (stub implementation).\nPlease implement actual logic.\n"), Times.Once);
        }
    }
}