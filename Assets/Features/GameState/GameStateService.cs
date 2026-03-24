using CircuitFlowAlchemy.Core.Interfaces;
using GameStateEnum = CircuitFlowAlchemy.Core.Interfaces.GameState;

namespace CircuitFlowAlchemy.Features.GameState
{
    /// <summary>
    /// Реализация сервиса управления состоянием игры
    /// </summary>
    public class GameStateService : IGameStateService
    {
        public GameStateEnum CurrentState { get; private set; } = GameStateEnum.MainMenu;
        public System.Action<GameStateEnum, GameStateEnum> OnStateChanged { get; set; }
        
        public void ChangeState(GameStateEnum newState)
        {
            if (CurrentState == newState)
                return;
            
            var previousState = CurrentState;
            CurrentState = newState;
            
            OnStateChanged?.Invoke(previousState, newState);
        }
    }
}
