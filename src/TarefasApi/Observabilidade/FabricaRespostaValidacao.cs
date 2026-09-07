using Microsoft.AspNetCore.Mvc;

namespace TarefasApi.Observabilidade;

/// <summary>
/// Fábrica da resposta 400 Bad Request gerada automaticamente pelo
/// <c>[ApiController]</c> quando o ModelState é inválido.
///
/// O filtro automático do ASP.NET Core roda ANTES do corpo da action, então sem
/// esta fábrica nenhum log seria emitido para payloads inválidos. Aqui garantimos
/// que todo erro de validação gere um LOG ESTRUTURADO e incremente a MÉTRICA de
/// rejeição, mantendo o mesmo corpo de resposta (ValidationProblemDetails).
/// </summary>
public static class FabricaRespostaValidacao
{
    public static IActionResult Criar(ActionContext contexto)
    {
        var servicos = contexto.HttpContext.RequestServices;
        var logger = servicos.GetRequiredService<ILoggerFactory>().CreateLogger("TarefasApi.Validacao");
        var metricas = servicos.GetRequiredService<MetricasTarefas>();

        var camposInvalidos = contexto.ModelState
            .Where(par => par.Value?.Errors.Count > 0)
            .Select(par => par.Key)
            .ToArray();

        var mensagens = contexto.ModelState
            .SelectMany(par => par.Value!.Errors.Select(erro => erro.ErrorMessage))
            .Where(mensagem => !string.IsNullOrWhiteSpace(mensagem))
            .ToArray();

        // Log estruturado (parâmetros nomeados) do caso de erro.
        logger.LogWarning(
            "Requisição rejeitada na validação do modelo. {HttpMethod} {RequestPath} {QuantidadeErros} {CamposInvalidos} {MensagensErro} {StatusCode}",
            contexto.HttpContext.Request.Method,
            contexto.HttpContext.Request.Path.Value,
            contexto.ModelState.ErrorCount,
            string.Join(", ", camposInvalidos),
            string.Join(" | ", mensagens),
            StatusCodes.Status400BadRequest);

        foreach (var campo in camposInvalidos)
        {
            metricas.RegistrarTarefaRejeitada($"validacao:{campo.ToLowerInvariant()}");
        }

        var problema = new ValidationProblemDetails(contexto.ModelState)
        {
            Title = "Um ou mais erros de validação ocorreram.",
            Status = StatusCodes.Status400BadRequest,
            Instance = contexto.HttpContext.Request.Path
        };

        problema.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problema)
        {
            ContentTypes = { "application/problem+json" }
        };
    }
}
