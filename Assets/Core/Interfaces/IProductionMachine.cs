using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Core.Interfaces
{
    /// <summary>
    /// Состояние машины производства
    /// </summary>
    public enum MachineState
    {
        Idle,      // Ожидание
        Working,   // Работает
        Broken,    // Сломана
        Paused     // Приостановлена
    }
    
    /// <summary>
    /// Интерфейс машины производства эссенций
    /// </summary>
    public interface IProductionMachine
    {
        string MachineId { get; }
        MachineState State { get; }
        Recipe CurrentRecipe { get; }
        float ProductionProgress { get; }
        
        /// <summary>
        /// Начать производство по рецепту
        /// </summary>
        bool StartProduction(Recipe recipe);
        
        /// <summary>
        /// Обновить состояние машины (вызывается каждый кадр)
        /// </summary>
        void UpdateMachine(float deltaTime);
        
        /// <summary>
        /// Остановить производство
        /// </summary>
        void StopProduction();
        
        /// <summary>
        /// Получить результат производства
        /// </summary>
        Essence GetOutput();
    }
}
