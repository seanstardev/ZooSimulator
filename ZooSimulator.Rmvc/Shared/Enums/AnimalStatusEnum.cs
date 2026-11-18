namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>Authoritative simulation status, independent of visual animation.</summary>
    public enum AnimalStatusEnum
    {
        /// <summary>Alive and able to walk.</summary>
        Alive,
        /// <summary>An elephant awaiting recovery to at least 70% health.</summary>
        UnableToWalk,
        /// <summary>Dead; subsequent feeding and decay cannot change the animal.</summary>
        Dead
    }
}
