using System.ComponentModel.DataAnnotations;

namespace TarefasApi.Models;

public class FiltroTarefasQuery
{
    public bool? Concluida { get; set; }

    [EnumDataType(typeof(PrioridadeTarefa), ErrorMessage = "O parâmetro 'prioridade' deve ser Baixa, Media ou Alta.")]
    public PrioridadeTarefa? Prioridade { get; set; }

    public string? Busca { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "O parâmetro 'pagina' deve ser maior ou igual a 1.")]
    public int Pagina { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "O parâmetro 'tamanhoPagina' deve estar entre 1 e 100.")]
    public int TamanhoPagina { get; set; } = 50;

    public string OrdenarPor { get; set; } = "criadaEm";

    public string Ordem { get; set; } = "asc";
}
