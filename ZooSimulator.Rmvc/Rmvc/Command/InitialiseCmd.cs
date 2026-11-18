using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Establishes the initial zoo before ordinary roots can run.</summary>
    internal sealed class InitialiseCmd : CommandBase<RVoid>
    {
        protected override Task<RVoid> Run()
        {
            base.ZooModel.Initialise();

            return Complete();
        }
    }
}
