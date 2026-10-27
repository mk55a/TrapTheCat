namespace TrapTheCat.Core
{
    public sealed class LevelValidationResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }
        public int ShortestEscapeSteps { get; private set; }
        public int ReachableExits { get; private set; }

        public LevelValidationResult(
            bool isValid,
            string message,
            int shortestEscapeSteps,
            int reachableExits)
        {
            IsValid = isValid;
            Message = message;
            ShortestEscapeSteps = shortestEscapeSteps;
            ReachableExits = reachableExits;
        }
    }
}
