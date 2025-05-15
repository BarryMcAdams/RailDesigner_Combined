using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using Autodesk.AutoCAD.Runtime; // Required for ErrorStatus

namespace RailDesigner1
{
    public static class TransactionManagerWrapper
    {
        public static Transaction GetTransaction(this Autodesk.AutoCAD.DatabaseServices.TransactionManager tm)
        {
            return tm.StartTransaction();
        }

        public static void CommitTransaction(Transaction transaction)
        {
            transaction.Commit();
        }

        public static void DisposeTransaction(Transaction transaction)
        {
            transaction.Dispose();
        }

        // Add error handling for transactions  
        public static Transaction StartTransactionWithErrorHandling(Database database)
        {
            try
            {
                return database.TransactionManager.StartTransaction();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acadEx) when (acadEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.NotInitializedYet)
            {
                throw new InvalidOperationException("Database not initialized. Ensure AutoCAD is properly set up.");
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("Failed to start transaction.", ex);
            }
        }
    }
}