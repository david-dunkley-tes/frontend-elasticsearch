using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IReindexService
{
    Task<ReindexResponse> ReindexAsync();
}
