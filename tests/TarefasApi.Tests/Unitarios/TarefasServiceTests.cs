using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TarefasApi.Models;
using TarefasApi.Observabilidade;
using TarefasApi.Repositories;
using TarefasApi.Services;

namespace TarefasApi.Tests.Unitarios;

/// <summary>
/// Testes UNITÁRIOS da classe TarefasService.
/// O ITarefasRepository é substituído por um Mock (Moq), portanto nenhum
/// dado real é gravado — testamos apenas a REGRA DE NEGÓCIO do serviço.
/// Todos os testes seguem o padrão AAA (Arrange, Act, Assert).
/// </summary>
public class TarefasServiceTests
{
    private readonly Mock<ITarefasRepository> _repositoryMock;
    private readonly ILogger<TarefasService> _logger;
    private readonly MetricasTarefas _metricas;
    private readonly TarefasService _service;

    public TarefasServiceTests()
    {
        // MockBehavior.Strict: qualquer chamada não configurada no repositório falha o teste.
        _repositoryMock = new Mock<ITarefasRepository>(MockBehavior.Strict);
        _logger = NullLogger<TarefasService>.Instance;
        _metricas = new MetricasTarefas();
        _service = new TarefasService(_repositoryMock.Object, _logger, _metricas);
    }

    // =======================================================================
    // CASO OBRIGATÓRIO 1 — Criação de uma tarefa com SUCESSO
    // =======================================================================

    [Fact(DisplayName = "CriarTarefa: deve criar a tarefa e chamar o repositorio uma unica vez")]
    public void CriarTarefa_QuandoDadosValidos_DeveCriarTarefaEChamarRepositorio()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest
        {
            Titulo = "Estudar Health Checks no .NET",
            Descricao = "Revisar o conteudo da aula de monitoramento",
            Prioridade = PrioridadeTarefa.Alta
        };

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Returns((Tarefa t) => t);   // o mock devolve a mesma tarefa recebida

        // ---------- ACT ----------
        var resultado = _service.CriarTarefa(request);

        // ---------- ASSERT ----------
        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Estudar Health Checks no .NET", resultado.Titulo);
        Assert.Equal("Revisar o conteudo da aula de monitoramento", resultado.Descricao);
        Assert.Equal(PrioridadeTarefa.Alta, resultado.Prioridade);
        Assert.False(resultado.Concluida);

