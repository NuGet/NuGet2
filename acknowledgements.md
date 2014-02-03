NuGet Hall of Fame
==================

## NuGet 2.8
1. [Llewellyn Pritchard](https://www.codeplex.com/site/users/view/leppie) ([@leppie](https://twitter.com/leppie))
    - [#3466](https://nuget.codeplex.com/workitem/3466) - When packing packages, verifying Id of dependency packages.
1. [Maarten Balliauw](https://www.codeplex.com/site/users/view/maartenba) ([@maartenballiauw](https://twitter.com/maartenballiauw))
    - [#2379](https://nuget.codeplex.com/workitem/2379) - Remove the $metadata suffix when persistening feed credentials.
1. [Filip De Vos](https://www.codeplex.com/site/users/view/FilipDeVos) ([@foxtricks](https://twitter.com/foxtricks))
    - [#3538](http://nuget.codeplex.com/workitem/3538) - Support specifying project file for the nuget.exe update command.
1. [Juan Gonzalez](https://www.codeplex.com/site/users/view/jjgonzalez)
    - [#3536](http://nuget.codeplex.com/workitem/3536) - Replacement tokens not passed with -IncludeReferencedProjects.
1. [David Poole](https://www.codeplex.com/site/users/view/Sarkie) ([@Sarkie_Dave](https://twitter.com/Sarkie_Dave))
    - [#3677](http://nuget.codeplex.com/workitem/3677) - Fix nuget.push throwing OutOfMemoryException when pushing large package.
1. [Wouter Ouwens](https://www.codeplex.com/site/users/view/Despotes)
	- [#3666](http://nuget.codeplex.com/workitem/3666) - Fix incorrect target path when project references another CLI/C++ project.
1. [Adam Ralph](http://www.codeplex.com/site/users/view/adamralph) ([@adamralph](https://twitter.com/adamralph))
    - [#3639](https://nuget.codeplex.com/workitem/3639) - Allow packages to be installed as development dependencies by default
1. [David Fowler](https://www.codeplex.com/site/users/view/dfowler) ([@davidfowl](https://twitter.com/davidfowl))
    - [#3717](https://nuget.codeplex.com/workitem/3717) - Remove implicit upgrades to the latest patch version
1. [Gregory Vandenbrouck](https://www.codeplex.com/site/users/view/vdbg)
    - Several bug fixes and improvements for NuGet.Server, the nuget.exe mirror command, and others.
    - This work was done over several months, with Gregory working with us on the right timing to integrate into master for 2.8.
 
## NuGet 2.7

1. [Mike Roth](http://www.codeplex.com/site/users/view/mxrss) ([@mxrss](https://twitter.com/mxrss))
    - Show License url when listing packages and verbosity is detailed. 
1. [Adam Ralph](http://www.codeplex.com/site/users/view/adamralph) ([@adamralph](https://twitter.com/adamralph))
    - [#1956](http://nuget.codeplex.com/workitem/1956) - Add developmentDependency attribute to packages.config and use it in pack command to only include runtime packages
1. [Rafael Nicoletti](http://www.codeplex.com/site/users/view/tkrafael) ([@tkrafael](https://twitter.com/tkrafael))
    - Avoid duplicate Properties key in nuget.exe pack command.
1. [Ben Phegan](http://www.codeplex.com/site/users/view/benphegan) ([@BenPhegan](https://twitter.com/benphegan))
    - [#2610](http://nuget.codeplex.com/workitem/2610) - Increase machine cache size to 200.
1. [Slava Trenogin](http://www.codeplex.com/site/users/view/derigel) ([@derigel](https://twitter.com/derigel))
    - [#3217](http://nuget.codeplex.com/workitem/3217) - Fix NuGet dialog showing updates in the wrong tab
    - Fix Project.TargetFramework can be null in ProjectManager
    - [#3248](http://nuget.codeplex.com/workitem/3248) - Fix SharedPackageRepository FindPackage/FindPackagesById will fail on non-existent packageId
1. [Kevin Boyle](http://www.codeplex.com/site/users/view/KevinBoyleRG) ([@kevfromireland](https://twitter.com/kevfromireland))
    - [#3234](http://nuget.codeplex.com/workitem/3234) - Enable support for Nomad project
1. [Corin Blaikie](http://www.codeplex.com/site/users/view/corinblaikie) ([@corinblaikie](https://twitter.com/corinblaikie))
    - [#3252](http://nuget.codeplex.com/workitem/3252) - Fix push command fails with exit code 0 when file doesn't exist.
1. [Martin Veselý](http://www.codeplex.com/site/users/view/veselkamartin)
    - [#3226](http://nuget.codeplex.com/workitem/3226) - Fix bug with Add-BindingRedirect command when a project references a database project.
1. [Miroslav Bajtos](http://www.codeplex.com/site/users/view/miroslavbajtos) ([@bajtos](https://twitter.com/bajtos))
    - [#2891](http://nuget.codeplex.com/workitem/2891) - Fix bug of nuget.pack parsing wildcard in the 'exclude' attribute incorrectly.
1. [Justin Dearing](http://www.codeplex.com/site/users/view/zippy1981) ([@zippy1981](https://twitter.com/zippy1981))
    - [#3307](http://nuget.codeplex.com/workitem/3307) - Fix bug NuGet.targets does not pass $(Platform) to nuget.exe when restoring packages.
1. [Brian Federici](http://www.codeplex.com/site/users/view/benerdin) ([@benerdin](https://twitter.com/benerdin))
    - [#3294](http://nuget.codeplex.com/workitem/3294) - Fix bug in nuget.exe package command which would allow adding files with the same name but different casing, eventually causing "Item already exists" exception.
1. [Daniel Cazzulino](http://www.codeplex.com/site/users/view/dcazzulino) ([@kzu](https://twitter.com/kzu))
    - [#2990](http://nuget.codeplex.com/workitem/2990) - Add Version property to NetPortableProfile class.
1. [David Simner](https://www.codeplex.com/site/users/view/DavidSimner)
    - [#3460](https://nuget.codeplex.com/workitem/3460) - Fix bug NullReferenceException if requireApiKey = true, but the header X-NUGET-APIKEY isn't present
1. [Michael Friis](https://www.codeplex.com/site/users/view/friism) ([@friism](https://twitter.com/friism))
    - [#3278](https://nuget.codeplex.com/workitem/3278) - Fixes NuGet.Build targets file to so that it works correctly on MonoDevelop
1. [Pranav Krishnamoorthy](https://www.codeplex.com/site/users/view/pranavkm) ([@pranav_km](https://twitter.com/pranav_km))
    - Improve Restore command performance by increasing parallelization

## NuGet 2.5

1. [Daniel Plaisted](https://www.codeplex.com/site/users/view/dsplaisted) ([@dsplaisted](https://twitter.com/dsplaisted))
    - [#2847](https://nuget.codeplex.com/workitem/2847) - Add MonoAndroid, MonoTouch, and MonoMac to the list of known target framework identifiers.
1. [Andres G. Aragoneses](https://www.codeplex.com/site/users/view/knocte) ([@knocte](https://twitter.com/knocte))
    - [#2865](https://nuget.codeplex.com/workitem/2865) - Fix spelling of NuGet.targets for a case-sensitive OS
1. [David Fowler](https://www.codeplex.com/site/users/view/dfowler) ([@davidfowl](https://twitter.com/davidfowl))
    - Make the solution build on Mono.
1. [Andrew Theken](https://www.codeplex.com/site/users/view/atheken) ([@atheken](https://twitter.com/atheken))
    - Fix unit tests failing on Mono.
1. [Olivier Dagenais](https://www.codeplex.com/site/users/view/OliIsCool) ([@OliIsCool](https://twitter.com/oliiscool))
    - [#2920](https://nuget.codeplex.com/workitem/2920) - nuget.exe pack command does not propagate Properties to msbuild
1. [Miroslav Bajtos](https://www.codeplex.com/site/users/view/MiroslavBajtos) ([@bajtos](https://twitter.com/bajtos))
    - [#1511](https://nuget.codeplex.com/workitem/1511) - Modified XML handling code to preserve formatting.
1. [Adam Ralph](http://www.codeplex.com/site/users/view/adamralph) ([@adamralph](https://twitter.com/adamralph))
    - Added recognized words to custom dictionary to allow build.cmd to succeed.
1. [Bruno Roggeri](https://www.codeplex.com/site/users/view/broggeri)
    - Fix unit tests when running in localized VS.
1. [Gareth Evans](https://www.codeplex.com/site/users/view/garethevans)
    - Extracted interface from PackageService
1. [Maxime Brugidou](https://www.codeplex.com/site/users/view/brugidou) ([@brugidou](https://twitter.com/brugidou))
    - [#936](https://nuget.codeplex.com/workitem/936) - Handle project dependencies when packing
1. [Xavier Decoster](https://www.codeplex.com/site/users/view/XavierDecoster) ([@XavierDecoster](https://twitter.com/xavierdecoster))
    - [#2991](https://nuget.codeplex.com/workitem/2991), [#3164](https://nuget.codeplex.com/workitem/3164) - Support Clear Text Password when storing package source credentials in nuget.cofig files
1. [James Manning](http://www.codeplex.com/site/users/view/jmanning) ([@manningj](https://twitter.com/manningj))
    - [#3190](http://nuget.codeplex.com/workitem/3190), [#3191](http://nuget.codeplex.com/workitem/3191) - Fix Get-Package help description
