namespace Pracrice.Services;

public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
