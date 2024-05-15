using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace TableTest.Utilities
{
    public static class Active
    {
        public static Document Document
        {
            get { return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument; }
        }

        public static DocumentCollection DocumentManager
        {
            get { return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager; }
        }

        public static Editor Editor
        {
            get { return Document.Editor; }
        }

        public static Database Database
        {
            get { return Document.Database; }
        }

        public static bool IsInModel()
        {
            if (Database.TileMode)
                return true;
            else
                return false;
        }

        public static bool IsInLayout()
        {
            return !IsInModel();
        }

        public static bool IsInLayoutPaper()
        {
            if (Database.TileMode)
                return false;
            else
            {
                if (Database.PaperSpaceVportId == ObjectId.Null)
                    return false;
                else if (Editor.CurrentViewportObjectId == ObjectId.Null)
                    return false;
                else if (Editor.CurrentViewportObjectId == Database.PaperSpaceVportId)
                    return true;
                else
                    return false;
            }
        }

        public static bool IsInLayoutViewport()
        {
            return IsInLayout() && !IsInLayoutPaper();
        }

        /// <summary>
        /// Sends a string to the command line in the active Editor.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public static void WriteMessage(string message)
        {
            Editor.WriteMessage(message);
        }

        /// <summary>
        /// Sends a string to the command line in the active Editor using string.Format
        /// </summary>
        /// <param name="message">The message containing format specifications.</param>
        /// <param name="parameter">The variables to substitute into the format string.</param>
        public static void WriteMessage(string message, params object[] parameter)
        {
            Editor.WriteMessage(message, parameter);
        }

    }
}
