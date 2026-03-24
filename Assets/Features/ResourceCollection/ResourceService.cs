using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Features.ResourceCollection
{
    /// <summary>
    /// Реализация сервиса управления ресурсами
    /// </summary>
    public class ResourceService : IResourceService
    {
        private readonly Dictionary<EssenceType, float> _essenceStorage = new();
        
        public void AddEssence(Essence essence)
        {
            if (_essenceStorage.ContainsKey(essence.Type))
            {
                _essenceStorage[essence.Type] += essence.Amount;
            }
            else
            {
                _essenceStorage[essence.Type] = essence.Amount;
            }
        }
        
        public float GetEssenceAmount(EssenceType type)
        {
            return _essenceStorage.ContainsKey(type) ? _essenceStorage[type] : 0f;
        }
        
        public bool ConsumeEssence(EssenceType type, float amount)
        {
            if (!HasEnoughEssence(type, amount))
                return false;
            
            _essenceStorage[type] -= amount;
            if (_essenceStorage[type] <= 0f)
            {
                _essenceStorage.Remove(type);
            }
            
            return true;
        }
        
        public bool HasEnoughEssence(EssenceType type, float amount)
        {
            return GetEssenceAmount(type) >= amount;
        }
    }
}
