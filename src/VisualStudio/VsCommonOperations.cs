using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsCommonOperations))]
    public class VsCommonOperations : IVsCommonOperations
    {
        private readonly DTE _dte;

        public VsCommonOperations() : this(ServiceLocator.GetInstance<DTE>())
        {
        }

        public VsCommonOperations(DTE dte)
        {
            _dte = dte;
        }

        public bool OpenFile(string filePath)
        {
            if (filePath == null) 
            {
                throw new ArgumentNullException("filePath");
            }

            if (_dte.ItemOperations != null && File.Exists(filePath))
            {
                Window window = _dte.ItemOperations.OpenFile(filePath);
                return window != null;
            }

            return false;
        }

        public IDisposable SaveSolutionExplorerNodeStates(ISolutionManager solutionManager)
        {
            IDictionary<string, ISet<VsHierarchyItem>> expandedNodes = VsHierarchyHelper.GetAllExpandedNodes(solutionManager);
            return new DisposableAction(() =>
                {
                    VsHierarchyHelper.CollapseAllNodes(solutionManager, expandedNodes);
                    expandedNodes.Clear();
                    expandedNodes = null;
                });
        }

        private class DisposableAction : IDisposable
        {
            private Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                if (_action != null)
                {
                    _action();
                    _action = null;
                }
            }
        }
    }
}