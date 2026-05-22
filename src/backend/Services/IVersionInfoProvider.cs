using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IVersionInfoProvider
{
    VersionInfoResponse GetVersionInfo();
}
