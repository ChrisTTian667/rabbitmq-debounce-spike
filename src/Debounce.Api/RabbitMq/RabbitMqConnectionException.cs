namespace Debounce.Api.RabbitMq;

using System;

[Serializable]
public class RabbitMqConnectionException : Exception
{
    public RabbitMqConnectionException()
    {
    }

    public RabbitMqConnectionException(string message)
        : base(message)
    {
    }

    public RabbitMqConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
