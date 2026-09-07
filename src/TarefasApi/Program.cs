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

// ---------------------------------------------------------------------------
// 1) LOGGING ESTRUTURADO (ILogger)
// ---------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opcoes =>
{
    opcoes.IncludeScopes = true;
    opcoes.SingleLine = false;
    opcoes.TimestampFormat = "[HH:mm:ss] ";
});
builder.Logging.AddDebug();

// ---------------------------------------------------------------------------
// 2) INJEÇÃO DE DEPENDÊNCIA — separação Service / Repository
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<ITarefasRepository, TarefasRepositoryEmMemoria>();
builder.Services.AddScoped<ITarefasService, TarefasService>();
builder.Services.AddSingleton<MetricasTarefas>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
    {
        // Serializa o enum PrioridadeTarefa como texto ("Alta") em vez de número.
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Todo 400 automatico do [ApiController] passa a gerar log estruturado + metrica.
builder.Services.Configure<ApiBehaviorOptions>(opcoes =>
{
    opcoes.InvalidModelStateResponseFactory = FabricaRespostaValidacao.Criar;
});

// ---------------------------------------------------------------------------
// 3) SWAGGER / OPENAPI
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// 4) HEALTH CHECKS (nativo do .NET)
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// 5) OPENTELEMETRY — METRICAS E TRACING
// ---------------------------------------------------------------------------
var otel = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(recurso => recurso.AddService(
        serviceName: "TarefasApi",
        serviceVersion: "1.0.0"));

otel.WithMetrics(metricas =>
{
    metricas.AddAspNetCoreInstrumentation();       // metricas HTTP nativas do ASP.NET Core
    metricas.AddHttpClientInstrumentation();
    metricas.AddMeter(MetricasTarefas.NomeMeter);  // metricas customizadas da aplicacao

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

// ---------------------------------------------------------------------------
// 6) PIPELINE HTTP
// ---------------------------------------------------------------------------

// Middleware customizado: mede tempo de resposta e conta as requisicoes.
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

// ---------------------------------------------------------------------------
// 7) ENDPOINTS DE MONITORAMENTO
// ---------------------------------------------------------------------------

// /health -> status geral ("Healthy") em JSON detalhado.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = EscritorRespostaHealthCheck.EscreverRespostaAsync
});

// /health/live -> a aplicacao esta de pe? (texto puro: Healthy)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains("live")
});

// /health/ready -> as dependencias estao prontas?
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains("ready"),
    ResponseWriter = EscritorRespostaHealthCheck.EscreverRespostaAsync
});

// /metrics -> snapshot legivel das metricas coletadas pelo middleware.
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

// Raiz -> redireciona para o Swagger em desenvolvimento.
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.Run();

/// <summary>
/// Tornar a classe Program publica e parcial permite que o
/// WebApplicationFactory&lt;Program&gt; a referencie nos testes de integracao.
/// </summary>
public partial class Program { }
