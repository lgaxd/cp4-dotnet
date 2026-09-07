using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TarefasApi.Middlewares;
using TarefasApi.Observabilidade;
using TarefasApi.Repositories;
using TarefasApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opcoes =>
{
    opcoes.IncludeScopes = true;
    opcoes.SingleLine = false;
    opcoes.TimestampFormat = "[HH:mm:ss] ";
});
builder.Logging.AddDebug();
builder.Services.AddSingleton<ITarefasRepository, TarefasRepositoryEmMemoria>();
builder.Services.AddScoped<ITarefasService, TarefasService>();
builder.Services.AddSingleton<MetricasTarefas>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(opcoes =>
{
    opcoes.InvalidModelStateResponseFactory = FabricaRespostaValidacao.Criar;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tarefas API - CP4 .NET (Monitoramento e Qualidade)",
        Version = "v1",
        Description = "API de gerenciamento de tarefas (To-Do) com Health Checks, "
                    + "logging estruturado, metricas/tracing e testes automatizados."
    });

    var caminhoXml = Path.Combine(AppContext.BaseDirectory, "TarefasApi.xml");
    if (File.Exists(caminhoXml))
    {
        opcoes.IncludeXmlComments(caminhoXml);
    }
});

builder.Services
    .AddHealthChecks()
    .AddCheck<RepositorioTarefasHealthCheck>(
        name: "repositorio-tarefas",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "infra" })
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("API no ar."),
        tags: new[] { "live" });

var otel = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(recurso => recurso.AddService(
        serviceName: "TarefasApi",
        serviceVersion: "1.0.0"));

otel.WithMetrics(metricas =>
{
    metricas.AddAspNetCoreInstrumentation();
    metricas.AddHttpClientInstrumentation();
    metricas.AddMeter(MetricasTarefas.NomeMeter);

    if (builder.Environment.IsDevelopment())
    {
        metricas.AddConsoleExporter((_, leitorOpcoes) =>
        {
            leitorOpcoes.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 15000;
        });
    }
});

otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();

    if (builder.Environment.IsDevelopment())
    {
        tracing.AddConsoleExporter();
    }
});

var app = builder.Build();

app.UseMetricasRequisicoes();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Tarefas API v1");
        opcoes.DocumentTitle = "Tarefas API - CP4 .NET";
    });
}

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = EscritorRespostaHealthCheck.EscreverRespostaAsync
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains("ready"),
    ResponseWriter = EscritorRespostaHealthCheck.EscreverRespostaAsync
});

app.MapGet("/metrics", (MetricasTarefas metricas) =>
{
    var snapshot = new
    {
        coletadoEm = DateTime.UtcNow,
        totalRequisicoes = metricas.TotalRequisicoes,
        tempoMedioRespostaMs = metricas.TempoMedioRespostaMs,
        tempoMinimoRespostaMs = metricas.TempoMinimoRespostaMs,
        tempoMaximoRespostaMs = metricas.TempoMaximoRespostaMs,
        totalTarefasCriadas = metricas.TotalTarefasCriadas,
        totalTarefasRejeitadas = metricas.TotalTarefasRejeitadas,
        requisicoesPorStatusCode = metricas.RequisicoesPorStatusCode,
        requisicoesPorRota = metricas.RequisicoesPorRota,
        motivosDeRejeicao = metricas.MotivosDeRejeicao
    };

    return Results.Json(snapshot, new JsonSerializerOptions { WriteIndented = true });
})
.WithName("ObterMetricas")
.WithTags("Monitoramento");

app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.Run();
public partial class Program { }
