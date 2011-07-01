Getting started 101:
====================

1. Put the .targets file in your MSBuildExtensions directory, which is normally %Program Files%\MSBuild

2. Edit the Microsoft.Common.Targets file adding the line
   <Import Project="$(MSBuildExtensionsPath)\nuget.targets" />
   at the end of the file.  This will enable the custom targets in *all* projects (though they are disabled by default)

3. Then, to automatically create a nuget package as part of the build, add this property to your .proj file:

    <CreateNuGetPackage>true</CreateNuGetPackage>

   To automatically pull in all the dependency packages, add this property to your .proj file:

    <SetupNuGetPackages>true</SetupNuGetPackages>
	(By default, the list of feeds is taken from the standard nuget.config file.  See below for customisation options)

4. Save your nuspec file as package.nuspec in your project

5. You're good to go.

Alternatively, you can add the import line into a specific .proj file, and the targets will then only be active for that project.

Detailed behaviour can be customised by setting properties (see below).

In a CI build, the properties can be set when calling msbuild, e.g. msbuild /p:CreateNuGetPackage=true will build as normal with the
steps to create the nuget package inserted into the process.


Customising:
============

 To customise the createpackage step, override the (Before/After)CreateNuGetPackage target

 To customise the publish step, override the  (Before/After)PublishNuGetPackage target

 All existing before/after * targets will work as normal.


Properties:
===========

Detailed control can be achieved by setting the following properties:

 NuSpecFileName
   The name of your nuspec package.  Default is 'package.nuspec'

 NuSpecFilePath
   The full path of your nuspec file (including filename).  Default is your output directory, using 'NuSpecFileName' for the filename.

 PackageDirName
   The name of your local package repository.  Default is 'Packages'

 PackageDir
   The full path of your local package repository.  Default is the directory named PackageDirName, in the solution root directory.
 
 PackageConfigName
   The name of your packages.config file - default is 'packages.config'.

 PackageConfigPath
   The full path to your packages.config file - default is to use PackageConfigName in the root of the project directory.

 CreateNuGetPackage
   Set to true if you want to build a nuget package from the output of your build step.  Default is false.

 CleanNuGetPackages
   Set to true if you want to clean the local package repository when running a clean build.  Default is false.
 
 SetupNuGetPackages
   Set to true if you want to attempt to install packages from the packages.config file at the start of the build.  Default is false.

 PackBaseDir
   The working directory that the pack task should use.  Certain file paths are resolved relative to this directory.

 PublishDir
   The directory to publish to, as calculated by Microsoft.  Default is app.publish. 

 NuGetFeedUrls
   Specify this if you want to use a list of alternative feeds when installing packages before build.
   Comma-seperated list of feed names or urls.


Publishing:
===========

The targets file allows cleaning, downloading and building the packages to be carried out as part of a normal build step.  Publishing requires
specifying a different target when running msbuild, using the /t parameter.  Two targets are provided:

 PublishNuGetPackage:
   the publish target runs the build step afresh, creates the package and attempts to publish it to the nuget feed.  Use this to publish as part of
   your build step.

 PublishNuGetPackageOnly:
   the publishonly target attempts to publish a package to the nuget feed, without re-running the entire build.  Use this to publish where the build
   has already been run, for example if you built from source, then ran tests separately, and want to publish the build.

The following properties are required:

 NuGetPublishUrl:
   The url of the feed to publish the package to 

 NuGetApiKey
   The API key to use for publishing 

 OutputPackagePath
   The full path to the package to publish (only required for the "PublishNuGetPackageOnly" target, as otherwise the build works it out)
