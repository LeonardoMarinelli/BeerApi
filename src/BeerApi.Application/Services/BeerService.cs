using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

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
            beer.BreweryId, beer.Brewery?.Name ?? string.Empty);
}
