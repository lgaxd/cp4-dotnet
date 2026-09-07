using System.ComponentModel.DataAnnotations;

namespace TarefasApi.Models;

public class CriarTarefaRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "O campo 'titulo' é obrigatório.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O campo 'titulo' deve ter entre 3 e 120 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(500, ErrorMessage = "O campo 'descricao' deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    [EnumDataType(typeof(PrioridadeTarefa), ErrorMessage = "O campo 'prioridade' deve ser Baixa, Media ou Alta.")]
    public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Media;

    public DateTime? DataConclusaoPrevista { get; set; }
}
