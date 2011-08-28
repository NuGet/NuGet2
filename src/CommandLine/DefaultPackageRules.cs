using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet {
    [Export(typeof(IPackageRule))]
    internal sealed class DefaultPackageRules : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            return DefaultPackageRuleSet.Rules.SelectMany(p => p.Validate(package));
        }
    }
}