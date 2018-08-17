namespace CircuitBreakerPattern 
{
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is currently operational
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit has been tripped
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is attempting a reset
        /// </summary>
        HalfOpen
    }
}
