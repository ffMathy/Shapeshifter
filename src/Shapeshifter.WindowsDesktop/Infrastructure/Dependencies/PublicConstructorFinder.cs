namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Autofac.Core.Activators.Reflection;

	class PublicConstructorFinder : IConstructorFinder
	{
		public ConstructorInfo[] FindConstructors(Type targetType)
		{
			return targetType
				.GetConstructors()
				.Where(x => x.IsPublic)
				.Where(x => AreAllParametersValid(x.GetParameters()))
				.OrderByDescending(x => x
					.GetParameters()
					.Length)
				.ToArray();
		}

		static bool AreAllParametersValid(ParameterInfo[] getParameters)
		{
			return getParameters.All(x => x.ParameterType != typeof(string));
		}
	}
}