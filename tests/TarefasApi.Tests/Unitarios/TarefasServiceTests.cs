using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TarefasApi.Models;
using TarefasApi.Observabilidade;
using TarefasApi.Repositories;
using TarefasApi.Services;

namespace TarefasApi.Tests.Unitarios;

public class TarefasServiceTests
{
    private readonly Mock<ITarefasRepository> _repositoryMock;
    private readonly ILogger<TarefasService> _registro;
    private readonly MetricasTarefas _metricas;
    private readonly TarefasService _servico;

    public TarefasServiceTests()
    {
        _repositoryMock = new Mock<ITarefasRepository>(MockBehavior.Strict);
        _registro = NullLogger<TarefasService>.Instance;
        _metricas = new MetricasTarefas();
        _servico = new TarefasService(_repositoryMock.Object, _registro, _metricas);
    }

    [Fact(DisplayName = "CriarTarefa: deve criar a tarefa e chamar o repositorio uma unica vez")]
    public void CriarTarefa_QuandoDadosValidos_DeveCriarTarefaEChamarRepositorio()
    {
        var request = new CriarTarefaRequest
        {
            Titulo = "Estudar Health Checks no .NET",
            Descricao = "Revisar o conteudo da aula de monitoramento",
            Prioridade = PrioridadeTarefa.Alta
        };

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Returns((Tarefa t) => t);

        var resultado = _servico.CriarTarefa(request);

        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Estudar Health Checks no .NET", resultado.Titulo);
        Assert.Equal("Revisar o conteudo da aula de monitoramento", resultado.Descricao);
        Assert.Equal(PrioridadeTarefa.Alta, resultado.Prioridade);
        Assert.False(resultado.Concluida);

        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
        _repositoryMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "CriarTarefa: deve remover espacos em branco das extremidades do titulo")]
    public void CriarTarefa_QuandoTituloTemEspacos_DeveArmazenarTituloSemEspacos()
    {
        var request = new CriarTarefaRequest { Titulo = "   Comprar cafe   " };
        Tarefa? tarefaEnviadaAoRepositorio = null;

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Callback<Tarefa>(t => tarefaEnviadaAoRepositorio = t)
            .Returns((Tarefa t) => t);

        var resultado = _servico.CriarTarefa(request);

        Assert.Equal("Comprar cafe", resultado.Titulo);
        Assert.NotNull(tarefaEnviadaAoRepositorio);
        Assert.Equal("Comprar cafe", tarefaEnviadaAoRepositorio!.Titulo);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
    }

    [Fact(DisplayName = "CriarTarefa: deve aplicar prioridade Media quando nao informada")]
    public void CriarTarefa_QuandoPrioridadeNaoInformada_DeveUsarPrioridadeMedia()
    {
        var request = new CriarTarefaRequest { Titulo = "Tarefa sem prioridade explicita" };

        _repositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Tarefa>()))
            .Returns((Tarefa t) => t);

        var resultado = _servico.CriarTarefa(request);

        Assert.Equal(PrioridadeTarefa.Media, resultado.Prioridade);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Once);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando o titulo e nulo")]
    public void CriarTarefa_QuandoTituloNulo_DeveLancarArgumentException()
    {
        var request = new CriarTarefaRequest { Titulo = null };

        var excecao = Assert.Throws<ArgumentException>(() => _servico.CriarTarefa(request));

        Assert.Contains("título da tarefa é obrigatório", excecao.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("request", excecao.ParamName);

        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
        _repositoryMock.VerifyNoOtherCalls();
    }

    [Theory(DisplayName = "CriarTarefa: deve lancar ArgumentException para titulo vazio ou em branco")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CriarTarefa_QuandoTituloVazioOuEmBranco_DeveLancarArgumentException(string tituloInvalido)
    {
        var request = new CriarTarefaRequest { Titulo = tituloInvalido };

        var excecao = Assert.Throws<ArgumentException>(() => _servico.CriarTarefa(request));

        Assert.Equal("request", excecao.ParamName);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentNullException quando a requisicao e nula")]
    public void CriarTarefa_QuandoRequisicaoNula_DeveLancarArgumentNullException()
    {
        CriarTarefaRequest? request = null;

        var excecao = Assert.Throws<ArgumentNullException>(() => _servico.CriarTarefa(request!));

        Assert.Equal("request", excecao.ParamName);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando a data prevista esta no passado")]
    public void CriarTarefa_QuandoDataPrevistaNoPassado_DeveLancarArgumentException()
    {
        var request = new CriarTarefaRequest
        {
            Titulo = "Tarefa com data invalida",
            DataConclusaoPrevista = DateTime.UtcNow.AddDays(-5)
        };
        var excecao = Assert.Throws<ArgumentException>(() => _servico.CriarTarefa(request));

        Assert.Contains("não pode estar no passado", excecao.Message, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }

    [Fact(DisplayName = "CriarTarefa: deve lancar ArgumentException quando a prioridade e invalida")]
    public void CriarTarefa_QuandoPrioridadeInvalida_DeveLancarArgumentException()
    {
        var request = new CriarTarefaRequest
        {
            Titulo = "Tarefa com prioridade fora do enum",
            Prioridade = (PrioridadeTarefa)99
        };

        var excecao = Assert.Throws<ArgumentException>(() => _servico.CriarTarefa(request));

        Assert.Contains("prioridade informada é inválida", excecao.Message, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.Adicionar(It.IsAny<Tarefa>()), Times.Never);
    }



    [Fact(DisplayName = "ListarTarefas: deve retornar todas as tarefas devolvidas pelo repositorio mockado")]
    public void ListarTarefas_QuandoSemFiltros_DeveRetornarTodasAsTarefas()
    {

        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Tarefa A", Prioridade = PrioridadeTarefa.Baixa },
            new() { Titulo = "Tarefa B", Prioridade = PrioridadeTarefa.Alta }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);


        var resultado = _servico.ListarTarefas(new FiltroTarefasQuery());


        Assert.Equal(2, resultado.TotalItens);
        Assert.Equal(2, resultado.Itens.Count);
        _repositoryMock.Verify(r => r.ObterTodas(), Times.Once);
    }

    [Fact(DisplayName = "ListarTarefas: deve filtrar pelo parametro prioridade")]
    public void ListarTarefas_QuandoFiltraPorPrioridade_DeveRetornarSomenteAsCorrespondentes()
    {

        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Baixa 1", Prioridade = PrioridadeTarefa.Baixa },
            new() { Titulo = "Alta 1",  Prioridade = PrioridadeTarefa.Alta },
            new() { Titulo = "Alta 2",  Prioridade = PrioridadeTarefa.Alta }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Prioridade = PrioridadeTarefa.Alta };


        var resultado = _servico.ListarTarefas(filtro);


        Assert.Equal(2, resultado.TotalItens);
        Assert.All(resultado.Itens, t => Assert.Equal(PrioridadeTarefa.Alta, t.Prioridade));
    }

    [Fact(DisplayName = "ListarTarefas: deve filtrar pelo parametro busca (titulo e descricao)")]
    public void ListarTarefas_QuandoFiltraPorBusca_DeveRetornarSomenteAsCorrespondentes()
    {

        var tarefasFalsas = new List<Tarefa>
        {
            new() { Titulo = "Estudar OpenTelemetry", Descricao = "metricas" },
            new() { Titulo = "Comprar pao",           Descricao = "padaria da esquina" },
            new() { Titulo = "Revisar PR",            Descricao = "estudar o diff com calma" }
        };

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Busca = "estudar" };


        var resultado = _servico.ListarTarefas(filtro);


        Assert.Equal(2, resultado.TotalItens);
    }

    [Fact(DisplayName = "ListarTarefas: deve respeitar os parametros de paginacao")]
    public void ListarTarefas_QuandoPaginado_DeveRetornarApenasAPaginaSolicitada()
    {

        var tarefasFalsas = Enumerable.Range(1, 10)
            .Select(i => new Tarefa { Titulo = $"Tarefa {i:00}", CriadaEm = DateTime.UtcNow.AddMinutes(i) })
            .ToList();

        _repositoryMock.Setup(r => r.ObterTodas()).Returns(tarefasFalsas);

        var filtro = new FiltroTarefasQuery { Pagina = 2, TamanhoPagina = 3, OrdenarPor = "titulo" };


        var resultado = _servico.ListarTarefas(filtro);


        Assert.Equal(10, resultado.TotalItens);
        Assert.Equal(3, resultado.Itens.Count);
        Assert.Equal(4, resultado.TotalPaginas);
        Assert.Equal("Tarefa 04", resultado.Itens[0].Titulo);
    }

    [Fact(DisplayName = "ObterPorId: deve devolver null quando o repositorio nao encontra a tarefa")]
    public void ObterPorId_QuandoNaoExiste_DeveRetornarNull()
    {

        var idInexistente = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorId(idInexistente)).Returns((Tarefa?)null);
        var resultado = _servico.ObterPorId(idInexistente);

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.ObterPorId(idInexistente), Times.Once);
    }
}
