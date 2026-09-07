namespace TarefasApi.Models;

public class Tarefa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Media;

    public DateTime? DataConclusaoPrevista { get; set; }

    public bool Concluida { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
