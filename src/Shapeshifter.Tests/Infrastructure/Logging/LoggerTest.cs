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
            systemUnderTest.Information("hello world");

            var fakeStream = container.Resolve<ILogStream>();
            fakeStream
                .Received()
                .WriteLine("Information: hello world");
        }

        [TestMethod]
        public void IndentationWorks()
        {
            using (systemUnderTest.Indent())
            {
                systemUnderTest.Information("inside indentation");
                using (systemUnderTest.Indent())
                    systemUnderTest.Information("deep inside indentation");
            }

            systemUnderTest.Information("outside indentation");

            var fakeStream = container.Resolve<ILogStream>();
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