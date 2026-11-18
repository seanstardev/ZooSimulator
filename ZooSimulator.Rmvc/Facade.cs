using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Composes the zoo application independently of its native host and renderer.</summary>
    /// <remarks>Create through RFacade.Create, attach IMainView, then raise its Ready event.</remarks>
    public sealed class Facade : RFacade
    {
        /// <summary>Serializes simulation operations across a reset boundary.</summary>
        protected override IRCommandLock[] RegisterCommandLocks()
            =>
            [
                new SimulationLock()
            ];

        /// <summary>Registers state and owned environmental capabilities.</summary>
        protected override IRModel[] RegisterModels()
            =>
            [
                new ZooModel(),
                new RandomProxy(),
                new ClockProxy()
            ];

        /// <summary>Registers composition, enclosure, clock and feeding presentation boundaries.</summary>
        protected override RMediator[] RegisterMediators()
            =>
            [
                new MainMediator(),
                new ZooMediator(),
                new ClockMediator(),
                new FeedingMediator()
            ];

        /// <summary>Creates the initial population before ordinary operations are admitted.</summary>
        protected override RCommandBase CreateInitialiseCommand(IRInitialisation? initialisation)
            => new InitialiseCmd();
    }
}
