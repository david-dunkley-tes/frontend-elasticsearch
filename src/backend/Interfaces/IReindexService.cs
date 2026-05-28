using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IReindexService
{
    Task<ReindexResponse> ReindexAsync();
}
