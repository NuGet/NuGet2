using System;
using System.Web.Mvc;
using NuGet.Server.Infrastructure;

namespace NuGet.Server.Controllers {
    public class PackagesController : Controller {
        private readonly IPackageStore _fileSystem;
        private readonly IServerPackageRepository _repository;

        public PackagesController(IPackageStore fileSystem, IServerPackageRepository repository) {
            _fileSystem = fileSystem;
            _repository = repository;
        }

        public ActionResult Index() {
            return View();
        }

        public string ReportAbuse(string id, string version) {
            return String.Format("Thank you for reporting {0} {1}", id, version);
        }

        // ?p=filename
        public ActionResult Download(string p) {
            DateTimeOffset lastModified = _fileSystem.GetLastModified(p);
            return new ConditionalGetResult(lastModified,
                                            () => File(_fileSystem.GetFullPath(p), "application/zip", p));
        }
    }
}
