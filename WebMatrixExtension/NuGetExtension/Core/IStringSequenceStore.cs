using System.Collections.Generic;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interface that represents a simple, persistent store for a sequence of strings.
    /// </summary>
    public interface IStringSequenceStore
    {
        /// <summary>
        /// Saves a sequence of strings to persistent storage.
        /// </summary>
        /// <param name="strings">Sequence of strings.</param>
        void Save(IEnumerable<string> strings);

        /// <summary>
        /// Loads a sequence of strings from persistent storage.
        /// </summary>
        /// <returns>Sequence of strings.</returns>
        IEnumerable<string> Load();
    }
}
