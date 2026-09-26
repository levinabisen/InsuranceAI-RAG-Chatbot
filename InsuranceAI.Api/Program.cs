using InsuranceAI.Api;
using InsuranceAI.Api.Services;
using InsuranceAI.Api.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.Configure<AzureOpenAISettings>(
    builder.Configuration.GetSection("AzureOpenAI"));

builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IPdfService, PdfService>();

builder.Services.AddHttpClient<
    IEmbeddingService,
    EmbeddingService
>();
builder.Services.AddScoped<IChunkService, ChunkService>();
builder.Services.AddSingleton<IQdrantService, QdrantService>();
var app = builder.Build();

// Collection ensure karo app start hote hi
using (var scope = app.Services.CreateScope())
{
    var qdrantService = scope.ServiceProvider.GetRequiredService<IQdrantService>();
    await qdrantService.EnsureCollectionExistsAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
