using System.Collections.Generic;
using System.Linq;
using CircuitFlowAlchemy.Core.Interfaces;

namespace CircuitFlowAlchemy.Features.FactoryBuilding
{
    /// <summary>
    /// Реализация менеджера фабрики
    /// </summary>
    public class FactoryManager : IFactoryManager
    {
        private readonly Dictionary<string, IProductionMachine> _machines = new();
        private readonly Dictionary<string, UnityEngine.Vector3> _machinePositions = new();
        
        public bool PlaceMachine(IProductionMachine machine, UnityEngine.Vector3 position)
        {
            if (machine == null || _machines.ContainsKey(machine.MachineId))
                return false;
            
            _machines[machine.MachineId] = machine;
            _machinePositions[machine.MachineId] = position;
            
            return true;
        }
        
        public bool RemoveMachine(string machineId)
        {
            if (!_machines.ContainsKey(machineId))
                return false;
            
            _machines.Remove(machineId);
            _machinePositions.Remove(machineId);
            
            return true;
        }
        
        public IProductionMachine GetMachine(string machineId)
        {
            return _machines.ContainsKey(machineId) ? _machines[machineId] : null;
        }
        
        public IEnumerable<IProductionMachine> GetAllMachines()
        {
            return _machines.Values;
        }
        
        public UnityEngine.Vector3 GetMachinePosition(string machineId)
        {
            return _machinePositions.ContainsKey(machineId) 
                ? _machinePositions[machineId] 
                : UnityEngine.Vector3.zero;
        }
    }
}
