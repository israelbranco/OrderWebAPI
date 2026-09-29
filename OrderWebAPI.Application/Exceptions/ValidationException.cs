using System;
using System.Collections.Generic;

namespace OrderWebAPI.Application.Exceptions
{
    public record ValidationError(string Field, string Message, string? Code = null, object? Data = null);

    public class ValidationException : Exception
    {
        public IEnumerable<ValidationError> Errors { get; }

        public ValidationException(IEnumerable<ValidationError> errors)
            : base("Validation failed")
        {
            Errors = errors ?? Array.Empty<ValidationError>();
        }
    }
}
