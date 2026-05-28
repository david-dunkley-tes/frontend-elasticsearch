using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IVersionInfoProvider
{
    VersionInfoResponse GetVersionInfo();
}
