using Autofac;
using NSubstitute;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;

namespace Shapeshifter.WindowsDesktop
{
	public abstract class TestBase
    {
        protected static ILifetimeScope CreateContainer(
            Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();

            var fakeEnvironment = builder.RegisterFake<IEnvironmentInformation>();

            fakeEnvironment
                .GetIsInDesignTime()
                .Returns(false);

            fakeEnvironment
                .GetIsDebugging()
                .Returns(true);

            builder.RegisterModule(
                new DefaultWiringModule(fakeEnvironment));

            setupCallback?.Invoke(builder);

            var result = builder
                .Build()
                .BeginLifetimeScope();

            return result;
        }
		
		protected IClipboardDataPackage CreateClipboardDataPackageContaining<TData>()
			where TData : class, IClipboardData
		{
			var fakePackage = Substitute.For<IClipboardDataPackage>();
			fakePackage.Contents.Returns(
				new IClipboardData[]
				{
					Substitute.For<TData>()
				});

			return fakePackage;
		}

		protected IClipboardDataPackage CreateClipboardDataPackageContaining<TData>(params TData[] data)
			where TData : class, IClipboardData
		{
			var fakePackage = Substitute.For<IClipboardDataPackage>();
			fakePackage.Contents.Returns(data);

			return fakePackage;
		}

		protected IClipboardFormat CreateClipboardFormatFromNumber(uint number)
		{
			var fake = Substitute.For<IClipboardFormat>();
			fake.Number.Returns(number);
			
			return fake;
		}

		protected IClipboardFormat CreateClipboardFormatFromName(string name)
		{
			var fake = Substitute.For<IClipboardFormat>();
			fake.Name.Returns(name);

			return fake;
		}
	}
}
