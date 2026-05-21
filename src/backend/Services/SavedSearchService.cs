using System.Text.Json;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class SavedSearchService(SearchConfiguration configuration) : ISavedSearchService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public async Task<IReadOnlyList<SavedSearch>> ListAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var savedSearches = await ReadAllUnsafeAsync();
            return savedSearches.OrderByDescending(search => search.CreatedAt).ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<SavedSearch> SaveAsync(SaveSearchRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Saved search name is required.", nameof(request));
        }

        var savedSearch = new SavedSearch(
            Guid.NewGuid().ToString("N"),
            name,
            request.Query.Trim(),
            NormalizeFilters(request.Filters),
            request.Sort.Trim(),
            Math.Clamp(request.PageSize, 1, 100),
            DateTimeOffset.UtcNow);

        await _fileLock.WaitAsync();
        try
        {
            var savedSearches = await ReadAllUnsafeAsync();
            savedSearches.Add(savedSearch);
            await WriteAllUnsafeAsync(savedSearches);
            return savedSearch;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await _fileLock.WaitAsync();
        try
        {
            var savedSearches = await ReadAllUnsafeAsync();
            var removed = savedSearches.RemoveAll(search => string.Equals(search.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                await WriteAllUnsafeAsync(savedSearches);
            }

            return removed;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<SavedSearch>> ReadAllUnsafeAsync()
    {
        if (!File.Exists(configuration.SavedSearchesPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(configuration.SavedSearchesPath);
        return await JsonSerializer.DeserializeAsync<List<SavedSearch>>(stream, SerializerOptions) ?? [];
    }

    private async Task WriteAllUnsafeAsync(List<SavedSearch> savedSearches)
    {
        var directory = Path.GetDirectoryName(configuration.SavedSearchesPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(configuration.SavedSearchesPath);
        await JsonSerializer.SerializeAsync(stream, savedSearches, SerializerOptions);
    }

    private static Dictionary<string, List<string>> NormalizeFilters(Dictionary<string, List<string>> filters)
    {
        return filters
            .Where(filter => !string.IsNullOrWhiteSpace(filter.Key))
            .Select(filter => new
            {
                Key = filter.Key.Trim(),
                Values = filter.Value
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(filter => filter.Values.Count > 0)
            .ToDictionary(filter => filter.Key, filter => filter.Values, StringComparer.OrdinalIgnoreCase);
    }
}
