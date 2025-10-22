namespace AdsApi.Errors;

public class NotFoundException : Exception { public NotFoundException(string m) : base(m) {} }
public class ConflictException : Exception { public ConflictException(string m) : base(m) {} }

public class DomainValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }
    public DomainValidationException(string message, IDictionary<string,string[]> errors) : base(message)
        => Errors = errors;
}
