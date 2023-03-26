namespace SuperSimpleArchitecture.Fitnet.IntegrationTests.Common.TestEngine.Configuration;

internal static class ConfigurationKeys
{
    private const string ConnectionStringsSection = "ConnectionStrings";
    internal const string PassesConnectionString = $"{ConnectionStringsSection}:Passes";
    internal const string ContractsConnectionString = $"{ConnectionStringsSection}:Contracts";
}