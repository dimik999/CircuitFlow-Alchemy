using CircuitFlowAlchemy.Core.Interfaces;

namespace CircuitFlowAlchemy.Core.Interfaces
{
    /// <summary>
    /// Менеджер управления фабрикой
    /// </summary>
    public interface IFactoryManager
    {
        /// <summary>
        /// Разместить машину на фабрике
        /// </summary>
        bool PlaceMachine(IProductionMachine machine, UnityEngine.Vector3 position);
        
        /// <summary>
        /// Удалить машину с фабрики
        /// </summary>
        bool RemoveMachine(string machineId);
        
        /// <summary>
        /// Получить машину по ID
        /// </summary>
        IProductionMachine GetMachine(string machineId);
        
        /// <summary>
        /// Получить все машины на фабрике
        /// </summary>
        System.Collections.Generic.IEnumerable<IProductionMachine> GetAllMachines();
    }
}
