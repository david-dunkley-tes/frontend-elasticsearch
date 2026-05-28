using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class ReindexService(IStudentIndexSeeder studentIndexSeeder) : IReindexService
{
    public Task<ReindexResponse> ReindexAsync()
    {
        return studentIndexSeeder.ReindexAsync();
    }
}
