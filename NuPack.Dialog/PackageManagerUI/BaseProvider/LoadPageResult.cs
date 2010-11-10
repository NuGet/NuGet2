using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NuGet.Dialog.Providers {

    internal class LoadPageResult  {
        private IEnumerable<IPackage> _packages;
        private int _totalCount;
        private int _pageNumber;

        public LoadPageResult(IEnumerable<IPackage> packages, int pageNumber, int totalCount) {
            _packages = packages;
            _pageNumber = pageNumber;
            _totalCount = totalCount;
        }

        public IEnumerable<IPackage> Packages {
            get {
                return _packages;
            }
        }

        public int TotalCount {
            get {
                return _totalCount;
            }
        }

        public int PageNumber {
            get {
                return _pageNumber;
            }
        }
    }
}
