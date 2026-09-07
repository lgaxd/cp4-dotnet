using TarefasApi.Models;

namespace TarefasApi.Repositories;

public interface ITarefasRepository
{
    Tarefa Adicionar(Tarefa tarefa);

    IEnumerable<Tarefa> ObterTodas();

    Tarefa? ObterPorId(Guid id);

    int Contar();
}
