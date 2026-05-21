using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface ISavedSearchService
{
    Task<IReadOnlyList<SavedSearch>> ListAsync();
    Task<SavedSearch> SaveAsync(SaveSearchRequest request);
    Task<bool> DeleteAsync(string id);
}
