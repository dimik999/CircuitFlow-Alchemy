using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Core.Interfaces
{
    /// <summary>
    /// Сервис управления ресурсами (эссенциями)
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Добавить эссенцию в хранилище
        /// </summary>
        void AddEssence(Essence essence);
        
        /// <summary>
        /// Получить количество эссенции определённого типа
        /// </summary>
        float GetEssenceAmount(EssenceType type);
        
        /// <summary>
        /// Потратить эссенцию
        /// </summary>
        bool ConsumeEssence(EssenceType type, float amount);
        
        /// <summary>
        /// Проверить наличие достаточного количества эссенции
        /// </summary>
        bool HasEnoughEssence(EssenceType type, float amount);
    }
}
