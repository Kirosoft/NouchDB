using Microsoft.VisualStudio.TestTools.UnitTesting;
using NDB;

namespace NouchDBTests
{
    [TestClass]
    public class CounterClassTests
    {
        [TestMethod]
        public void DocCounterTest()
        {

            string filePath = "c:\\temp\\test";

            DocCounter counter = new DocCounter(filePath);
            counter.Reset();
            long count = counter.Get();
            Assert.AreEqual(0, count);
            count = counter.Next();
            Assert.AreEqual(1, count);
            count = counter.Next();
            Assert.AreEqual(2, count);
            counter.Close();

            counter = null;

            counter = new DocCounter(filePath);
            count = counter.Get();
            Assert.AreEqual(2, count);

        }

        [TestMethod]
        public void SequenceCounterTest()
        {

            string filePath = "c:\\temp\\test";

            SequenceCounter counter = new SequenceCounter(filePath);
            counter.Reset();
            long count = counter.Get();
            Assert.AreEqual(0, count);
            count = counter.Next();
            Assert.AreEqual(1, count);
            count = counter.Next();
            Assert.AreEqual(2, count);
            counter.Close();

            counter = null;

            counter = new SequenceCounter(filePath);
            count = counter.Get();
            Assert.AreEqual(2, count);

        }

        [TestMethod]
        public void DualCounterTest()
        {

            string filePath = "c:\\temp\\test";

            DocCounter docCounter= new DocCounter(filePath);
            SequenceCounter sequenceCounter= new SequenceCounter(filePath);

            docCounter.Reset();
            sequenceCounter.Reset();

            long docCount = docCounter.Get();
            Assert.AreEqual(0, docCount);
            docCount = docCounter.Next();
            Assert.AreEqual(1, docCount);

            long sequenceCount = sequenceCounter.Get();
            Assert.AreEqual(0, sequenceCount);
            sequenceCount = sequenceCounter.Next();
            Assert.AreEqual(1, sequenceCount);

            docCounter.Close();

            docCounter = new DocCounter(filePath);
            docCount = docCounter.Get();
            Assert.AreEqual(1, docCount);

        }

    }
}
