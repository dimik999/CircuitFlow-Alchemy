using System.Collections.Generic;

namespace CircuitFlowAlchemy.Core.Models
{
    /// <summary>
    /// Рецепт создания эссенции или артефакта
    /// </summary>
    public class Recipe
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<EssenceType, float> InputEssences { get; set; }
        public EssenceType OutputEssence { get; set; }
        public float OutputAmount { get; set; }
        public float ProductionTime { get; set; }
        public float RequiredPurity { get; set; } // Минимальная чистота входных эссенций
        
        public Recipe()
        {
            InputEssences = new Dictionary<EssenceType, float>();
        }
    }
}
