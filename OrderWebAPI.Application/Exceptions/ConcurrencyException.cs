using System;

namespace OrderWebAPI.Application.Exceptions
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException() : base("Concurrency conflict occurred") { }
        public ConcurrencyException(string message) : base(message) { }
        public ConcurrencyException(string message, Exception inner) : base(message, inner) { }
    }
}
