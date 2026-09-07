using Microsoft.Extensions.Diagnostics.HealthChecks;
using TarefasApi.Repositories;

namespace TarefasApi.Observabilidade;

/// <summary>
/// Health Check customizado (nativo do .NET) que verifica se o repositório
/// em memória está acessível e responde.
/// </summary>
public class RepositorioTarefasHealthCheck : IHealthCheck
{
    private readonly ITarefasRepository _repository;
    private readonly ILogger<RepositorioTarefasHealthCheck> _logger;

    public RepositorioTarefasHealthCheck(
        ITarefasRepository repository,
        ILogger<RepositorioTarefasHealthCheck> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var total = _repository.Contar();

            var dados = new Dictionary<string, object>
            {
                ["totalTarefasEmMemoria"] = total,
                ["verificadoEm"] = DateTime.UtcNow
            };

            _logger.LogDebug("Health check do repositório executado. {TotalTarefas}", total);

            return Task.FromResult(HealthCheckResult.Healthy(
                "Repositório em memória acessível.", dados));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check do repositório falhou. {Verificacao}", nameof(RepositorioTarefasHealthCheck));
            return Task.FromResult(HealthCheckResult.Unhealthy("Repositório em memória indisponível.", ex));
        }
    }
}
