using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface ISavedSearchService
{
    Task<IReadOnlyList<SavedSearch>> ListAsync(string ownerSub);
    Task<SavedSearch> SaveAsync(string ownerSub, SaveSearchRequest request);
    Task<bool> DeleteAsync(string ownerSub, string id);
}
