using TarefasApi.Models;
using TarefasApi.Observabilidade;
using TarefasApi.Repositories;

namespace TarefasApi.Services;

/// <summary>
/// Camada de Serviço: concentra as regras de negócio e depende do
/// <see cref="ITarefasRepository"/> por injeção de dependência (mockável nos testes).
/// </summary>
public class TarefasService : ITarefasService
{
    private readonly ITarefasRepository _repository;
    private readonly ILogger<TarefasService> _logger;
    private readonly MetricasTarefas _metricas;

    public TarefasService(
        ITarefasRepository repository,
        ILogger<TarefasService> logger,
        MetricasTarefas metricas)
    {
        _repository = repository;
        _logger = logger;
        _metricas = metricas;
    }

    public Tarefa CriarTarefa(CriarTarefaRequest request)
    {
        if (request is null)
        {
            // Log estruturado de erro.
            _logger.LogError("Tentativa de criar tarefa com requisição nula. {Operacao}", nameof(CriarTarefa));
            _metricas.RegistrarTarefaRejeitada("requisicao_nula");
            throw new ArgumentNullException(nameof(request), "A requisição de criação de tarefa não pode ser nula.");
        }

        // REGRA DE NEGÓCIO 1: título é obrigatório.
        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            _logger.LogWarning(
                "Falha de validação ao criar tarefa: título não informado. {Operacao} {MotivoRejeicao}",
                nameof(CriarTarefa), "titulo_obrigatorio");

            _metricas.RegistrarTarefaRejeitada("titulo_obrigatorio");
            throw new ArgumentException("O título da tarefa é obrigatório.", nameof(request));
        }

        // REGRA DE NEGÓCIO 2: prioridade precisa ser um valor válido do enum.
        if (!Enum.IsDefined(typeof(PrioridadeTarefa), request.Prioridade))
        {
            _logger.LogWarning(
                "Falha de validação ao criar tarefa: prioridade inválida. {PrioridadeRecebida} {MotivoRejeicao}",
                request.Prioridade, "prioridade_invalida");

            _metricas.RegistrarTarefaRejeitada("prioridade_invalida");
            throw new ArgumentException("A prioridade informada é inválida.", nameof(request));
        }

        // REGRA DE NEGÓCIO 3: data prevista não pode estar no passado.
        if (request.DataConclusaoPrevista.HasValue &&
            request.DataConclusaoPrevista.Value.Date < DateTime.UtcNow.Date)
        {
            _logger.LogWarning(
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

        var tarefaCriada = _repository.Adicionar(tarefa);

        // LOG ESTRUTURADO exigido pelo enunciado: parâmetros nomeados, sem concatenar string.
        _logger.LogInformation(
            "Nova tarefa criada: {NomeTarefa} {TarefaId} {Prioridade} {CriadaEm}",
            tarefaCriada.Titulo, tarefaCriada.Id, tarefaCriada.Prioridade, tarefaCriada.CriadaEm);

        _metricas.RegistrarTarefaCriada(tarefaCriada.Prioridade);

        return tarefaCriada;
    }

    public ResultadoPaginado<Tarefa> ListarTarefas(FiltroTarefasQuery filtro)
    {
        filtro ??= new FiltroTarefasQuery();

        var consulta = _repository.ObterTodas() ?? Enumerable.Empty<Tarefa>();

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

        _logger.LogInformation(
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
        var tarefa = _repository.ObterPorId(id);

        if (tarefa is null)
        {
            _logger.LogWarning("Tarefa não encontrada. {TarefaId}", id);
        }
        else
        {
            _logger.LogInformation("Tarefa localizada. {TarefaId} {NomeTarefa}", tarefa.Id, tarefa.Titulo);
        }

        return tarefa;
    }
}
