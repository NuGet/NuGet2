using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Resolver;
using NuGet.Resources;
using NuGet.VisualStudio.Resources;

namespace NuGet
{
    public class OperationExecutor
    {
        public ILogger Logger { get; set; }

        // !!! It seems this property should be deleted. Instead, the classes that use this property, such 
        // as SolutionUpdatesProvider, should just call
        //   RegisterPackageOperationEvents(packageManager, projectManager)
        // before userOperationExecutor.Execute(), then call 
        //   UnregisterPackageOperationEvents when everything's done.
        // Also, it seems this whole thing should be replaced with using projectManager's 
        // events themselves.
        public IPackageOperationEventListener PackageOperationEventListener { get; set; }
        
        // true means the exception generated when execute a project operation is caught.
        // false means such an exception is rethrown.
        public bool CatchProjectOperationException { get; set; }

        public OperationExecutor()
        {
            Logger = NullLogger.Instance;
        }

        public void Execute(IEnumerable<Operation> operations)
        {
            var executedOperations = new List<Operation>();
            try
            {
                foreach (var op in operations)
                {
                    executedOperations.Add(op);
                    
                    if (op.Target == PackageOperationTarget.PackagesFolder)
                    {
                        op.PackageManager.Logger = Logger;
                        op.PackageManager.Execute(op);
                    }
                    else
                    {
                        ExecuteProjectOperation(op);
                    }

                    if (op.Action == PackageAction.Install &&
                        op.Target == PackageOperationTarget.Project && 
                        op.ProjectManager.PackageManager != null &&
                        op.ProjectManager.PackageManager.BindingRedirectEnabled &&
                        op.ProjectManager.Project.IsBindingRedirectSupported)
                    {
                        op.ProjectManager.PackageManager.AddBindingRedirects(op.ProjectManager);
                    }
                }
            }
            catch
            {
                Rollback(executedOperations);
                throw;
            }
        }

        private void ExecuteProjectOperation(Operation operation)
        {
            Debug.Assert(operation.Target == PackageOperationTarget.Project);

            try
            {
                if (PackageOperationEventListener != null)
                {
                    PackageOperationEventListener.OnBeforeAddPackageReference(operation.ProjectManager);
                }

                operation.ProjectManager.Execute(operation);
            }
            catch (Exception e)
            {
                if (CatchProjectOperationException)
                {
                    Logger.Log(MessageLevel.Error, ExceptionUtility.Unwrap(e).Message);

                    if (PackageOperationEventListener != null)
                    {
                        PackageOperationEventListener.OnAddPackageReferenceError(operation.ProjectManager, e);
                    }
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                
                if (PackageOperationEventListener != null)
                {
                    PackageOperationEventListener.OnAfterAddPackageReference(operation.ProjectManager);
                }
            }
        }

        private void Rollback(List<Operation> executedOperations)
        {
            if (executedOperations.Count > 0)
            {
                // Only print the rollback warning if we have something to rollback
                Logger.Log(MessageLevel.Warning, VsResources.Warning_RollingBack);
            }

            executedOperations.Reverse();
            foreach (var operation in executedOperations)
            {
                var reverseOperation = new Operation(
                    new PackageOperation(
                        operation.Package,
                        operation.Action == PackageAction.Install ?
                        PackageAction.Uninstall :
                        PackageAction.Install)
                        {
                            Target = operation.Target
                        },
                    projectManager: operation.ProjectManager,
                    packageManager: operation.PackageManager);

                if (reverseOperation.Target == PackageOperationTarget.PackagesFolder)
                {
                    // Don't log anything during the rollback
                    reverseOperation.PackageManager.Logger = NullLogger.Instance;
                    reverseOperation.PackageManager.Execute(reverseOperation);
                }
                else
                {
                    // Don't log anything during the rollback
                    operation.ProjectManager.Logger = NullLogger.Instance;
                    operation.ProjectManager.Execute(reverseOperation);
                }
            }
        }
    }    
}
