using System;
using System.Threading.Tasks;

namespace CircuitBreakerPattern
{
    public abstract class CircuitBreaker
    {
        private readonly TimeSpan resetTimeoutThreshold;
        private readonly int maximumNumberOfAttempts;

        // TODO: Need to persist this
        private CircuitBreakerState currentState = CircuitBreakerState.Closed;
        private DateTime lastTrip = DateTime.MinValue;

        protected CircuitBreaker(TimeSpan resetTimeoutThreshold, int maximumNumberOfAttempts)
        {
            this.resetTimeoutThreshold = resetTimeoutThreshold;
            this.maximumNumberOfAttempts = maximumNumberOfAttempts;
        }

        protected async Task<T> Protect<T>(Func<Task<T>> operationUnderLoad, Func<Task<T>> failOverOperation)
        {
            var circuitBreakerState = GetCurrentState();
            if (circuitBreakerState == CircuitBreakerState.Open)
                return await failOverOperation();

            if (circuitBreakerState == CircuitBreakerState.HalfOpen)
            {
                try
                {
                    return await operationUnderLoad();
                }
                catch (Exception e)
                {
                    Trip();
                    return await failOverOperation();
                }
            }

            // else CircuitBreakerState == Open
            var attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;
                    return await operationUnderLoad();
                }
                catch (Exception e)
                {
                    if (attempt < maximumNumberOfAttempts)
                        continue;

                    Trip();
                    return await failOverOperation();
                }
            }
        }

        private CircuitBreakerState GetCurrentState()
        {
            // Currently operational
            if (currentState == CircuitBreakerState.Closed)
                return currentState;

            // Currently in tripped state
            var timeSinceLastTrip = DateTime.UtcNow - lastTrip;

            if (timeSinceLastTrip > resetTimeoutThreshold)
                return CircuitBreakerState.HalfOpen;

            return CircuitBreakerState.Open;
        }

        private void Trip()
        {
            currentState = CircuitBreakerState.Open;
            lastTrip = DateTime.UtcNow;
        }
    }
}
