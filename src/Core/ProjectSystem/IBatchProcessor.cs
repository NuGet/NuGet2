using System.Collections.Generic;

namespace NuGet
{
    public interface IBatchProcessor<in T>
    {
        void BeginProcessing(IEnumerable<T> batch);
        void EndProcessing();
    }
}
