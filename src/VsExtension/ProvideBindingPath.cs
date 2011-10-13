/*
   Copyright 2010 Microsoft

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Text;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// This attribute registers a path that should be probed for candidate assemblies at assembly load time.
    /// 
    /// For example:
    ///   [...\VisualStudio\10.0\BindingPaths\{5C48C732-5C7F-40f0-87A7-05C4F15BC8C3}]
    ///     "$PackageFolder$"=""
    ///     
    /// This would register the "PackageFolder" (i.e. the location of the pkgdef file) as a directory to be probed
    /// for assemblies to load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProvideBindingPathAttribute : RegistrationAttribute
    {
        /// <summary>
        /// An optional SubPath to set after $PackageFolder$. This should be used
        /// if the assemblies to be probed reside in a different directory than
        /// the pkgdef file.
        /// </summary>
        public string SubPath { get; set; }

        private static string GetPathToKey(RegistrationContext context)
        {
            return string.Concat(@"BindingPaths\", context.ComponentType.GUID.ToString("B").ToUpperInvariant());
        }

        public override void Register(RegistrationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            using (Key childKey = context.CreateKey(GetPathToKey(context)))
            {
                StringBuilder keyName = new StringBuilder(context.ComponentPath);
                if (!string.IsNullOrEmpty(SubPath))
                {
                    keyName.Append("\\");
                    keyName.Append(SubPath);
                }

                childKey.SetValue(keyName.ToString(), string.Empty);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.RemoveKey(GetPathToKey(context));
        }
    }
}
