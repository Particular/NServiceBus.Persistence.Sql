namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Behavior when requesting timeouts
    /// </summary>
    public enum TimeoutRequestBehavior
    {
        /// <summary>
        /// Cancel all previous timeouts of a given type.
        /// </summary>
        CancelPrevious,
        /// <summary>
        /// Do not cancel previous timeouts. Just schedule a new one.
        /// </summary>
        ScheduleNew
    }
}