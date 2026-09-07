using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TarefasApi.Models;

namespace TarefasApi.Tests.Integracao;

/// <summary>
/// Testes de INTEGRAÇÃO dos controllers via WebApplicationFactory + HttpClient.
/// Cobrem os 3 fluxos HTTP exigidos pelo enunciado:
///   1) POST válido   -> 201 Created
///   2) GET listagem  -> 200 OK
///   3) POST inválido -> 400 Bad Request (nunca 500)
/// </summary>
public class TarefasControllerIntegrationTests : IClassFixture<TarefasApiFactory>
{
    private readonly TarefasApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public TarefasControllerIntegrationTests(TarefasApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // =======================================================================
    // FLUXO 1 — Criação com SUCESSO (201 Created)
    // =======================================================================

    [Fact(DisplayName = "POST /api/tarefas: payload valido deve retornar 201 Created")]
    public async Task PostTarefas_QuandoPayloadValido_DeveRetornar201Created()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest
        {
            Titulo = "Integracao - criar tarefa com sucesso",
            Descricao = "Teste de integracao do fluxo feliz",
            Prioridade = PrioridadeTarefa.Alta
        };

        // ---------- ACT ----------
        var resposta = await _client.PostAsJsonAsync("/api/tarefas", request, JsonOpcoes);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);

        var tarefaCriada = await resposta.Content.ReadFromJsonAsync<Tarefa>(JsonOpcoes);

