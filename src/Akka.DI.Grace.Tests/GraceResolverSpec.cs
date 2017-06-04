using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.DI.Core;
using Akka.DI.TestKit;
using Grace.DependencyInjection;

namespace Akka.DI.Grace.Tests
{
    public class GraceResolverSpec : DiResolverSpec
    {
        protected override object NewDiContainer()
        {
            return new DependencyInjectionContainer();
        }

        protected override IDependencyResolver NewDependencyResolver(object diContainer, ActorSystem system)
        {
            var grace = diContainer as IInjectionScope;
            return new GraceDependencyResolver(grace, system);
        }

        protected override void Bind<T>(object diContainer, Func<T> generator)
        {
            var grace = ToContainer(diContainer);
            grace.Configure(c => c.ExportFactory<T>(generator).As<T>());
        }

        protected override void Bind<T>(object diContainer)
        {
            var grace = ToContainer(diContainer);
            grace.Configure(c => c.Export<T>());
        }

        private static IInjectionScope ToContainer(object diContainer)
        {
            var grace = diContainer as IInjectionScope;
            Debug.Assert(grace != null, "grace != null");
            return grace;
        }
    }
}
