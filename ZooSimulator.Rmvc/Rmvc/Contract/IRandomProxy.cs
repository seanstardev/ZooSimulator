using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Replaceable pseudo-random percentage generation.</summary>
    internal interface IRandomProxy : IRModel
    {
        /// <summary>Returns an integer percentage including both requested endpoints.</summary>
        int NextPercentage(int minimumInclusive, int maximumInclusive);
    }
}
