using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class ReindexService : IReindexService
{
    private readonly IStudentIndexSeeder _studentIndexSeeder;

    public ReindexService(IStudentIndexSeeder studentIndexSeeder)
    {
        _studentIndexSeeder = studentIndexSeeder;
    }

    public Task<ReindexResponse> ReindexAsync()
    {
        return _studentIndexSeeder.ReindexAsync();
    }
}
