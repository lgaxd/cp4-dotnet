using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TarefasApi.Models;

namespace TarefasApi.Tests.Integracao;

public class MonitoramentoIntegrationTests : IClassFixture<TarefasApiFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public MonitoramentoIntegrationTests(TarefasApiFactory factory)
    {
        _client = factory.CreateClient();
    }



    [Fact(DisplayName = "GET /health: deve retornar 200 OK com status Healthy")]
    public async Task GetHealth_DeveRetornar200ComStatusHealthy()
    {
        const string endpoint = "/health";

        var resposta = await _client.GetAsync(endpoint);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Healthy", corpo, StringComparison.OrdinalIgnoreCase);

        using var documento = JsonDocument.Parse(corpo);
        Assert.Equal("Healthy", documento.RootElement.GetProperty("status").GetString());
    }

    [Fact(DisplayName = "GET /health: deve listar as verificacoes registradas")]
    public async Task GetHealth_DeveListarAsVerificacoesRegistradas()
    {
        const string endpoint = "/health";

        var corpo = await _client.GetStringAsync(endpoint);
        using var documento = JsonDocument.Parse(corpo);
        var verificacoes = documento.RootElement.GetProperty("verificacoes");

        var nomes = verificacoes.EnumerateArray()
            .Select(v => v.GetProperty("nome").GetString())
            .ToList();

        Assert.Contains("repositorio-tarefas", nomes);
        Assert.Contains("self", nomes);
    }

    [Fact(DisplayName = "GET /health/live: deve retornar 200 OK e o texto Healthy")]
    public async Task GetHealthLive_DeveRetornar200ComTextoHealthy()
    {
        const string endpoint = "/health/live";

        var resposta = await _client.GetAsync(endpoint);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", corpo.Trim());
    }

    [Fact(DisplayName = "GET /health/ready: deve retornar 200 OK com status Healthy")]
    public async Task GetHealthReady_DeveRetornar200ComStatusHealthy()
    {
        const string endpoint = "/health/ready";

        var resposta = await _client.GetAsync(endpoint);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Healthy", corpo, StringComparison.OrdinalIgnoreCase);
    }



    [Fact(DisplayName = "GET /metrics: deve contar as requisicoes e medir o tempo de resposta")]
    public async Task GetMetrics_DeveContarRequisicoesEMedirTempoDeResposta()
    {
        await _client.GetAsync("/api/tarefas");
        await _client.GetAsync("/api/tarefas");
        await _client.PostAsJsonAsync("/api/tarefas",
            new CriarTarefaRequest { Titulo = "Metricas - tarefa de aquecimento" }, JsonOpcoes);

        var resposta = await _client.GetAsync("/metrics");
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        Assert.True(raiz.GetProperty("totalRequisicoes").GetInt64() >= 3,
            "O contador de requisicoes deveria ter registrado ao menos as 3 chamadas anteriores.");

        Assert.True(raiz.GetProperty("tempoMedioRespostaMs").GetDouble() >= 0,
            "O tempo medio de resposta deveria ter sido calculado.");

        Assert.True(raiz.GetProperty("totalTarefasCriadas").GetInt64() >= 1,
            "O contador de tarefas criadas deveria ter registrado ao menos 1 criacao.");
    }

    [Fact(DisplayName = "GET /metrics: deve registrar os motivos de rejeicao das tarefas invalidas")]
    public async Task GetMetrics_AposRequisicaoInvalida_DeveRegistrarRejeicaoOuStatus400()
    {
        var payloadInvalido = new StringContent(
            """{ "descricao": "sem titulo" }""",
            System.Text.Encoding.UTF8, "application/json");

        var respostaInvalida = await _client.PostAsync("/api/tarefas", payloadInvalido);
        var corpoMetricas = await _client.GetStringAsync("/metrics");
        Assert.Equal(HttpStatusCode.BadRequest, respostaInvalida.StatusCode);

        using var documento = JsonDocument.Parse(corpoMetricas);
        var porStatus = documento.RootElement.GetProperty("requisicoesPorStatusCode");

        Assert.True(porStatus.TryGetProperty("400", out var contagem400),
            "O snapshot de metricas deveria conter a contagem de respostas 400.");
        Assert.True(contagem400.GetInt64() >= 1);
    }

    [Fact(DisplayName = "Middleware de metricas: deve devolver o header X-Correlation-Id em toda resposta")]
    public async Task Middleware_DeveDevolverHeaderDeCorrelacao()
    {
        const string endpoint = "/api/tarefas";

        var resposta = await _client.GetAsync(endpoint);
        Assert.True(resposta.Headers.Contains("X-Correlation-Id"),
            "O middleware de observabilidade deveria devolver o header X-Correlation-Id.");
    }

    [Fact(DisplayName = "Middleware de metricas: deve preservar o X-Correlation-Id enviado pelo cliente")]
    public async Task Middleware_QuandoClienteEnviaCorrelationId_DevePreservarOMesmoValor()
    {

        var correlationId = Guid.NewGuid().ToString("N");
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/tarefas");
        requisicao.Headers.Add("X-Correlation-Id", correlationId);

        var resposta = await _client.SendAsync(requisicao);

        Assert.Equal(correlationId, resposta.Headers.GetValues("X-Correlation-Id").First());
    }
}
