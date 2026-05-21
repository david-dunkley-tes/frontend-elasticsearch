using Elastic.Clients.Elasticsearch;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Infrastructure.Elasticsearch;
using StudentSearch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSingleton<SearchConfiguration>();
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

var app = builder.Build();

app.UseCors("ViteDev");
app.UseMiddleware<DevBearerAuthenticationMiddleware>();

app.MapControllers();

app.Run();
