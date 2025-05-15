using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace RailDesigner1
{
    public class AutoCadScriptGenerator
    {
        public bool GenerateScript(string outputPath, List<string> scriptCommands)
        {
            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Build script content from commands
                string scriptContent = string.Join("\n", scriptCommands) + "\n";

                // Write the script content to the specified file
                File.WriteAllText(outputPath, scriptContent);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error if needed
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;
                    ed.WriteMessage($"\nError generating script: {ex.Message}");
                }
                return false;
            }
        }
    }
}