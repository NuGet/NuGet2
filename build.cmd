msbuild NuPack.sln
msbuild PackageSources\PackageBuild.proj
mstest /testcontainer:NuPack.Test\bin\Debug\NuPack.Test.dll