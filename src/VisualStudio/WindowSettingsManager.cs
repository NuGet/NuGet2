using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.Internal.Web.Utils;

namespace NuGet.VisualStudio {
    [Export(typeof(IWindowSettingsManager))]
    internal class WindowSettingsManager : SettingsManagerBase, IWindowSettingsManager {

        private const string KeyTemplate = @"NuGet\Windows\";

        public WindowSettingsManager() :
            this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public WindowSettingsManager(IServiceProvider serivceProvider) :
            base(serivceProvider) {
        }

        public Size GetWindowSize(string windowToken) {
            if (String.IsNullOrEmpty(windowToken)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "windowToken");
            }

            string collection = KeyTemplate + windowToken;
            int width = ReadInt32(collection, "Width", 0);
            int height = ReadInt32(collection, "Height", 0);
            return new Size(width, height);
        }

        public void SetWindowSize(string windowToken, Size size) {
            if (String.IsNullOrEmpty(windowToken)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "windowToken");
            }

            string collection = KeyTemplate + windowToken;

            // The registry can't store double values, so we cast it to int. 
            // I believe the precision loss is negligible.
            int width = (int)size.Width;
            int height = (int)size.Height;

            WriteInt32(collection, "Width", width);
            WriteInt32(collection, "Height", height);
        }
    }
}
