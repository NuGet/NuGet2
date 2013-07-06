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
    }
}