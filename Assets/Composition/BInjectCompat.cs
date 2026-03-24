using System;
using System.Collections.Generic;
using UnityEngine;

namespace BInject
{
    /// <summary>
    /// Minimal compatibility container used by existing composition code.
    /// </summary>
    public class DiContainer
    {
        private readonly DiContainer _parent;
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        public DiContainer(DiContainer parent = null)
        {
            _parent = parent;
        }

        public BindingBuilder<TContract> Bind<TContract>() where TContract : class
        {
            return new BindingBuilder<TContract>(this);
        }

        public T Resolve<T>() where T : class
        {
            var contractType = typeof(T);
            if (_registrations.TryGetValue(contractType, out var resolver))
            {
                return (T)resolver();
            }

            if (_parent != null)
            {
                return _parent.Resolve<T>();
            }

            throw new InvalidOperationException($"Type is not registered: {contractType.FullName}");
        }

        public DiContainer CreateSubContainer()
        {
            return new DiContainer(this);
        }

        internal void RegisterSingleton<TContract, TImplementation>()
            where TContract : class
            where TImplementation : class, TContract, new()
        {
            TImplementation instance = null;
            _registrations[typeof(TContract)] = () =>
            {
                if (instance == null)
                {
                    instance = new TImplementation();
                }

                return instance;
            };
        }
    }

    public sealed class BindingBuilder<TContract> where TContract : class
    {
        private readonly DiContainer _container;

        public BindingBuilder(DiContainer container)
        {
            _container = container;
        }

        public ScopeBuilder<TContract, TImplementation> To<TImplementation>()
            where TImplementation : class, TContract, new()
        {
            return new ScopeBuilder<TContract, TImplementation>(_container);
        }
    }

    public sealed class ScopeBuilder<TContract, TImplementation>
        where TContract : class
        where TImplementation : class, TContract, new()
    {
        private readonly DiContainer _container;

        public ScopeBuilder(DiContainer container)
        {
            _container = container;
        }

        public void AsSingle()
        {
            _container.RegisterSingleton<TContract, TImplementation>();
        }
    }

    public abstract class MonoInstaller : MonoBehaviour
    {
        protected DiContainer Container { get; private set; }

        public virtual void InstallBindings()
        {
        }

        public void InstallBindings(DiContainer container)
        {
            Container = container;
            InstallBindings();
        }
    }
}
