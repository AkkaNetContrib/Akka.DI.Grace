using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DI.Core;
using Grace.DependencyInjection;

namespace Akka.DI.Grace
{
    /// <summary>
    /// Provides services to the <see cref="ActorSystem "/> extension system
    /// used to create actors using the Grace IoC container.
    /// </summary>
    public class GraceDependencyResolver : IDependencyResolver
    {
        private IInjectionScope container;
        private ConcurrentDictionary<string, Type> typeCache;
        private ActorSystem system;
        private ConditionalWeakTable<ActorBase, IInjectionScope> references;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraceDependencyResolver"/> class.
        /// </summary>
        /// <param name="container">The container used to resolve references</param>
        /// <param name="system">The actor system to plug into</param>
        /// <exception cref="ArgumentNullException">
        /// Either the <paramref name="container"/> or the <paramref name="system"/> was null.
        /// </exception>
        public GraceDependencyResolver(IInjectionScope container, ActorSystem system)
        {
            if (system == null) throw new ArgumentNullException("system");
            if (container == null) throw new ArgumentNullException("container");
            this.container = container;
#if NET45
            typeCache = new ConcurrentDictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
#else
            typeCache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
#endif
            this.system = system;
            this.system.AddDependencyResolver(this);
            this.references = new ConditionalWeakTable<ActorBase, IInjectionScope>();
        }

        /// <summary>
        /// Retrieves an actor's type with the specified name
        /// </summary>
        /// <param name="actorName">The name of the actor to retrieve</param>
        /// <returns>The type with the specified actor name</returns>
        public Type GetType(string actorName)
        {
            typeCache.TryAdd(actorName, actorName.GetTypeValue());

            return typeCache[actorName];

        }

        /// <summary>
        /// Creates a delegate factory used to create actors based on their type
        /// </summary>
        /// <param name="actorType">The type of actor that the factory builds</param>
        /// <returns>A delegate factory used to create actors</returns>
        public Func<ActorBase> CreateActorFactory(Type actorType)
        {
            return () =>
            {
                var nestedContainer = container.CreateChildScope();
                var actor = (ActorBase)nestedContainer.Locate(actorType);
                references.Add(actor, nestedContainer);
                return actor;
            };
        }

        /// <summary>
        /// Used to register the configuration for an actor of the specified type <typeparamref name="TActor"/>
        /// </summary>
        /// <typeparam name="TActor">The type of actor the configuration is based</typeparam>
        /// <returns>The configuration object for the given actor type</returns>
        public Props Create<TActor>() where TActor : ActorBase
        {
            return Create(typeof(TActor));
        }

        /// <summary>
        /// Used to register the configuration for an actor of the specified type <paramref name="actorType"/> 
        /// </summary>
        /// <param name="actorType">The <see cref="Type"/> of actor the configuration is based</param>
        /// <returns>The configuration object for the given actor type</returns>
        public virtual Props Create(Type actorType)
        {
            return system.GetExtension<DIExt>().Props(actorType);
        }

        /// <summary>
        /// Signals the container to release it's reference to the actor.
        /// </summary>
        /// <param name="actor">The actor to remove from the container</param>
        public void Release(ActorBase actor)
        {
            IInjectionScope nestedContainer;

            if (references.TryGetValue(actor, out nestedContainer))
            {
                nestedContainer.Dispose();
                references.Remove(actor);
            }
        }
    }
}
