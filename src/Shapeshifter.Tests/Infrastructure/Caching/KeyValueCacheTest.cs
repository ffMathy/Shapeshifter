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
        public void GetForUnknownKeyInIntegerCacheReturnsZero()
        {
            Assert.AreEqual(0, SystemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ValueWithKeyCanBeFetchedFromKeyAgain()
        {
            SystemUnderTest.Set("foobar", 123);
            Assert.AreEqual(123, SystemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void CallingSetOnExistingKeyUpdatesValue()
        {
            SystemUnderTest.Set("foobar", 123);
            SystemUnderTest.Set("foobar", 456);
            Assert.AreEqual(456, SystemUnderTest.Get("foobar"));
        }

        [TestMethod]
        public void ThunkifyCachesFunctionArguments()
        {
            var value = 0;
            var action = new Func<string, int>(input => value += input.Length);

            var result1 = SystemUnderTest.Thunkify("hello world", action);
            var result2 = SystemUnderTest.Thunkify("hello world", action);
            var result3 = SystemUnderTest.Thunkify("hello world", action);

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

            var result1 = await SystemUnderTest.ThunkifyAsync("hello world", action);
            var result2 = await SystemUnderTest.ThunkifyAsync("hello world", action);
            var result3 = await SystemUnderTest.ThunkifyAsync("hello world", action);

            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            Assert.AreEqual("hello world".Length, result1);
        }
    }
}