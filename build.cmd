msbuild NuPack.sln
msbuild Build\PackageBuild.proj
msbuild Build\Build.proj
mstest /testcontainer:NuPack.Test\bin\Debug\NuPack.Test.dll