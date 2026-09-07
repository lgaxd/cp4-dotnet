using System.ComponentModel.DataAnnotations;

namespace TarefasApi.Models;

/// <summary>
/// Payload de entrada do endpoint POST /api/tarefas.
/// A validação por DataAnnotations garante o 400 Bad Request automático
/// (ValidationProblemDetails) quando o título não é informado.
/// </summary>
public class CriarTarefaRequest
{
    /// <summary>Título da tarefa. Parâmetro OBRIGATÓRIO.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "O campo 'titulo' é obrigatório.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O campo 'titulo' deve ter entre 3 e 120 caracteres.")]
    public string? Titulo { get; set; }

    /// <summary>Descrição da tarefa. Parâmetro opcional.</summary>
    [StringLength(500, ErrorMessage = "O campo 'descricao' deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    /// <summary>Prioridade da tarefa. Parâmetro opcional (Baixa, Media, Alta). Padrão: Media.</summary>
    [EnumDataType(typeof(PrioridadeTarefa), ErrorMessage = "O campo 'prioridade' deve ser Baixa, Media ou Alta.")]
    public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Media;

    /// <summary>Data prevista de conclusão. Parâmetro opcional.</summary>
    public DateTime? DataConclusaoPrevista { get; set; }
}
