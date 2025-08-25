public static class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration config)
    {
        var requiredSettings = new[]
        {
            "Downstream:BaseUrl",
            "Security:Csp:DefaultSrc"
        };

        foreach (var setting in requiredSettings)
        {
            if (string.IsNullOrWhiteSpace(config[setting]))
                throw new InvalidOperationException($"Required setting '{setting}' is missing");
        }
    }
}