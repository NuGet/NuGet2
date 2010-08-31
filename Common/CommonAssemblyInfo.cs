using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

// Common Assembly Info for all Plan9 Projects
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("Microsoft ASP.Net WebPages")]
[assembly: AssemblyCopyright("\x00a9 Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]


// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]

#if CI_BUILD
[assembly: AssemblyFileVersion("1.0.10830.265")]
#else
[assembly: AssemblyFileVersion("1.0.10830.265")]
#endif

#if SECURITY_TRANSPARENT && !CODE_COVERAGE
[assembly: SecurityTransparent]
#endif

// REVIEW: Does the command line tool need to be cls compliant?
#if CLS_COMPLIANT
[assembly: CLSCompliant(true)]
#endif

[assembly: NeutralResourcesLanguage("en-US")]
