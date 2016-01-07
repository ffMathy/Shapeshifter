namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using WindowsDesktop;

    using Shared.Infrastructure.Logging.Interfaces;

    [TestClass]
    public class LoggerTest: TestBase
    {
        [TestMethod]
        public void InformationHasPrefix()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<ILogStream>();
                });

            var logger = container.Resolve<ILogger>();
            logger.Information("hello world");

            var fakeStream = container.Resolve<ILogStream>();
            fakeStream
                .Received()
                .WriteLine("Information: hello world");
        }

        [TestMethod]
        public void IndentationWorks()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<ILogStream>();
                });

            var logger = container.Resolve<ILogger>();
            using (logger.Indent())
            {
                logger.Information("inside indentation");
                using(logger.Indent())
                {
                    logger.Information("deep inside indentation");
                }
            }

            logger.Information("outside indentation");

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