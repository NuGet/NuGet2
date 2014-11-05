#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
mono lib/NuGet.exe restore
xbuild Build/Build.proj /p:Configuration="Mono Release" /flp:LogFile=msbuild.log /verbosity:detailed /t:GoMono