        // O repositório precisa ter sido chamado EXATAMENTE uma vez.
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
        _repositoryMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "CriarTarefa: deve remover espacos em branco das extremidades do titulo")]
    public void CriarTarefa_QuandoTituloTemEspacos_DeveArmazenarTituloSemEspacos()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = "   Comprar cafe   " };
        Tarefa? tarefaEnviadaAoRepositorio = null;

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Callback<Tarefa>(t => tarefaEnviadaAoRepositorio = t)
            .Returns((Tarefa t) => t);

        // ---------- ACT ----------
        var resultado = _service.CriarTarefa(request);

        // ---------- ASSERT ----------
        Assert.Equal("Comprar cafe", resultado.Titulo);
        Assert.NotNull(tarefaEnviadaAoRepositorio);
        Assert.Equal("Comprar cafe", tarefaEnviadaAoRepositorio!.Titulo);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
    }

    [Fact(DisplayName = "CriarTarefa: deve aplicar prioridade Media quando nao informada")]
    public void CriarTarefa_QuandoPrioridadeNaoInformada_DeveUsarPrioridadeMedia()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = "Tarefa sem prioridade explicita" };

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Returns((Tarefa t) => t);

        // ---------- ACT ----------
        var resultado = _service.CriarTarefa(request);

        // ---------- ASSERT ----------
        Assert.Equal(PrioridadeTarefa.Media, resultado.Prioridade);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
    }

    // =======================================================================
    // CASO OBRIGATÓRIO 2 — FALHA DE NEGÓCIO (tarefa sem título lança exceção)
    // =======================================================================

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando o titulo e nulo")]
    public void CriarTarefa_QuandoTituloNulo_DeveLancarArgumentException()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = null };

        // ---------- ACT ----------
        var excecao = Assert.Throws<ArgumentException>(() => _service.CriarTarefa(request));

        // ---------- ASSERT ----------
        Assert.Contains("título da tarefa é obrigatório", excecao.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("request", excecao.ParamName);

        // A regra falhou ANTES de tocar no repositório: ele nunca pode ser chamado.
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
        _repositoryMock.VerifyNoOtherCalls();
    }

    [Theory(DisplayName = "CriarTarefa: deve lancar ArgumentException para titulo vazio ou em branco")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CriarTarefa_QuandoTituloVazioOuEmBranco_DeveLancarArgumentException(string tituloInvalido)
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = tituloInvalido };

        // ---------- ACT ----------
        var excecao = Assert.Throws<ArgumentException>(() => _service.CriarTarefa(request));

        // ---------- ASSERT ----------
        Assert.Equal("request", excecao.ParamName);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentNullException quando a requisicao e nula")]
    public void CriarTarefa_QuandoRequisicaoNula_DeveLancarArgumentNullException()
    {
        // ---------- ARRANGE ----------
        CriarTarefaRequest? request = null;

        // ---------- ACT ----------
        var excecao = Assert.Throws<ArgumentNullException>(() => _service.CriarTarefa(request!));

        // ---------- ASSERT ----------
        Assert.Equal("request", excecao.ParamName);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando a data prevista esta no passado")]
    public void CriarTarefa_QuandoDataPrevistaNoPassado_DeveLancarArgumentException()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest
        {
            Titulo = "Tarefa com data invalida",
            DataConclusaoPrevista = DateTime.UtcNow.AddDays(-5)
        };

        // ---------- ACT ----------
        var excecao = Assert.Throws<ArgumentException>(() => _service.CriarTarefa(request));

        // ---------- ASSERT ----------
        Assert.Contains("não pode estar no passado", excecao.Message, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando a prioridade e invalida")]
    public void CriarTarefa_QuandoPrioridadeInvalida_DeveLancarArgumentException()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest
        {
            Titulo = "Tarefa com prioridade fora do enum",
            Prioridade = (PrioridadeTarefa)99
        };

        // ---------- ACT ----------
        var excecao = Assert.Throws<ArgumentException>(() => _service.CriarTarefa(request));

        // ---------- ASSERT ----------
        Assert.Contains("prioridade informada é inválida", excecao.Message, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    // =======================================================================
    // LISTAGEM — filtros, ordenação e paginação (parâmetros)
    // =======================================================================

    [Fact(DisplayName = "ListarTarefas: deve retornar todas as tarefas devolvidas pelo repositorio mockado")]
    public void ListarTarefas_QuandoSemFiltros_DeveRetornarTodasAsTarefas()
    {
        // ---------- ARRANGE ----------
        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Tarefa A", Prioridade = PrioridadeTarefa.Baixa },
            new() { Titulo = "Tarefa B", Prioridade = PrioridadeTarefa.Alta }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        // ---------- ACT ----------
        var resultado = _service.ListarTarefas(new FiltroTarefasQuery());

        // ---------- ASSERT ----------
        Assert.Equal(2, resultado.TotalItens);
        Assert.Equal(2, resultado.Itens.Count);
        _repositoryMock.Verify(r => r.ObterTodas(), Times.Once);
    }

    [Fact(DisplayName = "ListarTarefas: deve filtrar pelo parametro prioridade")]
    public void ListarTarefas_QuandoFiltraPorPrioridade_DeveRetornarSomenteAsCorrespondentes()
    {
        // ---------- ARRANGE ----------
        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Baixa 1", Prioridade = PrioridadeTarefa.Baixa },
            new() { Titulo = "Alta 1",  Prioridade = PrioridadeTarefa.Alta },
            new() { Titulo = "Alta 2",  Prioridade = PrioridadeTarefa.Alta }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Prioridade = PrioridadeTarefa.Alta };

        // ---------- ACT ----------
        var resultado = _service.ListarTarefas(filtro);

        // ---------- ASSERT ----------
        Assert.Equal(2, resultado.TotalItens);
        Assert.All(resultado.Itens, t => Assert.Equal(PrioridadeTarefa.Alta, t.Prioridade));
    }

    [Fact(DisplayName = "ListarTarefas: deve filtrar pelo parametro busca (titulo e descricao)")]
    public void ListarTarefas_QuandoFiltraPorBusca_DeveRetornarSomenteAsCorrespondentes()
    {
        // ---------- ARRANGE ----------
        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Estudar OpenTelemetry", Descricao = "metricas" },
            new() { Titulo = "Comprar pao",           Descricao = "padaria da esquina" },
            new() { Titulo = "Revisar PR",            Descricao = "estudar o diff com calma" }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Busca = "estudar" };

        // ---------- ACT ----------
        var resultado = _service.ListarTarefas(filtro);

        // ---------- ASSERT ----------
        Assert.Equal(2, resultado.TotalItens);
    }

    [Fact(DisplayName = "ListarTarefas: deve respeitar os parametros de paginacao")]
    public void ListarTarefas_QuandoPaginado_DeveRetornarApenasAPaginaSolicitada()
    {
        // ---------- ARRANGE ----------
        var tarefasFalsas = Enumerable.Range(1, 10)
            .Select(i => new Tarefa { Titulo = $"Tarefa {i:00}", CriadaEm = DateTime.UtcNow.AddMinutes(i) })
            .ToList();

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Pagina = 2, TamanhoPagina = 3, OrdenarPor = "titulo" };

        // ---------- ACT ----------
        var resultado = _service.ListarTarefas(filtro);

        // ---------- ASSERT ----------
        Assert.Equal(10, resultado.TotalItens);
        Assert.Equal(3, resultado.Itens.Count);
        Assert.Equal(4, resultado.TotalPaginas);
        Assert.Equal("Tarefa 04", resultado.Itens[0].Titulo);
    }

    [Fact(DisplayName = "ObterPorId: deve devolver null quando o repositorio nao encontra a tarefa")]
    public void ObterPorId_QuandoNaoExiste_DeveRetornarNull()
    {
        // ---------- ARRANGE ----------
        var idInexistente = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorId(idInexistente)).Returns((Tarefa?)null);

        // ---------- ACT ----------
        var resultado = _service.ObterPorId(idInexistente);

        // ---------- ASSERT ----------
        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.ObterPorId(idInexistente), Times.Once);
    }
}
