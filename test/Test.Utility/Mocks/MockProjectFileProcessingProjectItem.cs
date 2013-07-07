namespace NuGet.Test.Mocks
{
    public class MockProjectFileProcessingProjectItem : IProjectFileProcessingProjectItem
    {
        public MockProjectFileProcessingProjectItem(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
        public void SetPropertyValue(string name, string value)
        {
            
        }

        public void RunCustomTool()
        {
            
        }
    }
}