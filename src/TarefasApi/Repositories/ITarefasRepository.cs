using TarefasApi.Models;

namespace TarefasApi.Repositories;

/// <summary>
/// Abstração de persistência das tarefas.
/// É esta interface que os testes unitários "mockam" com Moq.
/// </summary>
public interface ITarefasRepository
{
    /// <summary>Persiste a tarefa e devolve a instância armazenada.</summary>
    Tarefa Adicionar(Tarefa tarefa);

    /// <summary>Devolve todas as tarefas armazenadas.</summary>
    IEnumerable<Tarefa> ObterTodas();

    /// <summary>Devolve uma tarefa pelo Id ou null quando não existir.</summary>
    Tarefa? ObterPorId(Guid id);

    /// <summary>Quantidade de tarefas armazenadas (usado pelo Health Check).</summary>
    int Contar();
}
