using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NuGet.Dialog.Providers {

    public class ExecuteCompletedEventArgs : AsyncCompletedEventArgs {
        private IEnumerable<IPackage> _results;
        private int _totalCount;
        private int _pageNumber;

        public ExecuteCompletedEventArgs(Exception exception, bool canceled, object userState, IEnumerable<IPackage> results, int pageNumber, int totalCount) :
            base(exception, canceled, userState) {
            _results = results;
            _pageNumber = pageNumber;
            _totalCount = totalCount;
        }
        public IEnumerable<IPackage> Results {
            get {
                RaiseExceptionIfNecessary();
                return _results;
            }
        }

        public int TotalCount {
            get {
                RaiseExceptionIfNecessary();
                return _totalCount;
            }
        }

        public int PageNumber {
            get {
                RaiseExceptionIfNecessary();
                return _pageNumber;
            }
        }
    }
}
