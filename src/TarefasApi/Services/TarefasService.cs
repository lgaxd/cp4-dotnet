using TarefasApi.Models;
using TarefasApi.Observabilidade;
using TarefasApi.Repositories;

namespace TarefasApi.Services;

public class TarefasService : ITarefasService
{
    private readonly ITarefasRepository _repositorio;
    private readonly ILogger<TarefasService> _registro;
    private readonly MetricasTarefas _metricas;

    public TarefasService(
        ITarefasRepository repositorio,
        ILogger<TarefasService> registro,
        MetricasTarefas metricas)
    {
        _repositorio = repositorio;
        _registro = registro;
        _metricas = metricas;
    }

    public Tarefa CriarTarefa(CriarTarefaRequest request)
    {
        if (request is null)
        {
            _registro.LogError("Tentativa de criar tarefa com requisição nula. {Operacao}", nameof(CriarTarefa));
            _metricas.RegistrarTarefaRejeitada("requisicao_nula");
            throw new ArgumentNullException(nameof(request), "A requisição de criação de tarefa não pode ser nula.");
        }

        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            _registro.LogWarning(
                "Falha de validação ao criar tarefa: título não informado. {Operacao} {MotivoRejeicao}",
                nameof(CriarTarefa), "titulo_obrigatorio");

            _metricas.RegistrarTarefaRejeitada("titulo_obrigatorio");
            throw new ArgumentException("O título da tarefa é obrigatório.", nameof(request));
        }

        if (!Enum.IsDefined(typeof(PrioridadeTarefa), request.Prioridade))
        {
            _registro.LogWarning(
                "Falha de validação ao criar tarefa: prioridade inválida. {PrioridadeRecebida} {MotivoRejeicao}",
                request.Prioridade, "prioridade_invalida");

            _metricas.RegistrarTarefaRejeitada("prioridade_invalida");
            throw new ArgumentException("A prioridade informada é inválida.", nameof(request));
        }

        if (request.DataConclusaoPrevista.HasValue &&
            request.DataConclusaoPrevista.Value.Date < DateTime.UtcNow.Date)
        {
            _registro.LogWarning(
                "Falha de validação ao criar tarefa: data prevista no passado. {DataConclusaoPrevista} {MotivoRejeicao}",
                request.DataConclusaoPrevista, "data_no_passado");

            _metricas.RegistrarTarefaRejeitada("data_no_passado");
            throw new ArgumentException("A data prevista de conclusão não pode estar no passado.", nameof(request));
        }

        var tarefa = new Tarefa
        {
            Titulo = request.Titulo.Trim(),
            Descricao = request.Descricao?.Trim(),
            Prioridade = request.Prioridade,
            DataConclusaoPrevista = request.DataConclusaoPrevista,
            Concluida = false
        };

        var tarefaCriada = _repositorio.Adicionar(tarefa);

        _registro.LogInformation(
            "Nova tarefa criada: {NomeTarefa} {TarefaId} {Prioridade} {CriadaEm}",
            tarefaCriada.Titulo, tarefaCriada.Id, tarefaCriada.Prioridade, tarefaCriada.CriadaEm);

        _metricas.RegistrarTarefaCriada(tarefaCriada.Prioridade);

        return tarefaCriada;
    }

    public ResultadoPaginado<Tarefa> ListarTarefas(FiltroTarefasQuery filtro)
    {
        filtro ??= new FiltroTarefasQuery();

        var consulta = _repositorio.ObterTodas() ?? Enumerable.Empty<Tarefa>();

        if (filtro.Concluida.HasValue)
        {
            consulta = consulta.Where(t => t.Concluida == filtro.Concluida.Value);
        }

        if (filtro.Prioridade.HasValue)
        {
            consulta = consulta.Where(t => t.Prioridade == filtro.Prioridade.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(t =>
                t.Titulo.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (t.Descricao is not null && t.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase)));
        }

        var descendente = string.Equals(filtro.Ordem, "desc", StringComparison.OrdinalIgnoreCase);

        consulta = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "titulo" => descendente
                ? consulta.OrderByDescending(t => t.Titulo)
                : consulta.OrderBy(t => t.Titulo),
            "prioridade" => descendente
                ? consulta.OrderByDescending(t => t.Prioridade)
                : consulta.OrderBy(t => t.Prioridade),
            _ => descendente
                ? consulta.OrderByDescending(t => t.CriadaEm)
                : consulta.OrderBy(t => t.CriadaEm)
        };

        var materializado = consulta.ToList();
        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamanho = filtro.TamanhoPagina < 1 ? 50 : filtro.TamanhoPagina;

        var itens = materializado
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        _registro.LogInformation(
            "Listagem de tarefas executada. {TotalEncontrado} {QuantidadeRetornada} {Pagina} {TamanhoPagina} {FiltroBusca} {FiltroPrioridade} {FiltroConcluida}",
            materializado.Count, itens.Count, pagina, tamanho,
            filtro.Busca ?? "(nenhum)", filtro.Prioridade, filtro.Concluida);

        return new ResultadoPaginado<Tarefa>
        {
            Itens = itens,
            TotalItens = materializado.Count,
            Pagina = pagina,
            TamanhoPagina = tamanho
        };
    }

    public Tarefa? ObterPorId(Guid id)
    {
        var tarefa = _repositorio.ObterPorId(id);

        if (tarefa is null)
        {
            _registro.LogWarning("Tarefa não encontrada. {TarefaId}", id);
        }
        else
        {
            _registro.LogInformation("Tarefa localizada. {TarefaId} {NomeTarefa}", tarefa.Id, tarefa.Titulo);
        }

        return tarefa;
    }
}
