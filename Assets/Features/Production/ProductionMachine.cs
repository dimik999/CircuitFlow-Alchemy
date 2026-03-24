using System;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Features.Production
{
    /// <summary>
    /// Базовая реализация машины производства
    /// </summary>
    public class ProductionMachine : IProductionMachine
    {
        public string MachineId { get; private set; }
        public MachineState State { get; private set; }
        public Recipe CurrentRecipe { get; private set; }
        public float ProductionProgress { get; private set; }
        
        private float _currentProductionTime;
        private Essence _output;
        
        public ProductionMachine(string machineId)
        {
            MachineId = machineId;
            State = MachineState.Idle;
        }
        
        public bool StartProduction(Recipe recipe)
        {
            if (State != MachineState.Idle || recipe == null)
                return false;
            
            CurrentRecipe = recipe;
            State = MachineState.Working;
            _currentProductionTime = 0f;
            ProductionProgress = 0f;
            _output = null;
            
            return true;
        }
        
        public void UpdateMachine(float deltaTime)
        {
            if (State != MachineState.Working || CurrentRecipe == null)
                return;
            
            _currentProductionTime += deltaTime;
            ProductionProgress = _currentProductionTime / CurrentRecipe.ProductionTime;
            
            if (ProductionProgress >= 1.0f)
            {
                CompleteProduction();
            }
        }
        
        private void CompleteProduction()
        {
            _output = new Essence(
                CurrentRecipe.OutputEssence,
                CurrentRecipe.OutputAmount,
                1.0f
            );
            
            State = MachineState.Idle;
            ProductionProgress = 1.0f;
        }
        
        public void StopProduction()
        {
            if (State == MachineState.Working)
            {
                State = MachineState.Idle;
                CurrentRecipe = null;
                _currentProductionTime = 0f;
                ProductionProgress = 0f;
            }
        }
        
        public Essence GetOutput()
        {
            var output = _output;
            _output = null;
            return output;
        }
        
        /// <summary>
        /// Фабрика для создания ProductionMachine
        /// </summary>
        public interface IFactory
        {
            ProductionMachine Create(string machineId);
        }
        
        /// <summary>
        /// Реализация фабрики для создания ProductionMachine
        /// </summary>
        public class Factory : IFactory
        {
            public ProductionMachine Create(string machineId)
            {
                return new ProductionMachine(machineId);
            }
        }
    }
}
