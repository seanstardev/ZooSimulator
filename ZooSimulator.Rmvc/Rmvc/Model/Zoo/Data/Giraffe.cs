using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Model.Zoo
{
    /// <summary>A giraffe dies immediately when health falls strictly below 50%.</summary>
    internal sealed class Giraffe : Animal
    {
        public Giraffe(int id) : base(id, AnimalSpeciesEnum.Giraffe) { }

        protected override void EvaluateHealth(long hour)
        {
            if (base.Health < 50)
                base.Status = AnimalStatusEnum.Dead;
        }
    }
}
