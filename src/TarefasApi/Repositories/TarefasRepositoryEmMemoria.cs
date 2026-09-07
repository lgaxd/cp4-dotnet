using System.Collections.Concurrent;
using TarefasApi.Models;

namespace TarefasApi.Repositories;

/// <summary>
/// Implementação em memória do repositório (sem banco de dados real),
/// registrada como Singleton no container de injeção de dependência.
/// </summary>
public class TarefasRepositoryEmMemoria : ITarefasRepository
{
    private readonly ConcurrentDictionary<Guid, Tarefa> _tarefas = new();
    private readonly ILogger<TarefasRepositoryEmMemoria> _logger;

    public TarefasRepositoryEmMemoria(ILogger<TarefasRepositoryEmMemoria> logger)
    {
        _logger = logger;
    }

    public Tarefa Adicionar(Tarefa tarefa)
    {
        _tarefas[tarefa.Id] = tarefa;

        // Log estruturado: parâmetros nomeados, nunca concatenação de string.
        _logger.LogDebug(
            "Tarefa persistida no repositório em memória. {TarefaId} {TotalTarefas}",
            tarefa.Id, _tarefas.Count);

        return tarefa;
    }

    public IEnumerable<Tarefa> ObterTodas() => _tarefas.Values.ToList();

    public Tarefa? ObterPorId(Guid id) => _tarefas.TryGetValue(id, out var tarefa) ? tarefa : null;

    public int Contar() => _tarefas.Count;
}
