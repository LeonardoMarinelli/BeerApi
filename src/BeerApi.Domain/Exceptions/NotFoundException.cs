namespace BeerApi.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"Não foi possível encontrar {entityName} com id '{id}'.") { }

    public NotFoundException(string message) : base(message) { }
}
