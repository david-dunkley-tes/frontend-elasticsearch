using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IStudentIndexSeeder
{
    Task<ReindexResponse> ReindexAsync();
}
