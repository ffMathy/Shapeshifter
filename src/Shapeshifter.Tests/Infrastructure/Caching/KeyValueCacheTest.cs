namespace Shapeshifter.WindowsDesktop.Infrastructure.Caching
{
    using System;
    using System.Threading.Tasks;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class KeyValueCacheTest: UnitTestFor<IKeyValueCache<string, int>>
    {
        [TestMethod]
        public void GetForUnknownKeyInStringCacheReturnsNull()
        {
            Assert.IsNull(systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void GetForUnknownKeyInIntegerCacheReturnsZero()
        {
            Assert.AreEqual(0, systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ValueWithKeyCanBeFetchedFromKeyAgain()
        {
            systemUnderTest.Set("foobar", 123);
            Assert.AreEqual(123, systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void CallingSetOnExistingKeyUpdatesValue()
        {
            systemUnderTest.Set("foobar", 123);
            systemUnderTest.Set("foobar", 456);
            Assert.AreEqual(456, systemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ThunkifyCachesFunctionArguments()
        {
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