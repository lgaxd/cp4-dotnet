using System.Collections.Concurrent;
using TarefasApi.Models;

namespace TarefasApi.Repositories;

public class TarefasRepositoryEmMemoria : ITarefasRepository
{
    private readonly ConcurrentDictionary<Guid, Tarefa> _tarefas = new();
    private readonly ILogger<TarefasRepositoryEmMemoria> _registro;

    public TarefasRepositoryEmMemoria(ILogger<TarefasRepositoryEmMemoria> registro)
    {
        _registro = registro;
    }

    public Tarefa Adicionar(Tarefa tarefa)
    {
        _tarefas[tarefa.Id] = tarefa;

        _registro.LogDebug(
            "Tarefa persistida no repositório em memória. {TarefaId} {TotalTarefas}",
            tarefa.Id, _tarefas.Count);

        return tarefa;
    }

    public IEnumerable<Tarefa> ObterTodas() => _tarefas.Values.ToList();

    public Tarefa? ObterPorId(Guid id) => _tarefas.TryGetValue(id, out var tarefa) ? tarefa : null;

    public int Contar() => _tarefas.Count;
}
