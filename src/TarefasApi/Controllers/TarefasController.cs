using Microsoft.AspNetCore.Mvc;
using TarefasApi.Models;
using TarefasApi.Services;

namespace TarefasApi.Controllers;

/// <summary>
/// Endpoints de gerenciamento de Tarefas (To-Do).
/// </summary>
[ApiController]
[Route("api/tarefas")]
[Produces("application/json")]
public class TarefasController : ControllerBase
{
    private readonly ITarefasService _service;
    private readonly ILogger<TarefasController> _logger;

    public TarefasController(ITarefasService service, ILogger<TarefasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Cria uma nova tarefa.</summary>
    /// <response code="201">Tarefa criada com sucesso.</response>
    /// <response code="400">Payload inválido (ex.: sem título).</response>
    [HttpPost]
    [ProducesResponseType(typeof(Tarefa), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Criar([FromBody] CriarTarefaRequest request)
    {
        // 1ª barreira (DataAnnotations): o filtro do [ApiController] já devolveu 400
        // antes de chegar aqui — o log e a métrica desse caso ficam em
        // FabricaRespostaValidacao. Esta checagem é apenas uma defesa extra caso o
        // filtro automático seja desativado.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            // 2ª barreira: regras de negócio da camada de serviço.
            var tarefa = _service.CriarTarefa(request);

            _logger.LogInformation(
                "POST /api/tarefas concluído com sucesso. {TarefaId} {NomeTarefa} {StatusCode}",
                tarefa.Id, tarefa.Titulo, StatusCodes.Status201Created);

            return CreatedAtAction(nameof(ObterPorId), new { id = tarefa.Id }, tarefa);
        }
        catch (ArgumentException ex)
        {
            // Converte a exceção de negócio em 400 Bad Request — nunca em 500.
            _logger.LogWarning(ex,
                "Regra de negócio violada ao criar tarefa. {MensagemErro} {StatusCode}",
                ex.Message, StatusCodes.Status400BadRequest);

            ModelState.AddModelError(ex.ParamName ?? nameof(request.Titulo), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    /// <summary>Lista as tarefas com filtros, ordenação e paginação opcionais.</summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="400">Parâmetros de query inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Tarefa>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Listar([FromQuery] FiltroTarefasQuery filtro)
    {
        // Defesa extra: o 400 dos parâmetros de query já é tratado (e logado)
        // por FabricaRespostaValidacao antes de a action executar.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var resultado = _service.ListarTarefas(filtro);

        // Metadados de paginação nos headers (o corpo continua sendo uma lista simples).
        Response.Headers["X-Total-Itens"] = resultado.TotalItens.ToString();
        Response.Headers["X-Pagina"] = resultado.Pagina.ToString();
        Response.Headers["X-Tamanho-Pagina"] = resultado.TamanhoPagina.ToString();
        Response.Headers["X-Total-Paginas"] = resultado.TotalPaginas.ToString();

        _logger.LogInformation(
            "GET /api/tarefas concluído. {QuantidadeRetornada} {TotalItens} {StatusCode}",
            resultado.Itens.Count, resultado.TotalItens, StatusCodes.Status200OK);

        return Ok(resultado.Itens);
    }

    /// <summary>Obtém uma tarefa específica pelo seu Id.</summary>
    /// <response code="200">Tarefa encontrada.</response>
    /// <response code="404">Tarefa não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Tarefa), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult ObterPorId([FromRoute] Guid id)
    {
        var tarefa = _service.ObterPorId(id);

        if (tarefa is null)
        {
            _logger.LogWarning("GET /api/tarefas/{TarefaId} retornou 404. {StatusCode}", id, StatusCodes.Status404NotFound);
            return Problem(
                title: "Tarefa não encontrada.",
                detail: $"Nenhuma tarefa encontrada com o Id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(tarefa);
    }
}
