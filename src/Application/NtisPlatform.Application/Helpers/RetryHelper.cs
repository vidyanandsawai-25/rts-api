using Microsoft.Extensions.Logging;

namespace NtisPlatform.Application.Helpers
{
    /// <summary>
    /// Provides retry logic with exponential backoff for resilience against transient failures.
    /// Implements industry-standard retry patterns for distributed systems.
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes an async operation with automatic retry on transient failures.
        /// Implements exponential backoff strategy (50ms, 100ms, 200ms delays).
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="logger">Logger for tracking retry attempts</param>
        /// <param name="operationName">Descriptive name for logging (e.g., "RuleExecution", "DatabaseQuery")</param>
        /// <param name="contextId">Context identifier for logging (e.g., PropertyDetailsId, UserId)</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="cancellationToken">Token to cancel the retry operation and delays</param>
        /// <returns>The result of the successful operation</returns>
        /// <exception cref="Exception">Throws the last exception if all retries are exhausted</exception>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            ILogger logger,
            string operationName,
            string contextId,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    var result = await operation();

                    // Success - log if retry was needed
                    if (attempt > 1)
                    {
                        logger.LogInformation(
                            "[{OperationName}] ✅ Succeeded on attempt {Attempt}/{MaxRetries} for {ContextId}",
                            operationName, attempt, maxRetries, contextId);
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < maxRetries && IsTransientException(ex))
                {
                    lastException = ex;
                    var delay = CalculateExponentialBackoff(attempt);

                    logger.LogWarning(
                        "[{OperationName}] ⚠️ Transient failure on attempt {Attempt}/{MaxRetries} for {ContextId}. " +
                        "Retrying after {DelayMs}ms. Error: {ErrorMessage}",
                        operationName, attempt, maxRetries, contextId, delay, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            // All retries exhausted - throw last exception
            logger.LogError(lastException,
                "[{OperationName}] ❌ All {MaxRetries} retry attempts exhausted for {ContextId}",
                operationName, maxRetries, contextId);

            throw lastException ?? new InvalidOperationException($"{operationName} failed with unknown error");
        }

        /// <summary>
        /// Determines if an exception is transient and worth retrying.
        /// Transient exceptions are temporary issues that may resolve themselves (network, timeout, temp DB issues).
        /// NOTE: TaskCanceledException and OperationCanceledException are NOT retried as they represent intentional cancellation.
        /// </summary>
        /// <param name="ex">The exception to evaluate</param>
        /// <returns>True if the exception is transient, false otherwise</returns>
        public static bool IsTransientException(Exception ex)
        {
            // Do NOT retry cancellation exceptions - they represent intentional cancellation
            if (ex is TaskCanceledException || ex is OperationCanceledException)
                return false;

            // Retry on timeout or temporary database/network issues
            return ex is TimeoutException
                || (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false)
                || (ex.Message?.Contains("network", StringComparison.OrdinalIgnoreCase) ?? false)
                || (ex.Message?.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ?? false)
                || (ex.Message?.Contains("connection", StringComparison.OrdinalIgnoreCase) ?? false)
                || (ex.InnerException != null && IsTransientException(ex.InnerException));
        }

        /// <summary>
        /// Calculates exponential backoff delay based on attempt number.
        /// Formula: 2^(attempt-1) * 50ms
        /// Example: Attempt 1 = 50ms, Attempt 2 = 100ms, Attempt 3 = 200ms
        /// </summary>
        /// <param name="attempt">Current attempt number (1-based)</param>
        /// <returns>Delay in milliseconds</returns>
        private static int CalculateExponentialBackoff(int attempt)
        {
            return (int)Math.Pow(2, attempt - 1) * 50; // 50ms, 100ms, 200ms, 400ms...
        }
    }
}
