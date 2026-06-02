using ActionsShowcase.Features.Random;
using ActionsShowcase.Features.Random.Services;
using ActionsShowcase.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<SecretsOptions>(
    builder.Configuration.GetSection(SecretsOptions.SectionName));
builder.Services.Configure<RandomOptions>(
    builder.Configuration.GetSection(RandomOptions.SectionName));

builder.Services.AddScoped<IRandomService, RandomService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
