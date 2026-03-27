namespace PeruShopHub.Application.Exceptions;

public class AppValidationException : Exception
{
    public Dictionary<string, List<string>> Errors { get; }

    public AppValidationException(Dictionary<string, List<string>> errors)
        : base("Erro de validação.")
    {
        Errors = errors;
    }

    public AppValidationException(string field, string message)
        : base("Erro de validação.")
    {
        Errors = new Dictionary<string, List<string>>
        {
            { field, new List<string> { message } }
        };
    }
}
