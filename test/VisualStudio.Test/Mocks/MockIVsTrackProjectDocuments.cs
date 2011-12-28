using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio.Test.Mocks
{
    public class MockIVsTrackProjectDocuments : IVsTrackProjectDocuments2
    {
        private int _cookieCounter;
        private readonly Dictionary<int, IVsTrackProjectDocumentsEvents2> _eventSinks;

        public MockIVsTrackProjectDocuments()
        {
            _eventSinks = new Dictionary<int, IVsTrackProjectDocumentsEvents2>();
        }

        public int AdviseTrackProjectDocumentsEvents(IVsTrackProjectDocumentsEvents2 pEventSink, out uint pdwCookie)
        {
            _cookieCounter++;
            _eventSinks[_cookieCounter] = pEventSink;
            pdwCookie = (uint)_cookieCounter;
            return 0;
        }

        public int BeginBatch()
        {
            throw new NotImplementedException();
        }

        public int EndBatch()
        {
            throw new NotImplementedException();
        }

        public int Flush()
        {
            throw new NotImplementedException();
        }

        public int OnAfterAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments)
        {
            throw new NotImplementedException();
        }

        public int OnAfterAddDirectoriesEx(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments)
        {
            throw new NotImplementedException();
        }

        public int OnAfterAddFilesEx(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRenameFile(IVsProject pProject, string pszMkOldName, string pszMkNewName, VSRENAMEFILEFLAGS flags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterSccStatusChanged(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            List<IVsTrackProjectDocumentsEvents2> sinks = _eventSinks.Values.ToList();
            foreach (var eventSink in sinks)
            {
                eventSink.OnAfterSccStatusChanged(1, 1, new[] {pProject}, null, rgpszMkDocuments, rgdwSccStatus);
            }

            return 0;
        }

        public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int OnQueryRenameFile(IVsProject pProject, string pszMkOldName, string pszMkNewName, VSRENAMEFILEFLAGS flags, out int pfRenameCanContinue)
        {
            throw new NotImplementedException();
        }

        public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            throw new NotImplementedException();
        }

        public int UnadviseTrackProjectDocumentsEvents(uint dwCookie)
        {
            _eventSinks.Remove((int) dwCookie);
            return 0;
        }
    }
}
