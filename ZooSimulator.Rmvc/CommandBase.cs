using System;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>The typed capabilities used to coordinate zoo operations.</summary>
    internal abstract class CommandBase<TResult> : RCommand<TResult>
    {
        protected SimulationLock SimulationLock
            => base.Lock<SimulationLock>();

        protected ZooModel ZooModel
            => base.Model<ZooModel>();

        protected ClockProxy ClockProxy
            => base.Model<ClockProxy>();

        // Randomness is the deliberate substitution point for the scripted test facade.
        protected IRandomProxy RandomProxy
            => base.ModelByContract<IRandomProxy>()
                ?? throw new InvalidOperationException(
                    "The random percentage capability is not uniquely registered.");

        protected ZooMediator ZooMediator
            => base.Mediator<ZooMediator>();

        protected ClockMediator ClockMediator
            => base.Mediator<ClockMediator>();
    }
}
