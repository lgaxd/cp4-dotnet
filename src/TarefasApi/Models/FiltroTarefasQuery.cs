using System.ComponentModel.DataAnnotations;

namespace TarefasApi.Models;

/// <summary>
/// Parâmetros de query string aceitos pelo endpoint GET /api/tarefas.
/// Todos são opcionais — sem nenhum deles a API devolve a lista completa.
/// </summary>
public class FiltroTarefasQuery
{
    /// <summary>Filtra por situação: true = concluídas, false = pendentes.</summary>
    public bool? Concluida { get; set; }

    /// <summary>Filtra por prioridade (Baixa, Media, Alta).</summary>
    [EnumDataType(typeof(PrioridadeTarefa), ErrorMessage = "O parâmetro 'prioridade' deve ser Baixa, Media ou Alta.")]
    public PrioridadeTarefa? Prioridade { get; set; }

    /// <summary>Busca textual (case-insensitive) no título e na descrição.</summary>
    public string? Busca { get; set; }

    /// <summary>Número da página (base 1).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "O parâmetro 'pagina' deve ser maior ou igual a 1.")]
    public int Pagina { get; set; } = 1;

    /// <summary>Quantidade de itens por página (1 a 100).</summary>
    [Range(1, 100, ErrorMessage = "O parâmetro 'tamanhoPagina' deve estar entre 1 e 100.")]
    public int TamanhoPagina { get; set; } = 50;

    /// <summary>Campo de ordenação: criadaEm (padrão), titulo ou prioridade.</summary>
    public string OrdenarPor { get; set; } = "criadaEm";

    /// <summary>Direção da ordenação: asc (padrão) ou desc.</summary>
    public string Ordem { get; set; } = "asc";
}
