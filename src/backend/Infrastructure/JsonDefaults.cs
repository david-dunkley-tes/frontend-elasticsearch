using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentSearch.Api.Infrastructure;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static readonly JsonSerializerOptions WebIgnoreNullsOnWrite = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
