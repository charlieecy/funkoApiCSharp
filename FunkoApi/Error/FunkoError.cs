namespace FunkoApi.Error;

public record FunkoError (string Message)
{
    public string Message { get; set; } = Message;
}

public record FunkoNotFoundError (string Message) : FunkoError (Message);

public record FunkoValidationError (string Message) : FunkoError (Message);

public record FunkoBadRequestError (string Message) : FunkoError (Message);

public record FunkoConflictError (string Message) : FunkoError (Message);

public record FunkoStorageError (string Message) : FunkoError (Message);