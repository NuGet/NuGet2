// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

// The code analysis in this case is wrong, and should be suppressed.
// The interface is callable via the ErrorHandler protected property.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PowerShell.Commands.NuGetBaseCommand.#NuGet.PowerShell.Commands.IErrorHandler.WriteProjectNotFoundError(System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PowerShell.Commands.NuGetBaseCommand.#NuGet.PowerShell.Commands.IErrorHandler.HandleError(System.Management.Automation.ErrorRecord,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PowerShell.Commands.NuGetBaseCommand.#NuGet.PowerShell.Commands.IErrorHandler.HandleException(System.Exception,System.Boolean,System.String,System.Management.Automation.ErrorCategory,System.Object)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PowerShell.Commands.NuGetBaseCommand.#NuGet.PowerShell.Commands.IErrorHandler.ThrowNoCompatibleProjectsTerminatingError()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PowerShell.Commands.NuGetBaseCommand.#NuGet.PowerShell.Commands.IErrorHandler.ThrowSolutionNotOpenTerminatingError()")]
