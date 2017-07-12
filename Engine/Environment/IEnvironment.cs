namespace QuantConnect.Lean.Engine.Environment
{
    public interface IEnvironment
    {
        void Create(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers);
    }
}
