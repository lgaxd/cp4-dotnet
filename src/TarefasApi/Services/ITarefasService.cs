using TarefasApi.Models;

namespace TarefasApi.Services;

/// <summary>Contrato da camada de serviço (regras de negócio das tarefas).</summary>
public interface ITarefasService
{
    /// <summary>
    /// Cria uma nova tarefa aplicando as regras de negócio.
    /// </summary>
    /// <exception cref="ArgumentNullException">Quando a requisição é nula.</exception>
    /// <exception cref="ArgumentException">Quando o título está vazio/em branco ou a data prevista é passada.</exception>
    Tarefa CriarTarefa(CriarTarefaRequest request);

    /// <summary>Lista as tarefas aplicando filtros, ordenação e paginação.</summary>
    ResultadoPaginado<Tarefa> ListarTarefas(FiltroTarefasQuery filtro);

    /// <summary>Obtém uma tarefa pelo Id (null quando não encontrada).</summary>
    Tarefa? ObterPorId(Guid id);
}
