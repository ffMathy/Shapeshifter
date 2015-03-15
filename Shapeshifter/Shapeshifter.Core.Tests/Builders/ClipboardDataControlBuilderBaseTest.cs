using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.Core.Builders;
using Shapeshifter.Core.Strategies;
using Moq;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Controls;

namespace Shapeshifter.Core.Tests
{

    [TestClass]
    public class ClipboardDataControlBuilderBaseTest
    {
        [TestMethod]
        public void TestCanBuild()
        {
            var dataSourceMock = new Mock<IDataSource>();
            dataSourceMock.Setup(d => d.Text).Returns("some source text");

            var clipboardDataMock = new Mock<IClipboardData>();
            clipboardDataMock.Setup(c => c.Source).Returns(dataSourceMock.Object);

            var headerBuildingStrategyMock = new Mock<IControlConstructionStrategy<string, IClipboardData>>();
            headerBuildingStrategyMock.Setup(b => b.ConstructControl(clipboardDataMock.Object)).Returns("header stuff");

            var backgroundImageBuildingStrategyMock = new Mock<IControlConstructionStrategy<string, IClipboardData>>();
            backgroundImageBuildingStrategyMock.Setup(b => b.ConstructControl(clipboardDataMock.Object)).Returns("background image stuff");

            var bodyBuildingStrategyMock = new Mock<IControlConstructionStrategy<string, IClipboardData>>();
            bodyBuildingStrategyMock.Setup(b => b.ConstructControl(clipboardDataMock.Object)).Returns("body stuff");

            var sourceBuildingStrategyMock = new Mock<IControlConstructionStrategy<string, IClipboardData>>();
            sourceBuildingStrategyMock.Setup(b => b.ConstructControl(clipboardDataMock.Object)).Returns<IClipboardData>(d => d.Source.Text);

            var builderMock = new Mock<ClipboardDataControlBuilderBaseDummy>();
            builderMock.Setup(x => x.CanBuild(clipboardDataMock.Object)).Returns(true);
            builderMock.CallBase = true;

            var builder = builderMock.Object;
            builder.SetBackgroundImageBuildingStrategy(backgroundImageBuildingStrategyMock.Object);
            builder.SetBodyBuildingStrategy(bodyBuildingStrategyMock.Object);
            builder.SetHeaderBuildingStrategy(headerBuildingStrategyMock.Object);
            builder.SetSourceBuildingStrategy(sourceBuildingStrategyMock.Object);

            Assert.IsTrue(builder.CanBuild(clipboardDataMock.Object));

            var control = builder.Build(clipboardDataMock.Object);
            Assert.IsNotNull(control);

            dataSourceMock.Verify(x => x.Text, Times.Once);

            clipboardDataMock.Verify(x => x.Source, Times.Once);

            headerBuildingStrategyMock.Verify(x => x.ConstructControl(clipboardDataMock.Object), Times.Once);
            backgroundImageBuildingStrategyMock.Verify(x => x.ConstructControl(clipboardDataMock.Object), Times.Once);
            bodyBuildingStrategyMock.Verify(x => x.ConstructControl(clipboardDataMock.Object), Times.Once);
            sourceBuildingStrategyMock.Verify(x => x.ConstructControl(clipboardDataMock.Object), Times.Once);

            Assert.AreEqual("some source text", control.Source);
            Assert.AreEqual("background image stuff", control.BackgroundImage);
            Assert.AreEqual("header stuff", control.Header);
            Assert.AreEqual("body stuff", control.Body);
        }

        public class ClipboardItemControlDummy : IClipboardItemControl<string>
        {
            public string BackgroundImage { get; set; }

            public string Body { get; set; }

            public string Header { get; set; }

            public string Source { get; set; }
        }

        public class ClipboardDataControlBuilderBaseDummy : ClipboardDataControlBuilderBase<string, IClipboardData>
        {
            private IControlConstructionStrategy<string, IClipboardData> backgroundImageBuildingStrategy;
            protected override IControlConstructionStrategy<string, IClipboardData> BackgroundImageBuildingStrategy
            {
                get
                {
                    return backgroundImageBuildingStrategy;
                }
            }

            private IControlConstructionStrategy<string, IClipboardData> bodyBuildingStrategy;
            protected override IControlConstructionStrategy<string, IClipboardData> BodyBuildingStrategy
            {
                get
                {
                    return bodyBuildingStrategy;
                }
            }

            private IControlConstructionStrategy<string, IClipboardData> headerBuildingStrategy;
            protected override IControlConstructionStrategy<string, IClipboardData> HeaderBuildingStrategy
            {
                get
                {
                    return headerBuildingStrategy;
                }
            }

            private IControlConstructionStrategy<string, IClipboardData> sourceBuildingStrategy;
            protected override IControlConstructionStrategy<string, IClipboardData> SourceBuildingStrategy
            {
                get
                {
                    return sourceBuildingStrategy;
                }
            }

            public void SetBodyBuildingStrategy(IControlConstructionStrategy<string, IClipboardData> strategy)
            {
                bodyBuildingStrategy = strategy;
            }

            public void SetHeaderBuildingStrategy(IControlConstructionStrategy<string, IClipboardData> strategy)
            {
                headerBuildingStrategy = strategy;
            }

            public void SetBackgroundImageBuildingStrategy(IControlConstructionStrategy<string, IClipboardData> strategy)
            {
                backgroundImageBuildingStrategy = strategy;
            }

            public void SetSourceBuildingStrategy(IControlConstructionStrategy<string, IClipboardData> strategy)
            {
                sourceBuildingStrategy = strategy;
            }
        }
    }
}