namespace PeruShopHub.Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException()
        : base("Este registro foi modificado por outro usuário. Recarregue e tente novamente.")
    {
    }
}
