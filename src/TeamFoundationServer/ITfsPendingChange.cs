namespace NuGet.TeamFoundationServer
{
    public interface ITfsPendingChange
    {
        bool IsAdd { get; }
        bool IsDelete { get; }
        bool IsEdit { get; }
        string LocalItem { get; }
    }
}