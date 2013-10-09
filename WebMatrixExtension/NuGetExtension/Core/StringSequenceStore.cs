using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Class that implements a simple, persistent store for a sequence of strings.
    /// </summary>
    internal class StringSequenceStore : IStringSequenceStore
    {
        /// <summary>
        /// Reference to the IPreferencesService.
        /// </summary>
        private readonly IPreferences _preferences;

        /// <summary>
        /// Unique name for the sequence of feeds
        /// </summary>
        private const string FeedsSequenceKey = "Feeds";
        private static readonly string[] EnvironmentNewLine = new string[] { Environment.NewLine };

        /// <summary>
        /// Initializes a new instance of the StringSequenceStore class.
        /// </summary>
        /// <param name="preferences">A preferences store</param>
        public StringSequenceStore(IPreferences preferences)
        {
            _preferences = preferences;
        }

        /// <summary>
        /// Saves a sequence of strings to persistent storage.
        /// </summary>
        /// <param name="strings">Sequence of strings.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Safe to dispose memory stream twice. Alternative is to not explictly dispose memory stream but habitually assuming someone else will close the stream could lead to leaks")]
        public void Save(IEnumerable<string> strings)
        {
            // feedSources are stored in a single string delimited by default line terminator
            // Note that feedSources are either urls or the name of the feedSource
            string feedSources = String.Empty;
            if (strings != null)
            {
                feedSources = String.Join(Environment.NewLine, strings);
            }

            _preferences.SetValue(FeedsSequenceKey, feedSources);
        }

        /// <summary>
        /// Loads a sequence of strings from persistent storage.
        /// </summary>
        /// <returns>Sequence of strings.</returns>
        public IEnumerable<string> Load()
        {
            // Read the list of strings from the store
            string feedSequence = _preferences.GetValue(FeedsSequenceKey);
            IEnumerable<string> feedSources;

            if (!String.IsNullOrEmpty(feedSequence))
            {
                feedSources = feedSequence.Split(EnvironmentNewLine, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                feedSources = new List<String>();
            }

            return feedSources;
        }
    }
}
