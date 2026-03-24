namespace CircuitFlowAlchemy.Core.Models
{
    /// <summary>
    /// Модель магической эссенции с физическими свойствами
    /// </summary>
    public class Essence
    {
        public EssenceType Type { get; set; }
        public float Amount { get; set; }
        public float Purity { get; set; } // Чистота от 0 до 1
        public float Temperature { get; set; }
        public float Viscosity { get; set; }
        
        public Essence(EssenceType type, float amount, float purity = 1.0f)
        {
            Type = type;
            Amount = amount;
            Purity = purity;
            Temperature = GetDefaultTemperature(type);
            Viscosity = GetDefaultViscosity(type);
        }
        
        private float GetDefaultTemperature(EssenceType type)
        {
            return type switch
            {
                EssenceType.Ignis => 1000f,
                EssenceType.Aqua => 20f,
                EssenceType.Terra => 25f,
                EssenceType.Aeris => 15f,
                EssenceType.Vitus => 37f,
                EssenceType.Fulgar => 5000f,
                _ => 20f
            };
        }
        
        private float GetDefaultViscosity(EssenceType type)
        {
            return type switch
            {
                EssenceType.Ignis => 0.8f,  // Густой, медленный
                EssenceType.Aqua => 0.1f,   // Быстрая, текучая
                EssenceType.Terra => 0.9f,  // Зернистая
                EssenceType.Aeris => 0.0f,  // Газообразный
                EssenceType.Vitus => 0.5f,  // Растущий
                EssenceType.Fulgar => 0.3f, // Нестабильная
                _ => 0.5f
            };
        }
    }
}
