namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class LoggerTest: UnitTestFor<ILogger>
    {
        [TestMethod]
        public void InformationHasPrefix()
        {
            SystemUnderTest.Information("hello world");

            var fakeStream = Container.Resolve<ILogStream>();
            fakeStream
                .Received()
                .WriteLine("Information: hello world");
        }

        [TestMethod]
        public void IndentationWorks()
        {
            using (SystemUnderTest.Indent())
            {
                SystemUnderTest.Information("inside indentation");
                using (SystemUnderTest.Indent())
                    SystemUnderTest.Information("deep inside indentation");
            }

            SystemUnderTest.Information("outside indentation");

            var fakeStream = Container.Resolve<ILogStream>();
            fakeStream
                .Received()
                .WriteLine("    Information: deep inside indentation");
            fakeStream
                .Received()
                .WriteLine("  Information: inside indentation");
            fakeStream
                .Received()
                .WriteLine("Information: outside indentation");
        }
    }
}