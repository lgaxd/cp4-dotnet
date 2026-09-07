using Microsoft.AspNetCore.Mvc;
using TarefasApi.Models;
using TarefasApi.Services;

namespace TarefasApi.Controllers;

[ApiController]
[Route("api/tarefas")]
[Produces("application/json")]
public class TarefasController : ControllerBase
{
    private readonly ITarefasService _servicoTarefas;
    private readonly ILogger<TarefasController> _registro;

    public TarefasController(ITarefasService servico, ILogger<TarefasController> registro)
    {
        _servicoTarefas = servico;
        _registro = registro;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Tarefa), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Inserir([FromBody] CriarTarefaRequest req)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var tarefa = _servicoTarefas.CriarTarefa(req);

            _registro.LogInformation(
                "POST /api/tarefas concluído com sucesso. {TarefaId} {NomeTarefa} {StatusCode}",
                tarefa.Id, tarefa.Titulo, StatusCodes.Status201Created);

            return CreatedAtAction(nameof(BuscarPorId), new { id = tarefa.Id }, tarefa);
        }
        catch (ArgumentException ex)
        {
            _registro.LogWarning(ex,
                "Regra de negócio violada ao criar tarefa. {MensagemErro} {StatusCode}",
                ex.Message, StatusCodes.Status400BadRequest);

            ModelState.AddModelError(ex.ParamName ?? nameof(req.Titulo), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Tarefa>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Recuperar([FromQuery] FiltroTarefasQuery filtroQuery)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var resposta = _servicoTarefas.ListarTarefas(filtroQuery);

        Response.Headers["X-Total-Itens"] = resposta.TotalItens.ToString();
        Response.Headers["X-Pagina"] = resposta.Pagina.ToString();
        Response.Headers["X-Tamanho-Pagina"] = resposta.TamanhoPagina.ToString();
        Response.Headers["X-Total-Paginas"] = resposta.TotalPaginas.ToString();

        _registro.LogInformation(
            "GET /api/tarefas concluído. {QuantidadeRetornada} {TotalItens} {StatusCode}",
            resposta.Itens.Count, resposta.TotalItens, StatusCodes.Status200OK);

        return Ok(resposta.Itens);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Tarefa), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult BuscarPorId([FromRoute] Guid id)
    {
        var item = _servicoTarefas.ObterPorId(id);

        if (item is null)
        {
            _registro.LogWarning("GET /api/tarefas/{TarefaId} retornou 404. {StatusCode}", id, StatusCodes.Status404NotFound);
            return Problem(
                title: "Tarefa não encontrada.",
                detail: $"Nenhuma tarefa encontrada com o Id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(item);
    }
}
