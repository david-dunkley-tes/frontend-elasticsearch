using System.Reflection;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class VersionInfoProvider(IConfiguration configuration, IHostEnvironment environment) : IVersionInfoProvider
{
    private const string Unknown = "unknown";
    private const string Local = "local";
    private readonly Assembly assembly = typeof(Program).Assembly;

    public VersionInfoResponse GetVersionInfo()
    {
        return new VersionInfoResponse(
            Service: environment.ApplicationName,
            Version: ReadConfigurationValue("APP_VERSION") ?? ReadAssemblyVersion(),
            Commit: ReadConfigurationValue("GIT_COMMIT") ?? Local,
            BuildTime: ReadConfigurationValue("BUILD_TIME") ?? Unknown,
            Environment: environment.EnvironmentName);
    }

    private string ReadAssemblyVersion()
    {
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? Unknown;
    }

    private string? ReadConfigurationValue(string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
