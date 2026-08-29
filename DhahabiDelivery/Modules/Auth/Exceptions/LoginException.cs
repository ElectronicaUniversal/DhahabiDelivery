namespace DhahabiDelivery.Modules.Auth.Exceptions;

public class LoginException : Exception
{
    public LoginException()
    {
    }

    public LoginException(string message) : base(message)
    {
    }
}