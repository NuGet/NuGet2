Change Log / Release Notes
==========================

## NuGet 2.8
* [#3538](http://nuget.codeplex.com/workitem/3538) - Support specifying project file for the nuget.exe update command.
* Load packages.<project_name>.config if it already exists.
* Option -PackageSaveMode is supported by nuget.exe install and restore commands.

## NuGet 2.7

* [#3281](http://nuget.codeplex.com/workitem/3281) - Support 'Preinstalled Package' functionality outside of VS templates
* Show License Url when listing packages and verbosity is detailed. 
* [#1956](http://nuget.codeplex.com/workitem/1956) - Add developmentDependency attribute to packages.config and use it in pack command to only include runtime packages
* [#3386](https://nuget.codeplex.com/workitem/3386) - Group-Policy compatible feature for providing NuGet defaults including the ability to override nuget.org
* Add Package Restore VS package, which restores NuGet packages when user builds a solution/project inside VS.
* [#2276](https://nuget.codeplex.com/workitem/2276) Support XNA for Windows Phone projects.
* Add restore command to nuget.exe
* [#2565](https://nuget.codeplex.com/workitem/2565) Handling of Project Retargeting and Project Upgrading Events
* [#3463](https://nuget.codeplex.com/workitem/3463) Show package restore bar by default, even if package restore mode is not enabled.
* [#3502](https://nuget.codeplex.com/workitem/3463) Add two method overloads for IsPackageInstalledEx() and GetInstalledPackages() to IVsPackageInstallerServices
* Package Restore user consent is now default to Yes.
* Show License friendly names in the dialog.
* Sign assembly in NuGet.Server package.

## NuGet 2.6

* Add XDT support

## NuGet 2.5

* Add support for MonoAndroid, MonoTouch and MonoMac framework identifiers.
* Add to all commands new option -ConfigFile, which enables user to use his/her own config file instead of the default one %AppData%\nuget\nuget.config.
* Add support for UNC and directory path as source for push and delete commands.
* Add a parameter to View.PackageManagerConsole. Using this parameter, PSCmdlets of Package Manager Console can be invoked from anywhere inside VS.
* Reduce overall memory consumption significantly, avoiding OutOfMemory exception in certain circumstances.
* Nuget.exe outputs the http requests it is making when -Verbosity is set to detailed.
* Allow setting assembly References per target framework.
* Add support for multiple repositories for preinstalled packages in project templates.
* Add -AsPath option to nuget.exe Config command.
* Updates tab in Manage Packages dialog now honors the 'allowedVersions' constraints in packages.config.
* Added Update All button in the dialog to allow updating all packages with one click.
* Add the 'minClientVersion' attribute to manifest schema to allow packages to require minimum version of NuGet.
* Add the -minClientVersion argument to the nuget.exe pack command 
* Add support for C++ projects.
* Enable package to import .targets or .props file into project.
* When building a package from a project, when -IncludeReferencedProjects is specified, projects referenced by the project are either added as a dependency of the package, if nuspec file exists, or are added into the package if nuspec file doesn't exist.
* Allow users to overwrite files if there's a file conflict in package and project.
* [#1681](http://nuget.codeplex.com/workitem/1681) - NuGet won't update a dependency package if the installed version already satisfies the constraint of dependency version.
* Support Clear Text Password when storing package source credentials in nuget.cofig files (Use 'ClearTextPassword' instead of 'Password' attribute)
* Add the -StorePasswordInClearText to 'nuget.exe source' command.


## NuGet 2.2

* When a package uninstallation fails to delete all files, we show a message asking users to restart VS.
* The Quick Launch feature.
* In .nuspec, allow specifying an entire directory in the <file> element using this syntax:

```
     <file src="scripts\" target="contents\scripts" />
```

  This will also allow package authors to create empty directory easily.
