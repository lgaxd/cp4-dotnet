using TarefasApi.Models;

namespace TarefasApi.Services;

public interface ITarefasService
{
    Tarefa CriarTarefa(CriarTarefaRequest request);

    ResultadoPaginado<Tarefa> ListarTarefas(FiltroTarefasQuery filtro);

    Tarefa? ObterPorId(Guid id);
}
