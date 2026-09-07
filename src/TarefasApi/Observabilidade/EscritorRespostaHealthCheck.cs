using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TarefasApi.Observabilidade;

public static class EscritorRespostaHealthCheck
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    public static Task EscreverRespostaAsync(HttpContext context, HealthReport relatorio)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = relatorio.Status.ToString(),
            duracaoTotalMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds, 2),
            verificadoEm = DateTime.UtcNow,
            verificacoes = relatorio.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                descricao = e.Value.Description,
                duracaoMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                tags = e.Value.Tags,
                dados = e.Value.Data,
                erro = e.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, Opcoes));
    }
}
