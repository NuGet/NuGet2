using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace NuGet.WebMatrix
{
    internal class RequirementsViewModel : IEnumerable<RequirementViewModel>
    {
        private List<RequirementViewModel> _requirementsList;
        private Requirements _requirements;

        /// <summary>
        /// Construct requirements view model for the specified package.
        /// </summary>
        /// <param name="package"></param>
        internal RequirementsViewModel(string packageTags, Version osVersion, Version spVersion, string type, string architecture)
        {
            _requirements = new Requirements(packageTags, osVersion, spVersion, type, architecture);
        }

        /// <summary>
        /// Construct requirements view model from the package requirements.
        /// </summary>
        /// <param name="requirements"></param>
        internal RequirementsViewModel(Requirements requirements)
        {
            _requirements = requirements;
        }

        /// <summary>
        /// Determines if the system specification does not meet the requirements.
        /// </summary>
        public bool AreNotMet
        {
            get
            {
                return _requirements.AreNotMet;
            }
        }

        /// <summary>
        /// Determines if the system specification meets the requirements.
        /// </summary>
        public bool AreMet
        {
            get
            {
                return _requirements.AreMet;
            }
        }

        /// <summary>
        /// Returns the private collection of requirements
        /// </summary>
        private List<RequirementViewModel> RequirementsList
        {
            get
            {
                if (_requirementsList == null)
                {
                    _requirementsList = new List<RequirementViewModel>();
                    foreach (Requirement requirement in _requirements)
                    {
                        _requirementsList.Add(new RequirementViewModel(requirement));
                    }
                }

                return _requirementsList;
            }
        }

        /// <summary>
        /// Return enumerator for the collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<RequirementViewModel> GetEnumerator()
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
