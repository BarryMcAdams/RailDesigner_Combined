// Utils/ErrorLogger.cs
using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices; // For Application fallback
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application; // Alias

namespace RailDesigner1.Utils
{
    public static class ErrorLogger
    {
        private static StreamWriter _logWriter;
        private static string _logFilePath;

        public static void Initialize(string logFileName)
        {
            try
            {
                string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RailDesigner1", "Logs");
                Directory.CreateDirectory(logDirectory); // Ensures the directory exists
                _logFilePath = Path.Combine(logDirectory, logFileName);

                _logWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                LogMessage($"ErrorLogger Initialized. Log file: {_logFilePath}");
            }
            catch (Exception ex)
            {
                AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nErrorLogger initialization failed: {ex.Message}. Attempted log path: {_logFilePath}\n");
                // _logWriter will remain null, so logging attempts will be no-ops
            }
        }

        public static void LogMessage(string message)
        {
            try
            {
                _logWriter?.WriteLine($"{DateTime.Now}: {message}");
            }
            catch (Exception ex)
            {
                // Avoid crashing the application if logging itself fails
                System.Diagnostics.Debug.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }

        public static void LogError(string message, Exception ex = null)
        {
            try
            {
                _logWriter?.WriteLine($"{DateTime.Now} [ERROR]: {message}");
                if (ex != null)
                {
                    _logWriter?.WriteLine($"Exception Details: {ex.ToString()}");
                }
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write error to log: {logEx.Message}");
            }
        }

        public static void Close()
        {
            if (_logWriter != null)
            {
                LogMessage("ErrorLogger Closing.");
                _logWriter.Close();
                _logWriter.Dispose();
                _logWriter = null;
            }
        }
    }
}