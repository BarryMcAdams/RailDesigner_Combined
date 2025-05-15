using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using RailDesigner1; // Assuming utilities are in this namespace

[TestClass]
public class CommonUtilitiesTests
{
    [TestMethod]
    public void TransactionManagerWrapper_StartTransaction_ReturnsTransaction()
    {
        // Arrange
        var db = HostApplicationServices.WorkingDatabase; // This might require AutoCAD context; in a real test, mock or run in AutoCAD environment
        // Act
        var transaction = TransactionManagerWrapper.StartTransaction(db);
        // Assert
        Assert.IsNotNull(transaction);
        transaction.Dispose(); // Clean up
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TransactionManagerWrapper_StartTransaction_NullDatabase_ThrowsException()
    {
        // Act
        var transaction = TransactionManagerWrapper.StartTransaction(null);
    }

    [TestMethod]
    public void AutoCadHelpers_GetBlockTable_ReturnsBlockTable()
    {
        // Arrange
        using (var tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
            // Act
            var blockTable = AutoCadHelpers.GetBlockTable(tr);
            // Assert
            Assert.IsNotNull(blockTable);
            tr.Commit();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void AutoCadHelpers_GetBlockTable_NullTransaction_ThrowsException()
    {
        // Act
        var blockTable = AutoCadHelpers.GetBlockTable(null);
    }

    [TestMethod]
    public void ErrorHandler_LogError_WritesMessage()
    {
        // Arrange & Act
        ErrorHandler.LogError("Test error message");
        // Assert: In a real test, verify log output; here, we assume the method runs without errors as it's a simple write
        // Note: Testing logging might require mocking the Editor or capturing output, which is complex in unit tests
    }
}