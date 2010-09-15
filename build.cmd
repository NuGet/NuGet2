msbuild NuPack.sln
msbuild PackageSources\PackageBuild.msbuild
mstest /testcontainer:NuPack.Test\bin\Debug\NuPack.Test.dll