namespace Shapeshifter.WindowsDesktop
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Autofac;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Serilog;

	public abstract class UnitTestFor<TSystemUnderTest> : TestBase
		where TSystemUnderTest : class
	{
		ILifetimeScope container;

		/// <summary>
		/// Represents the container, which has the class of the unit's dependencies automatically registered as fakes. You can use the <see cref="IncludeFakeFor{T}"/> and <see cref="ExcludeFakeFor{T}"/> method to exclude or include fakes other than the ones automatically registered.
		/// </summary>
		protected ILifetimeScope Container
		{
			get
			{
				Setup();
				return container;
			}
		}

		TSystemUnderTest systemUnderTest;

		/// <summary>
		/// Represents the class of the unit being tested, with all its dependencies registered as fakes in the container automatically. You can use the <see cref="IncludeFakeFor{T}"/> and <see cref="ExcludeFakeFor{T}"/> method to exclude or include fakes other than the ones automatically registered.
		/// </summary>
		protected TSystemUnderTest SystemUnderTest
		{
			get
			{
				Setup();
				return systemUnderTest;
			}
		}

		protected TSystem Get<TSystem>()
		{
			return Container.Resolve<TSystem>();
		}

		readonly List<Type> fakeExceptions;
		readonly List<Type> fakeInclusions;

		protected UnitTestFor()
		{
			fakeExceptions = new List<Type>();
			fakeInclusions = new List<Type>();
		}

		protected void ExcludeFakeFor<T>()
		{
			fakeExceptions.Add(typeof(T));
		}

		protected void IncludeFakeFor<T>()
		{
			fakeInclusions.Add(typeof(T));
		}

		void Setup()
		{
			if ((container != null) && (systemUnderTest != null))
			{
				return;
			}

			container = CreateContainerWithFakeDependencies(
				(c) => {
					foreach (var fake in fakeInclusions)
					{
						Extensions.RegisterFake(c, fake);
					}
				},
				fakeExceptions.ToArray());
			systemUnderTest = container.Resolve<TSystemUnderTest>();
		}

		[TestCleanup]
		public void Cleanup()
		{
			Log.CloseAndFlush();

			var appDataPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Shapeshifter");
			if (Directory.Exists(appDataPath))
			{
				Directory.Delete(appDataPath, true);
			}

			var temporaryPath = Path.Combine(
				Path.GetTempPath(),
				"Shapeshifter");
			if (Directory.Exists(temporaryPath))
			{
				Directory.Delete(temporaryPath, true);
			}

			fakeExceptions.Clear();
			fakeInclusions.Clear();

			Extensions.ClearCache();
			DisposeContainer();
		}

		void DisposeContainer()
		{
			container?.Dispose();
			container = null;
		}

		static ILifetimeScope CreateContainerWithFakeDependencies(
			Action<ContainerBuilder> setupCallback,
			params Type[] exceptTypes)
		{
			return CreateContainer(
				c => {
					c.RegisterFakesForDependencies<TSystemUnderTest>(exceptTypes);
					setupCallback(c);
				});
		}
	}
}