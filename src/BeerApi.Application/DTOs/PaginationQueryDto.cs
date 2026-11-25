using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public sealed record PaginationQueryDto
{
    public const int MaxPage = 10_000;

    public PaginationQueryDto()
    {
    }

    public PaginationQueryDto(int page, int pageSize = 20)
    {
        Page = page;
        PageSize = pageSize;
    }

    [Range(1, MaxPage)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
