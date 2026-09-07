using Microsoft.Extensions.Diagnostics.HealthChecks;
using TarefasApi.Repositories;

namespace TarefasApi.Observabilidade;

public class RepositorioTarefasHealthCheck : IHealthCheck
{
    private readonly ITarefasRepository _repositorio;
    private readonly ILogger<RepositorioTarefasHealthCheck> _registro;

    public RepositorioTarefasHealthCheck(
        ITarefasRepository repositorio,
        ILogger<RepositorioTarefasHealthCheck> registro)
    {
        _repositorio = repositorio;
        _registro = registro;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var total = _repositorio.Contar();

            var dados = new Dictionary<string, object>
            {
                ["totalTarefasEmMemoria"] = total,
                ["verificadoEm"] = DateTime.UtcNow
            };

            _registro.LogDebug("Health check do repositório executado. {TotalTarefas}", total);

            return Task.FromResult(HealthCheckResult.Healthy(
                "Repositório em memória acessível.", dados));
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "Health check do repositório falhou. {Verificacao}", nameof(RepositorioTarefasHealthCheck));
            return Task.FromResult(HealthCheckResult.Unhealthy("Repositório em memória indisponível.", ex));
        }
    }
}
