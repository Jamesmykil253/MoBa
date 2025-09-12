using System;

namespace MOBA
{
    /// <summary>
    /// Result pattern implementation for consistent error handling
    /// Based on Code Complete defensive programming principles
    /// Replaces inconsistent null returns and exception throwing
    /// </summary>
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error);

        public static implicit operator bool(Result result) => result.IsSuccess;

        public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
    }

    /// <summary>
    /// Generic result pattern for operations that return values
    /// </summary>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string error) => new Result<T>(false, default(T), error);

        public static implicit operator bool(Result<T> result) => result.IsSuccess;

        /// <summary>
        /// Get value or throw exception if failed
        /// </summary>
        public T GetValueOrThrow()
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Result failed: {Error}");
            return Value;
        }

        /// <summary>
        /// Get value or return default
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default(T))
        {
            return IsSuccess ? Value : defaultValue;
        }

        /// <summary>
        /// Execute action if successful
        /// </summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess) action(Value);
            return this;
        }

        /// <summary>
        /// Execute action if failed
        /// </summary>
        public Result<T> OnFailure(Action<string> action)
        {
            if (!IsSuccess) action(Error);
            return this;
        }

        public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
    }

    /// <summary>
    /// Extension methods for Result pattern
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Convert nullable to Result
        /// </summary>
        public static Result<T> ToResult<T>(this T value, string errorMessage = "Value is null") where T : class
        {
            return value != null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage);
        }

        /// <summary>
        /// Convert boolean operation to Result
        /// </summary>
        public static Result ToResult(this bool condition, string errorMessage = "Operation failed")
        {
            return condition ? Result.Success() : Result.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Validation helper for defensive programming
    /// </summary>
    public static class Validate
    {
        public static Result<T> NotNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
                return Result<T>.Failure($"Parameter '{parameterName}' cannot be null");
            return Result<T>.Success(value);
        }

        public static Result NotNaN(float value, string parameterName)
        {
            if (float.IsNaN(value))
                return Result.Failure($"Parameter '{parameterName}' cannot be NaN");
            return Result.Success();
        }

        public static Result InRange(float value, float min, float max, string parameterName)
        {
            if (value < min || value > max)
                return Result.Failure($"Parameter '{parameterName}' must be between {min} and {max}, got {value}");
            return Result.Success();
        }

        public static Result<UnityEngine.Vector3> ValidMovementInput(UnityEngine.Vector3 input, float maxMagnitude)
        {
            // Check for NaN values
            if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsNaN(input.z))
                return Result<UnityEngine.Vector3>.Failure("Movement input contains NaN values");

            // Check for infinite values
            if (float.IsInfinity(input.x) || float.IsInfinity(input.y) || float.IsInfinity(input.z))
                return Result<UnityEngine.Vector3>.Failure("Movement input contains infinite values");

            // Clamp magnitude if too large
            if (input.magnitude > maxMagnitude)
            {
                input = input.normalized * maxMagnitude;
                Logger.LogWarning($"Movement input clamped to maximum magnitude: {maxMagnitude}");
            }

            return Result<UnityEngine.Vector3>.Success(input);
        }
    }
}
