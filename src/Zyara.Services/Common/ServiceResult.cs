using System;
using System.Threading.Tasks;

namespace Zyara.Services.Common
{
    /// <summary>
    /// Wraps a result with a Success/Failure flag and optional error message.
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Succeeded { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }

        private ServiceResult() { }

        public static ServiceResult<T> Ok(T value) => new() { Succeeded = true, Value = value };
        public static ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
    }

    public class ServiceResult
    {
        public bool Succeeded { get; private set; }
        public string? Error { get; private set; }

        private ServiceResult() { }

        public static ServiceResult Ok() => new() { Succeeded = true };
        public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
    }
}

