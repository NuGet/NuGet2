using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet
{
    [Export(typeof(IPackageRule))]
    internal sealed class DefaultPackageRules : IPackageRule
    {
        internal static IEnumerable<IPackageRule> RuleSet
        {
            get { return DefaultPackageRuleSet.Rules.Concat(new IPackageRule[] {new DefaultManifestValuesRule()}); }
        }

        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            return RuleSet.SelectMany(p => p.Validate(package));
        }
    }
}