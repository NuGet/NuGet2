using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGet
{
    public interface ISettings
    {
        /// <summary>
        /// Returns the value associated with the specified key in the specified section.
        /// </summary>
        /// <param name="section">The section in which to find the value.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="isPath">If true, the value is treated as a path relative to
        /// the location of the config file. If false, the value is returned as is.</param>
        /// <returns>The value associated with the key.</returns>
        /// <example>
        /// Suppose the config file is located in directory c:\temp, and the value specified in the config
        /// file is ".\a", then the return value will be ".\a" when isPath is false, 
        /// and "c:\temp\.\a", when is Path is true.
        /// </example>
        /// <remarks>
        /// If the same key is specified multiple times, then the last entry wins.
        /// </remarks>
        string GetValue(string section, string key, bool isPath);

        /// <summary>
        /// Returns the values listed in the specified section.
        /// </summary>
        /// <param name="section">The section in which to find the values.</param>
        /// <param name="isPath">If true, the values are treated as a path relative to
        /// the location of the config file. If false, the values are returned as is.</param>
        /// <returns>The values listed in the specified section.</returns>
        /// <remarks>
        /// <para>If the same key is specified multiple times, all values are returned.</para>
        /// <para>If &lt;clear> exists, then all values specified before are discarded and will
        /// not be returned.</para>
        /// </remarks>
        IList<SettingValue> GetValues(string section, bool isPath);

        /// <summary>
        /// Returns the values listed in the specified subsection in the specified section.
        /// </summary>
        /// <param name="section">The section in which to find the subsection.</param>
        /// <param name="subsection">The subsection in which to find the values.</param>
        /// <remarks>
        /// <para>If the same key is specified multiple times, all values are returned.</para>
        /// <para>If &lt;clear> exists, then all values specified before are discarded and will
        /// not be returned.</para>
        /// </remarks>
        IList<SettingValue> GetNestedValues(string section, string subsection);
        
        void SetValue(string section, string key, string value);
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is the best fit for this internal class")]
        void SetValues(string section, IList<KeyValuePair<string, string>> values);
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is the best fit for this internal class")]
        void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values);
        
        bool DeleteValue(string section, string key);
        bool DeleteSection(string section);
    }
}
