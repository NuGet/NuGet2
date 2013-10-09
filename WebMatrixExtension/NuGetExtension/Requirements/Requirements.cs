using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace NuGet.WebMatrix
{
    internal class Requirements : IEnumerable<Requirement>
    {
        private Version _osVersion;
        private Version _spVersion;
        private string _type;
        private string _architecture;
        private List<Requirement> _requirementsList;

        /// <summary>
        /// Construct requirements for the specified package using the current OS
        /// </summary>
        /// <param name="packageTags"></param>
        internal Requirements(string packageTags)
            : this(packageTags, SystemInformation.Current.OSVersion, SystemInformation.Current.SPVersion, SystemInformation.Current.ProductType, SystemInformation.Current.Architecture)
        {
        }

        /// <summary>
        /// Construct requirements for the specified package using the specified OS configuration
        /// </summary>
        /// <param name="packageTags"></param>
        /// <param name="osVersion"></param>
        /// <param name="spVersion"></param>
        /// <param name="type"></param>
        /// <param name="architecture"></param>
        internal Requirements(string packageTags, Version osVersion, Version spVersion, string type, string architecture)
        {
            // Save the system information for validation
            _osVersion = osVersion;
            _spVersion = spVersion;
            _type = type;
            _architecture = architecture;

            if (!string.IsNullOrEmpty(packageTags))
            {
                // Split the package tags up
                // In NuGet the typical delimiter is space but we also support comma in the web matrix extension gallery.
                string[] tags = packageTags.Split(new char[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Parse each tag to see if it is a valid requirement and if so add it to our collection
                foreach (string tag in tags)
                {
                    Requirement systemRequirement = new Requirement(tag);
                    if (systemRequirement.Parse())
                    {
                        RequirementsList.Add(systemRequirement);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the system specification does not meet the requirements.
        /// </summary>
        public bool AreNotMet
        {
            get
            {
                return !AreMet;
            }
        }

        /// <summary>
        /// Determines if the system specification meets the requirements.
        /// </summary>
        public bool AreMet
        {
            get
            {
                if (!RequirementsList.Any())
                {
                    // No requirements
                    return true;
                }

                foreach (Requirement systemRequirement in RequirementsList)
                {
                    if (systemRequirement.IsMet(_osVersion, _spVersion, _type, _architecture))
                    {
                        // Meets one of the requirements
                        return true;
                    }
                }

                // None of the requirements were met
                return false;
            }
        }

        /// <summary>
        /// Returns the private collection of requirements
        /// </summary>
        private List<Requirement> RequirementsList
        {
            get
            {
                if (_requirementsList == null)
                {
                    _requirementsList = new List<Requirement>();
                }

                return _requirementsList;
            }
        }

        /// <summary>
        /// Return enumerator for the collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Requirement> GetEnumerator()
        {
            return RequirementsList.GetEnumerator();
        }

        /// <summary>
        /// Return enumerator for the collection
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return RequirementsList.GetEnumerator();
        }
    }
}
