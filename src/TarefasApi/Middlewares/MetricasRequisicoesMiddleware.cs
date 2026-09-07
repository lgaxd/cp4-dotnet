using System.Diagnostics;
using TarefasApi.Observabilidade;

namespace TarefasApi.Middlewares;

public class MetricasRequisicoesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricasRequisicoesMiddleware> _registro;

    public MetricasRequisicoesMiddleware(RequestDelegate next, ILogger<MetricasRequisicoesMiddleware> registro)
    {
        _next = next;
        _registro = registro;
    }

    public async Task InvokeAsync(HttpContext context, MetricasTarefas metricas)
    {
        var cronometro = Stopwatch.StartNew();
        var correlationId = ObterOuCriarCorrelationId(context);

        try
        {
            await _next(context);
        }
        finally
        {
            cronometro.Stop();
            var duracaoMs = cronometro.Elapsed.TotalMilliseconds;

            var rota = context.GetEndpoint()?.DisplayName
                       ?? context.Request.Path.Value
                       ?? "desconhecida";

            metricas.RegistrarRequisicao(
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Response.StatusCode,
                duracaoMs);

            _registro.LogInformation(
                "Requisição finalizada. {HttpMethod} {RequestPath} {QueryString} {StatusCode} {DuracaoMs} {CorrelationId} {Rota}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "(sem query)",
                context.Response.StatusCode,
                Math.Round(duracaoMs, 2),
                correlationId,
                rota);
        }
    }

    private static string ObterOuCriarCorrelationId(HttpContext context)
    {
        const string header = "X-Correlation-Id";

        var correlationId = context.Request.Headers.TryGetValue(header, out var valor) && !string.IsNullOrWhiteSpace(valor)
            ? valor.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[header] = correlationId;
        return correlationId;
    }
}

public static class MetricasRequisicoesMiddlewareExtensions
{
    public static IApplicationBuilder UseMetricasRequisicoes(this IApplicationBuilder app)
        => app.UseMiddleware<MetricasRequisicoesMiddleware>();
}
