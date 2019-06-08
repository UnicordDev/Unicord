namespace DSharpPlus
{
    /// <summary>
    /// Represents a server's premium tier.
    /// </summary>
    public enum PremiumTier : int
    {
        /// <summary>
        /// Indicates that this server was not boosted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that this server was boosted two times.
        /// </summary>
        Tier1 = 1,

        /// <summary>
        /// Indicates that this server was boosted ten times.
        /// </summary>
        Tier2 = 2,

        /// <summary>
        /// Indicates that this server was boosted fifty times.
        /// </summary>
        Tier3 = 3,

        /// <summary>
        /// Indicates an unknown premium tier.
        /// </summary>
        Unknown = int.MaxValue
    }
}