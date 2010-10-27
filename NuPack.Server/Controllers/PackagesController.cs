using System;
using System.Web.Mvc;
using NuGet.Server.Infrastructure;

namespace NuGet.Server.Controllers {
    public class PackagesController : Controller {
        private readonly IPackageStore _fileSystem;
        private readonly IPackageRepository _repository;

        public PackagesController(IPackageStore fileSystem, IPackageRepository repository) {
            _fileSystem = fileSystem;
            _repository = repository;
        }

        public ActionResult Index() {
            return View();
        }

        // ?p=filename
        public ActionResult Download(string p) {
            DateTimeOffset lastModified = _fileSystem.GetLastModified(p);
            return new ConditionalGetResult(lastModified,
                                            () => File(_fileSystem.GetFullPath(p), "application/zip", p));
        }
        
    }
}
