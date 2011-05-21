using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Internal.Web.Utils;

namespace NuGet.VisualStudio {
    public static class PathHelper {
        public static string SmartTruncate(string path, int maxWidth) {
            if (maxWidth < 6) {
                string message = String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Must_Be_GreaterThanOrEqualTo, 6);
                throw new ArgumentOutOfRangeException("maxWidth", message);
            }

            if (path == null) {
                throw new ArgumentNullException("path");
            }

            if (path.Length <= maxWidth) {
                return path;
            }

            // get the leaf folder name of this directory path
            // e.g. if the path is C:\documents\projects\visualstudio\, we want to get the 'visualstudio' part.
            string folder = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? String.Empty;
            // surround the folder name with the pair of \ characters.
            folder = Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar;

            string root = Path.GetPathRoot(path);
            int remainingWidth = maxWidth - root.Length - 3;       // 3 = length(ellipsis)

            // is the directory name too big? 
            if (folder.Length >= remainingWidth) {
                // yes drop leading backslash and eat into name
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}...{1}",
                    root,
                    folder.Substring(folder.Length - remainingWidth));
            }
            else {
                // no, show like VS solution explorer (drive+ellipsis+end)
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}...{1}",
                    root,
                    folder);
            }
        }
    }
}
