namespace CircuitFlowAlchemy.Core.Interfaces
{
    /// <summary>
    /// Состояние игры
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        Building,
        GameOver
    }
    
    /// <summary>
    /// Сервис управления состоянием игры
    /// </summary>
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        
        /// <summary>
        /// Изменить состояние игры
        /// </summary>
        void ChangeState(GameState newState);
        
        /// <summary>
        /// Событие изменения состояния
        /// </summary>
        System.Action<GameState, GameState> OnStateChanged { get; set; }
    }
}
