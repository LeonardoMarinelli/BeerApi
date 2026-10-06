using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Queries;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeerApi.Application.Services;

public class BeerService(IBeerRepository beerRepository, IBreweryRepository breweryRepository) : IBeerService
{
    private readonly IBeerRepository _beerRepository = beerRepository;
    private readonly IBreweryRepository _breweryRepository = breweryRepository;

    public async Task<IEnumerable<BeerDto>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default)
    {
        if (!await _breweryRepository.ExistsAsync(breweryId, ct))
            throw new NotFoundException("Cervejaria", breweryId);

        var beers = await _beerRepository.GetByBreweryIdAsync(breweryId, ct);
        return beers.Select(MapToDto);
    }

    public async Task<PagedResultDto<BeerDto>> SearchAsync(
        BeerSearchFiltersDto filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var criteria = BuildCriteria(filters);
        var (beers, totalCount) = await _beerRepository.SearchAsync(criteria, page, pageSize, ct);
        return new PagedResultDto<BeerDto>(beers.Select(MapToDto), page, pageSize, totalCount);
    }

    public async Task<CursorPageResultDto<BeerDto>> SearchCursorAsync(
        BeerSearchFiltersDto filters,
        int pageSize,
        string? cursor,
        CancellationToken ct = default)
    {
        var criteria = BuildCriteria(filters);
        if (criteria.SortBy == BeerSortField.Name)
            throw new BusinessException("A paginação por cursor aceita ordenação por id, preço ou teor alcoólico.");

        var filterHash = GetFilterHash(criteria);
        var position = cursor is null ? null : DecodeCursor(cursor, filterHash);
        var results = (await _beerRepository.SearchCursorAsync(criteria, position, pageSize + 1, ct)).ToList();
        var hasMore = results.Count > pageSize;
        if (hasMore)
            results.RemoveAt(results.Count - 1);

        var items = results.Select(MapToDto).ToList();
        var nextCursor = hasMore && items.Count > 0
            ? EncodeCursor(filterHash, GetSortValue(items[^1], criteria.SortBy), items[^1].Id)
            : null;

        return new CursorPageResultDto<BeerDto>(items, nextCursor, hasMore);
    }

    public async Task<BeerDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var beer = await _beerRepository.GetByIdWithBreweryAsync(id, ct)
            ?? throw new NotFoundException("Cerveja", id);
        return MapToDto(beer);
    }

    public async Task<BeerDto> CreateAsync(int breweryId, CreateBeerDto dto, CancellationToken ct = default)
    {
        var brewery = await _breweryRepository.GetByIdAsync(breweryId, ct)
            ?? throw new NotFoundException("Cervejaria", breweryId);

        var beer = new Beer
        {
            Name = dto.Name,
            Description = dto.Description,
            AlcoholContent = dto.AlcoholContent,
            Price = dto.Price,
            Style = dto.Style,
            BreweryId = breweryId,
            Brewery = brewery
        };

        await _beerRepository.AddAsync(beer, ct);
        return MapToDto(beer);
    }

    public async Task<BeerDto> UpdateAsync(int breweryId, int beerId, UpdateBeerDto dto, CancellationToken ct = default)
    {
        var beer = await _beerRepository.GetByIdWithBreweryAsync(beerId, ct)
            ?? throw new NotFoundException("Cerveja", beerId);

        if (beer.BreweryId != breweryId)
            throw new BusinessException("Esta cerveja não pertence a esta cervejaria.");

        beer.Name = dto.Name;
        beer.Description = dto.Description;
        beer.AlcoholContent = dto.AlcoholContent;
        beer.Price = dto.Price;
        beer.Style = dto.Style;

        await _beerRepository.UpdateAsync(beer, ct);
        return MapToDto(beer);
    }

    public async Task DeleteAsync(int breweryId, int beerId, CancellationToken ct = default)
    {
        var beer = await _beerRepository.GetByIdAsync(beerId, ct)
            ?? throw new NotFoundException("Cerveja", beerId);

        if (beer.BreweryId != breweryId)
            throw new BusinessException("Esta cerveja não pertence a esta cervejaria.");

        await _beerRepository.DeleteAsync(beer, ct);
    }

    private static BeerDto MapToDto(Beer beer) =>
        new(beer.Id, beer.Name, beer.Description, beer.AlcoholContent, beer.Price,
            beer.BreweryId, beer.Brewery?.Name ?? string.Empty, beer.Style);

    private static BeerSearchCriteria BuildCriteria(BeerSearchFiltersDto filters)
    {
        if (filters.MinAbv > filters.MaxAbv || filters.MinPrice > filters.MaxPrice)
            throw new BusinessException("Os limites mínimos não podem exceder os máximos.");

        var sortBy = filters.SortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "id" => BeerSortField.Id,
            "name" => BeerSortField.Name,
            "price" => BeerSortField.Price,
            "abv" => BeerSortField.AlcoholContent,
            _ => throw new BusinessException("Ordenação inválida. Use id, name, price ou abv.")
        };
        var direction = filters.SortDir?.Trim().ToLowerInvariant() switch
        {
            null or "" or "asc" => SortDirection.Ascending,
            "desc" => SortDirection.Descending,
            _ => throw new BusinessException("Direção inválida. Use asc ou desc.")
        };

        return new BeerSearchCriteria(
            string.IsNullOrWhiteSpace(filters.Q) ? null : filters.Q.Trim(),
            filters.BreweryId,
            filters.Style,
            filters.MinAbv,
            filters.MaxAbv,
            filters.MinPrice,
            filters.MaxPrice,
            sortBy,
            direction);
    }

    private static string GetFilterHash(BeerSearchCriteria criteria) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(criteria)));

    private static string EncodeCursor(string filterHash, decimal sortValue, int beerId)
    {
        var payload = new BeerCursorPayload(filterHash, sortValue, beerId);
        var encoded = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload));
        return encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static BeerCursorPosition DecodeCursor(string cursor, string expectedFilterHash)
    {
        try
        {
            if (cursor.Length is 0 or > 2048 || cursor.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
                throw new FormatException();

            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var payload = JsonSerializer.Deserialize<BeerCursorPayload>(Convert.FromBase64String(base64));
            if (payload is null || !string.Equals(payload.FilterHash, expectedFilterHash, StringComparison.Ordinal))
                throw new FormatException();

            return new BeerCursorPosition(payload.SortValue, payload.BeerId);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new BusinessException("Cursor inválido ou incompatível com os filtros informados.");
        }
    }

    private static decimal GetSortValue(BeerDto beer, BeerSortField sortBy) => sortBy switch
    {
        BeerSortField.Id => beer.Id,
        BeerSortField.Price => beer.Price,
        BeerSortField.AlcoholContent => beer.AlcoholContent,
        _ => throw new BusinessException("Campo de ordenação não suportado pelo cursor.")
    };

    private sealed record BeerCursorPayload(string FilterHash, decimal SortValue, int BeerId);
}
