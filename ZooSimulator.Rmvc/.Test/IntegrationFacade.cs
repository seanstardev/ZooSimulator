using RMVC;

namespace ZooSimulator.Rmvc.Tests
{
    // A direct RMVC composition with the production model, mediator, commands and clock.
    // Only the owned scheduler and random capability are replaced with deterministic fixtures.
    public sealed class IntegrationFacade : RFacade
    {
        internal static readonly ZooModel Zoo = new();
        internal static readonly ScriptedRandomProxy Random = new();
        internal static readonly ManualScheduler Scheduler = new();

        protected override IRCommandLock[] RegisterCommandLocks()
            =>
            [
                new SimulationLock()
            ];

        protected override IRModel[] RegisterModels()
            =>
            [
                Zoo,
                Random,
                new ClockProxy(Scheduler)
            ];

        protected override RMediator[] RegisterMediators()
            =>
            [
                new MainMediator(),
                new ZooMediator(),
                new ClockMediator(),
                new FeedingMediator()
            ];

        protected override RCommandBase CreateInitialiseCommand(IRInitialisation? initialisation)
            => new InitialiseCmd();
    }
}
