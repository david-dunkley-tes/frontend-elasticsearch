using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IStudentIndexSeeder
{
    Task<ReindexResponse> ReindexAsync();
}