        Assert.NotNull(tarefaCriada);
        Assert.NotEqual(Guid.Empty, tarefaCriada!.Id);
        Assert.Equal(request.Titulo, tarefaCriada.Titulo);
        Assert.Equal(PrioridadeTarefa.Alta, tarefaCriada.Prioridade);
        Assert.False(tarefaCriada.Concluida);
    }

    [Fact(DisplayName = "POST /api/tarefas: tarefa criada deve ser recuperavel pelo Location retornado")]
    public async Task PostTarefas_DepoisDeCriar_DeveSerRecuperavelPeloLocation()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = "Integracao - buscar pelo Location" };

        // ---------- ACT ----------
        var respostaCriacao = await _client.PostAsJsonAsync("/api/tarefas", request, JsonOpcoes);
        var location = respostaCriacao.Headers.Location!.ToString();
        var respostaBusca = await _client.GetAsync(location);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.Created, respostaCriacao.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respostaBusca.StatusCode);

        var tarefa = await respostaBusca.Content.ReadFromJsonAsync<Tarefa>(JsonOpcoes);
        Assert.Equal("Integracao - buscar pelo Location", tarefa!.Titulo);
    }

    // =======================================================================
    // FLUXO 2 — Listagem com SUCESSO (200 OK)
    // =======================================================================

    [Fact(DisplayName = "GET /api/tarefas: deve retornar 200 OK com uma lista JSON")]
    public async Task GetTarefas_DeveRetornar200OkComListaJson()
    {
        // ---------- ARRANGE ----------
        await _client.PostAsJsonAsync("/api/tarefas",
            new CriarTarefaRequest { Titulo = "Integracao - garantir item na listagem" }, JsonOpcoes);

        // ---------- ACT ----------
        var resposta = await _client.GetAsync("/api/tarefas");

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);

        var tarefas = await resposta.Content.ReadFromJsonAsync<List<Tarefa>>(JsonOpcoes);

        Assert.NotNull(tarefas);
        Assert.NotEmpty(tarefas!);
        Assert.Contains(tarefas!, t => t.Titulo == "Integracao - garantir item na listagem");

        // Metadados de paginação expostos em headers.
        Assert.True(resposta.Headers.Contains("X-Total-Itens"));
        Assert.True(resposta.Headers.Contains("X-Pagina"));
    }

    [Fact(DisplayName = "GET /api/tarefas: deve respeitar os parametros de query (busca e prioridade)")]
    public async Task GetTarefas_ComParametrosDeQuery_DeveFiltrarOsResultados()
    {
        // ---------- ARRANGE ----------
        var marcador = $"filtro-{Guid.NewGuid():N}";

        await _client.PostAsJsonAsync("/api/tarefas",
            new CriarTarefaRequest { Titulo = $"{marcador} alta", Prioridade = PrioridadeTarefa.Alta }, JsonOpcoes);

        await _client.PostAsJsonAsync("/api/tarefas",
            new CriarTarefaRequest { Titulo = $"{marcador} baixa", Prioridade = PrioridadeTarefa.Baixa }, JsonOpcoes);

        // ---------- ACT ----------
        var resposta = await _client.GetAsync($"/api/tarefas?busca={marcador}&prioridade=Alta");

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var tarefas = await resposta.Content.ReadFromJsonAsync<List<Tarefa>>(JsonOpcoes);

        Assert.NotNull(tarefas);
        Assert.Single(tarefas!);
        Assert.Equal($"{marcador} alta", tarefas![0].Titulo);
        Assert.Equal(PrioridadeTarefa.Alta, tarefas[0].Prioridade);
    }

    [Fact(DisplayName = "GET /api/tarefas: parametro de paginacao invalido deve retornar 400")]
    public async Task GetTarefas_ComPaginaInvalida_DeveRetornar400BadRequest()
    {
        // ---------- ARRANGE ----------
        const string urlComParametroInvalido = "/api/tarefas?pagina=0";

        // ---------- ACT ----------
        var resposta = await _client.GetAsync(urlComParametroInvalido);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    // =======================================================================
    // FLUXO 3 — Tratamento de erros (400 Bad Request, nunca 500)
    // =======================================================================

    [Fact(DisplayName = "POST /api/tarefas: payload sem titulo deve retornar 400 Bad Request")]
    public async Task PostTarefas_QuandoPayloadSemTitulo_DeveRetornar400BadRequest()
    {
        // ---------- ARRANGE ----------
        var payloadInvalido = new StringContent(
            """{ "descricao": "Tarefa sem titulo", "prioridade": "Media" }""",
            Encoding.UTF8, "application/json");

        // ---------- ACT ----------
        var resposta = await _client.PostAsync("/api/tarefas", payloadInvalido);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);

        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOpcoes);

        Assert.NotNull(problema);
        Assert.NotEmpty(problema!.Errors);

        // O erro precisa apontar para o campo 'titulo'.
        Assert.Contains(problema.Errors.Keys,
            chave => chave.Contains("titulo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "POST /api/tarefas: titulo em branco deve retornar 400 Bad Request")]
    public async Task PostTarefas_QuandoTituloEmBranco_DeveRetornar400BadRequest()
    {
        // ---------- ARRANGE ----------
        var payloadInvalido = new StringContent(
            """{ "titulo": "   " }""",
            Encoding.UTF8, "application/json");

        // ---------- ACT ----------
        var resposta = await _client.PostAsync("/api/tarefas", payloadInvalido);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact(DisplayName = "POST /api/tarefas: titulo muito curto deve retornar 400 Bad Request")]
    public async Task PostTarefas_QuandoTituloMuitoCurto_DeveRetornar400BadRequest()
    {
        // ---------- ARRANGE ----------
        var request = new CriarTarefaRequest { Titulo = "ab" }; // minimo = 3 caracteres

        // ---------- ACT ----------
        var resposta = await _client.PostAsJsonAsync("/api/tarefas", request, JsonOpcoes);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact(DisplayName = "POST /api/tarefas: JSON malformado deve retornar 400 e nao 500")]
    public async Task PostTarefas_QuandoJsonMalformado_DeveRetornar400ENao500()
    {
        // ---------- ARRANGE ----------
        var jsonQuebrado = new StringContent("{ isso nao e json }", Encoding.UTF8, "application/json");

        // ---------- ACT ----------
        var resposta = await _client.PostAsync("/api/tarefas", jsonQuebrado);

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }

    [Fact(DisplayName = "GET /api/tarefas/{id}: id inexistente deve retornar 404 Not Found")]
    public async Task GetTarefaPorId_QuandoNaoExiste_DeveRetornar404NotFound()
    {
        // ---------- ARRANGE ----------
        var idInexistente = Guid.NewGuid();

        // ---------- ACT ----------
        var resposta = await _client.GetAsync($"/api/tarefas/{idInexistente}");

        // ---------- ASSERT ----------
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
