using System.Text;

namespace SQLMultiScript.Core.Models
{
    public class Result
    {
        public bool Success { get; protected set; }
        public List<string> Errors { get; protected set; } = new();

        public static Result Ok() => new Result { Success = true };
        public static Result Fail(params string[] errors) => new Result { Success = false, Errors = errors.ToList() };

        protected Result() { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var error in Errors)
                sb.AppendLine(error);

            return sb.ToString();
        }

    }

    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        public static Result<T> Ok(T value) => new Result<T> { Success = true, Value = value };
        public new static Result<T> Fail(params string[] errors) => new Result<T> { Success = false, Errors = errors.ToList() };

        protected Result() { }
    }

}
