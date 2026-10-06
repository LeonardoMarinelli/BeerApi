using System.Text.Json.Serialization;

namespace BeerApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<BeerStyle>))]
public enum BeerStyle
{
    Other,
    Lager,
    Witbier,
    Blonde,
    Dubbel,
    Tripel,
    Quadrupel,
    StrongGolden,
    AbbeyAle
}