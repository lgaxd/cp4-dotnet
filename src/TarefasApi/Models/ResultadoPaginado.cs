namespace TarefasApi.Models;

public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();
    public int TotalItens { get; init; }
    public int Pagina { get; init; }
    public int TamanhoPagina { get; init; }
    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
