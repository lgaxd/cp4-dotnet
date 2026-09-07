using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using TarefasApi.Models;

namespace TarefasApi.Observabilidade;

public class MetricasTarefas : IDisposable
{
    public const string NomeMeter = "TarefasApi.Metricas";

    private readonly Meter _meter;

    private readonly Counter<long> _requisicoesTotal;
    private readonly Counter<long> _tarefasCriadas;
    private readonly Counter<long> _tarefasRejeitadas;
    private readonly Histogram<double> _duracaoRequisicoesMs;

    private long _totalRequisicoes;
    private long _totalTarefasCriadas;
    private long _totalTarefasRejeitadas;
    private double _somaDuracaoMs;
    private double _duracaoMaximaMs;
    private double _duracaoMinimaMs = double.MaxValue;

    private readonly ConcurrentDictionary<int, long> _porStatusCode = new();
    private readonly ConcurrentDictionary<string, long> _porRota = new();
    private readonly ConcurrentDictionary<string, long> _motivosRejeicao = new();
    private readonly object _lock = new();

    public MetricasTarefas()
    {
        _meter = new Meter(NomeMeter, "1.0.0");

        _requisicoesTotal = _meter.CreateCounter<long>(
            "tarefas_api.requisicoes.total", "requisições",
            "Quantidade total de requisições HTTP recebidas pela API.");

        _duracaoRequisicoesMs = _meter.CreateHistogram<double>(
            "tarefas_api.requisicoes.duracao", "ms",
            "Tempo de resposta das requisições HTTP em milissegundos.");

        _tarefasCriadas = _meter.CreateCounter<long>(
            "tarefas_api.tarefas.criadas", "tarefas",
            "Quantidade de tarefas criadas com sucesso.");

        _tarefasRejeitadas = _meter.CreateCounter<long>(
            "tarefas_api.tarefas.rejeitadas", "tarefas",
            "Quantidade de tentativas de criação rejeitadas por regra de negócio.");

        _meter.CreateObservableGauge(
            "tarefas_api.requisicoes.duracao_media", () => TempoMedioRespostaMs, "ms",
            "Tempo médio de resposta das requisições HTTP.");
    }

    public long TotalRequisicoes => Interlocked.Read(ref _totalRequisicoes);
    public long TotalTarefasCriadas => Interlocked.Read(ref _totalTarefasCriadas);
    public long TotalTarefasRejeitadas => Interlocked.Read(ref _totalTarefasRejeitadas);

    public double TempoMedioRespostaMs
    {
        get
        {
            lock (_lock)
            {
                return _totalRequisicoes == 0 ? 0 : Math.Round(_somaDuracaoMs / _totalRequisicoes, 3);
            }
        }
    }

    public double TempoMaximoRespostaMs { get { lock (_lock) { return Math.Round(_duracaoMaximaMs, 3); } } }

    public double TempoMinimoRespostaMs
    {
        get { lock (_lock) { return _duracaoMinimaMs == double.MaxValue ? 0 : Math.Round(_duracaoMinimaMs, 3); } }
    }

    public IReadOnlyDictionary<int, long> RequisicoesPorStatusCode => _porStatusCode;
    public IReadOnlyDictionary<string, long> RequisicoesPorRota => _porRota;
    public IReadOnlyDictionary<string, long> MotivosDeRejeicao => _motivosRejeicao;

    public void RegistrarRequisicao(string metodo, string rota, int statusCode, double duracaoMs)
    {
        Interlocked.Increment(ref _totalRequisicoes);

        lock (_lock)
        {
            _somaDuracaoMs += duracaoMs;
            if (duracaoMs > _duracaoMaximaMs) _duracaoMaximaMs = duracaoMs;
            if (duracaoMs < _duracaoMinimaMs) _duracaoMinimaMs = duracaoMs;
        }

        _porStatusCode.AddOrUpdate(statusCode, 1, (_, atual) => atual + 1);
        _porRota.AddOrUpdate($"{metodo} {rota}", 1, (_, atual) => atual + 1);

        var tags = new KeyValuePair<string, object?>[]
        {
            new("http.request.method", metodo),
            new("http.route", rota),
            new("http.response.status_code", statusCode)
        };

        _requisicoesTotal.Add(1, tags);
        _duracaoRequisicoesMs.Record(duracaoMs, tags);
    }

    public void RegistrarTarefaCriada(PrioridadeTarefa prioridade)
    {
        Interlocked.Increment(ref _totalTarefasCriadas);
        _tarefasCriadas.Add(1, new KeyValuePair<string, object?>("prioridade", prioridade.ToString()));
    }

    public void RegistrarTarefaRejeitada(string motivo)
    {
        Interlocked.Increment(ref _totalTarefasRejeitadas);
        _motivosRejeicao.AddOrUpdate(motivo, 1, (_, atual) => atual + 1);
        _tarefasRejeitadas.Add(1, new KeyValuePair<string, object?>("motivo", motivo));
    }

    public void Dispose() => _meter.Dispose();
}
