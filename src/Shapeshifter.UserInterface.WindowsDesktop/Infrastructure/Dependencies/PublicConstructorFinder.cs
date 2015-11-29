namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Autofac.Core.Activators.Reflection;

    class PublicConstructorFinder: IConstructorFinder
    {
        public ConstructorInfo[] FindConstructors(Type targetType)
        {
            return targetType.GetConstructors()
                             .Where(x => x.IsPublic)
                             .OrderByDescending(
                                                x => x.GetParameters()
                                                      .Length)
                             .ToArray();
        }
    }
}