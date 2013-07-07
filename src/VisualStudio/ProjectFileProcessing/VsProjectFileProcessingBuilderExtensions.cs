namespace NuGet.VisualStudio
{
    public static class VsProjectFileProcessingBuilderExtensions
    {
        public static ProjectFileProcessingBuilder WithVsPropertySetter(
            this ProjectFileProcessingBuilder builder,
            string matchPattern, string propertyName, string propertyValue)
        {
            return
                builder.WithProcessor(
                    new VsProjectItemPropertySetter(
                        matchPattern, propertyName, propertyValue));
        }

        public static ProjectFileProcessingBuilder WithVsCustomToolSetter(
            this ProjectFileProcessingBuilder builder,
            string matchPattern, string customTool, string customToolNamespace)
        {
            return
                builder.WithProcessor(
                    new VsProjectItemCustomToolSetter(
                        matchPattern, customTool, customToolNamespace));
        }
    }
}