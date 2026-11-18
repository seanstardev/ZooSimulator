using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Model.Zoo
{
    /// <summary>A monkey dies immediately when health falls strictly below 30%.</summary>
    internal sealed class Monkey : Animal
    {
        public Monkey(int id) : base(id, AnimalSpeciesEnum.Monkey) { }

        protected override void EvaluateHealth(long hour)
        {
            if (base.Health < 30)
                base.Status = AnimalStatusEnum.Dead;
        }
    }
}
