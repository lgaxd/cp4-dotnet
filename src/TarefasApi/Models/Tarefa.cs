namespace TarefasApi.Models;

/// <summary>
/// Representa uma tarefa (To-Do) armazenada em memória.
/// </summary>
public class Tarefa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Título/nome da tarefa. Obrigatório.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Descrição livre da tarefa (parâmetro opcional).</summary>
    public string? Descricao { get; set; }

    /// <summary>Prioridade da tarefa (parâmetro opcional, padrão = Media).</summary>
    public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Media;

    /// <summary>Data limite para conclusão (parâmetro opcional).</summary>
    public DateTime? DataConclusaoPrevista { get; set; }

    /// <summary>Indica se a tarefa já foi concluída.</summary>
    public bool Concluida { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
