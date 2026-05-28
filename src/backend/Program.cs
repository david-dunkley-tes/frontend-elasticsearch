using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Health;
using StudentSearch.Api.Infrastructure;
using StudentSearch.Api.Infrastructure.Anthropic;
using StudentSearch.Api.Infrastructure.Elasticsearch;
using StudentSearch.Api.Infrastructure.Voyage;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequireAuthenticatedApiUserFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Search API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Development bearer token. Enter only the token value; Swagger UI adds the Bearer prefix.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "dev-token"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSingleton<SearchConfiguration>();
builder.Services.AddSingleton<RagConfiguration>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<SearchConfiguration>();
    return new ElasticsearchClient(new Uri(configuration.ElasticsearchUrl));
});
builder.Services.AddScoped<IStudentSearchService, StudentSearchService>();
builder.Services.AddScoped<IAuthorizationScopeResolver, AuthorizationScopeResolver>();
builder.Services.AddScoped<IReindexService, ReindexService>();
builder.Services.AddSingleton<ISavedSearchService, SavedSearchService>();
builder.Services.AddScoped<IStudentSearchIndex, ElasticsearchStudentSearchIndex>();
builder.Services.AddScoped<IStudentIndexSeeder, ElasticsearchStudentIndexSeeder>();
builder.Services.AddSingleton<IElasticsearchGateway, ElasticsearchGateway>();
builder.Services.AddSingleton<IVersionInfoProvider, VersionInfoProvider>();
builder.Services.AddScoped<IStudentKnnRetriever, ElasticsearchStudentKnnRetriever>();
builder.Services.AddSingleton<INarrativeRedactor, NarrativeRedactor>();
builder.Services.AddScoped<ISafeguardingService, SafeguardingService>();
builder.Services.AddHttpClient<IEmbeddingClient, VoyageEmbeddingClient>(client =>
{
    client.BaseAddress = new Uri("https://api.voyageai.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<IAnthropicClient, AnthropicMessagesClient>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var swaggerDevBearerToken = $"Bearer {DevAccessTokenFactory.Encode(DevAccessTokenFactory.SwaggerUser)}";

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Search API v1");
        options.EnableTryItOutByDefault();
        options.UseRequestInterceptor($"function(request) {{ request.headers = request.headers || {{}}; request.headers.Authorization = request.headers.Authorization || `{swaggerDevBearerToken}`; return request; }}");
    });
}

app.UseCors("ViteDev");
app.UseMiddleware<DevBearerAuthenticationMiddleware>();

app.MapControllers();
app.MapGet("/version", (IVersionInfoProvider provider) => provider.GetVersionInfo())
    .WithName("GetVersionInfo")
    .WithTags("Version");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

app.Run();
