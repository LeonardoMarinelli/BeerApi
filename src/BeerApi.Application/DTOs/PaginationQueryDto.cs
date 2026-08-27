using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public record PaginationQueryDto(
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 20);
