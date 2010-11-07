using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NuGet.Server.DataServices;

namespace NuGet.Server.Infrastructure
{
    public interface IServerPackageRepository : IPackageRepository
    {
        IQueryable<Package> GetPackagesWithDerivedData();
    }
}
