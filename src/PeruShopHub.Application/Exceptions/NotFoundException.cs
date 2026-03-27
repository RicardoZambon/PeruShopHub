namespace PeruShopHub.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} com ID '{id}' não encontrado(a).")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
