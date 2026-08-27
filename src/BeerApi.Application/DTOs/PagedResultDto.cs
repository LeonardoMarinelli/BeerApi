namespace BeerApi.Application.DTOs;

public record PagedResultDto<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
