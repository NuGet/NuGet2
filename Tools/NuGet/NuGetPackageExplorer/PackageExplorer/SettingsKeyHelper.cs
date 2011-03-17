using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace PackageExplorer
{
    internal sealed class SettingsKeyHelper
    {
        public const string ApiKeysSectionName = "apikeys";

        public static string ReadApiKeyFromSettingFile()
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            return settings.GetDecryptedValue(ApiKeysSectionName, GalleryServer.DefaultGalleryServerUrl);
        }

        public static void WriteApiKeyToSettingFile(string apiKey)
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(ApiKeysSectionName, GalleryServer.DefaultGalleryServerUrl, apiKey);
        }
    }
}