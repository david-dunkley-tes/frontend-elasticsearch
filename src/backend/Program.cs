using Elastic.Clients.Elasticsearch;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSingleton<SearchConfiguration>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<SearchConfiguration>();
    return new ElasticsearchClient(new Uri(configuration.ElasticsearchUrl));
});
builder.Services.AddSingleton<IElasticsearchGateway, ElasticsearchGateway>();
builder.Services.AddScoped<IStudentSearchService, StudentSearchService>();
builder.Services.AddScoped<IReindexService, ReindexService>();

var app = builder.Build();

app.UseCors("ViteDev");

app.MapControllers();

app.Run();
