namespace BeerApi.Application.DTOs;

public sealed record CursorPageResultDto<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);