namespace Shapeshifter.WindowsDesktop.Infrastructure.Caching
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    [TestClass]
    public class KeyValueCacheTest: TestBase
    {
        [TestMethod]
        public void GetForUnknownKeyInStringCacheReturnsNull()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, string>>();
            Assert.IsNull(systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void GetForUnknownKeyInIntegerCacheReturnsZero()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, int>>();
            Assert.AreEqual(0, systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ValueWithKeyCanBeFetchedFromKeyAgain()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, string>>();
            systemUnderTest.Set("foobar", "hello world");
            Assert.AreEqual("hello world", systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void CallingSetOnExistingKeyUpdatesValue()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, string>>();
            systemUnderTest.Set("foobar", "hello world");
            systemUnderTest.Set("foobar", "hello there world");
            Assert.AreEqual("hello there world", systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ThunkifyCachesFunctionArguments()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, int>>();

            var value = 0;
            var action = new Func<string, int>(input => value += input.Length);

            var result1 = systemUnderTest.Thunkify("hello world", action);
            var result2 = systemUnderTest.Thunkify("hello world", action);
            var result3 = systemUnderTest.Thunkify("hello world", action);

            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            Assert.AreEqual("hello world".Length, result1);
        }

        [TestMethod]
        public async Task ThunkifyAsyncCachesFunctionArguments()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyValueCache<string, int>>();

            var value = 0;
            var action = new Func<string, Task<int>>(
                input => Task.FromResult(value += input.Length));

            var result1 = await systemUnderTest.ThunkifyAsync("hello world", action);
            var result2 = await systemUnderTest.ThunkifyAsync("hello world", action);
            var result3 = await systemUnderTest.ThunkifyAsync("hello world", action);

            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            Assert.AreEqual("hello world".Length, result1);
        }
    }
}