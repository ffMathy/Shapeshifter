namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Autofac;

	using Data.Factories.Interfaces;
	using Data.Interfaces;

	using Files.Interfaces;

	using Interfaces;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using NSubstitute;

	[TestClass]
	public class ClipboardPersistanceServiceTest : UnitTestFor<IClipboardPersistenceService>
	{

		[TestMethod]
		public async Task CanPersistClipboardData()
		{
			Container.Resolve<IFileManager>()
				.PrepareIsolatedFolder(Arg.Any<string>())
				.Returns("preparedFolder");

			var clipboardFormat1337 = CreateClipboardFormatFromNumber(1337u);
			var clipboardFormat1338 = CreateClipboardFormatFromNumber(1338u);

			var fakeData1 = Substitute.For<IClipboardData>()
									  .With(
										  x => {
											  x.RawFormat.Returns(clipboardFormat1337);
											  x.RawData.Returns(
												  new byte[]
												  {
													  1,
													  2
												  });
										  });
			var fakeData2 = Substitute.For<IClipboardData>()
									  .With(
										  x => {
											  x.RawFormat.Returns(clipboardFormat1338);
											  x.RawData.Returns(
												  new byte[]
												  {
													  3,
													  4
												  });
										  });

			var fakePackageId = Guid.NewGuid();

			var fakePackage = Substitute.For<IClipboardDataPackage>();
			fakePackage.Id.Returns(fakePackageId);
			fakePackage
				.Contents
				.Returns(
					new List<IClipboardData>(
						new[]
						{
							fakeData1,
							fakeData2
						}));

			var service = Container.Resolve<IClipboardPersistenceService>();
			await service.PersistClipboardPackageAsync(fakePackage);

			var fakeFileManager = Container.Resolve<IFileManager>();
			fakeFileManager
				.Received()
				.WriteBytesToFileAsync(
					@"preparedFolder\1.1337",
					fakeData1.RawData)
				.IgnoreAwait();
			fakeFileManager
				.Received()
				.WriteBytesToFileAsync(
					@"preparedFolder\2.1338",
					fakeData2.RawData)
				.IgnoreAwait();
		}

		[TestCategory("Integration")]
		[TestMethod]
		public async Task CanFetchPersistedPackages()
		{
			IncludeFakeFor<IClipboardDataFactory>();

			ExcludeFakeFor<IClipboardDataPackageFactory>();
			ExcludeFakeFor<IFileManager>();

			var clipboardFormat1337 = CreateClipboardFormatFromNumber(1337u);
			var clipboardFormat1338 = CreateClipboardFormatFromNumber(1338u);

			Container.Resolve<IClipboardDataFactory>()
			 .With(
				 x => {
					 x.CanBuildData(Arg.Any<IClipboardFormat>())
					  .Returns(true);

					 x.BuildData(
						 Arg.Any<IClipboardFormat>(),
						 Arg.Any<byte[]>())
					  .Returns(
						  r => {
							  var resultingData = Substitute.For<IClipboardData>();
							  resultingData
								  .RawData
								  .Returns(
									  r.Arg<byte[]>());
							  resultingData
								  .RawFormat
								  .Returns(
									  r.Arg<IClipboardFormat>());
							  return resultingData;
						  });
				 });

			var fakeData1 = Substitute.For<IClipboardData>()
									  .With(
										  x => {
											  x.RawFormat.Returns(clipboardFormat1337);
											  x.RawData.Returns(
												  new byte[]
												  {
													  1,
													  2
												  });
										  });
			var fakeData2 = Substitute.For<IClipboardData>()
									  .With(
										  x => {
											  x.RawFormat.Returns(clipboardFormat1338);
											  x.RawData.Returns(
												  new byte[]
												  {
													  3,
													  4
												  });
										  });

			var fakePackage1 = Substitute.For<IClipboardDataPackage>();
			fakePackage1.Id.Returns(Guid.NewGuid());
			fakePackage1
				.Contents
				.Returns(
					new List<IClipboardData>(
						new[]
						{
							fakeData1,
							fakeData2
						}));

			var fakePackage2 = Substitute.For<IClipboardDataPackage>();
			fakePackage2.Id.Returns(Guid.NewGuid());
			fakePackage2
				.Contents
				.Returns(
					new List<IClipboardData>(
						new[]
						{
							fakeData2,
							fakeData1
						}));

			await SystemUnderTest.PersistClipboardPackageAsync(fakePackage1);
			await SystemUnderTest.PersistClipboardPackageAsync(fakePackage2);

			var persistedPackages = await SystemUnderTest.GetPersistedPackagesAsync();
			var persistedPackagesArray = persistedPackages.ToArray();

			Assert.AreEqual(2, persistedPackagesArray.Length);

			var persistedPackage1 = persistedPackagesArray[0];
			var persistedPackage2 = persistedPackagesArray[1];

			Assert.AreEqual(fakePackage1.Id, persistedPackage1.Id);
			Assert.AreEqual(fakePackage2.Id, persistedPackage2.Id);

			Assert.AreEqual(
				1,
				persistedPackage1
					.Contents[0]
					.RawData[0]);
			Assert.AreEqual(
				2,
				persistedPackage1
					.Contents[0]
					.RawData[1]);
			Assert.AreEqual(
				3,
				persistedPackage1
					.Contents[1]
					.RawData[0]);
			Assert.AreEqual(
				4,
				persistedPackage1
					.Contents[1]
					.RawData[1]);

			Assert.AreEqual(
				3,
				persistedPackage2
					.Contents[0]
					.RawData[0]);
			Assert.AreEqual(
				4,
				persistedPackage2
					.Contents[0]
					.RawData[1]);
			Assert.AreEqual(
				1,
				persistedPackage2
					.Contents[1]
					.RawData[0]);
			Assert.AreEqual(
				2,
				persistedPackage2
					.Contents[1]
					.RawData[1]);
		}
	}
}