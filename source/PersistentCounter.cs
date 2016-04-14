using System;
using LevelDB;
using System.Collections.Generic;

namespace NDB
{
    // Manages a persistent counter within a store/db
    // Note: this class operates as a cache value for a single counter
    // The store may contain other counters
    public class PersistentCounter
    {
        private DB store = null;
        private string storePath = "";
        private long counter = 0;

        public PersistentCounter()
        {
        }

        void Dispose()
        {
            if (store != null)
            {
                Close();
            }
        }

        public PersistentCounter(string storePath, string key, Dictionary<string, string> options = null)
        {
            Init(storePath, key, options);
        }

        public void Init(string storePath, string key, Dictionary<string, string> options = null)
        {
            DBLockManager lockManager = DBLockManager.Instance;
            store = lockManager.GetDB(storePath);
            this.storePath = storePath;
        }

        protected virtual long Get(string key)
        {
            Slice outValue = new Slice();
            outValue = "0";

            if (!store.TryGet(ReadOptions.Default, key, out outValue))
            {
                // Key does not exist, initialise to 0
                store.Put(WriteOptions.Default, key, outValue);
            }
            counter = Convert.ToInt64(outValue.ToString());

            return counter;
        }

        protected virtual long Next(string key)
        {
            counter++;
            store.Put(WriteOptions.Default, key, Convert.ToString(counter));

            return counter;
        }

        protected virtual void Reset(string key)
        {
            counter = 0;
            store.Put(WriteOptions.Default, key, Convert.ToString(counter));
        }

        public virtual void Close()
        {
            if (store != null)
            {
                DBLockManager lockManager = DBLockManager.Instance;
                lockManager.Close(storePath);
                store = null;
            }
        }
    }

    public class DocCounter : PersistentCounter
    {
        private const string DOC_COUNT_KEY = "_local_doc_count";


        public DocCounter(string storePath, Dictionary<string, string> options = null)
            : base(storePath, DOC_COUNT_KEY, options)
        {
            Get(DOC_COUNT_KEY);
        }

        public long Get()
        {

            return base.Get(DOC_COUNT_KEY);
        }

        public long Next()
        {
            return base.Next(DOC_COUNT_KEY);
        }

        public void Reset()
        {
            base.Reset(DOC_COUNT_KEY);
            return;
        }


    }

    public class SequenceCounter : PersistentCounter
    {
        private const string UPDATE_SEQ_KEY = "_local_last_update_seq";

        public SequenceCounter(string storePath, Dictionary<string, string> options = null)
            : base(storePath, UPDATE_SEQ_KEY, options)
        {
            Get(UPDATE_SEQ_KEY);
        }

        public long Get()
        {
            return base.Get(UPDATE_SEQ_KEY);
        }

        public long Next()
        {
            return base.Next(UPDATE_SEQ_KEY);
        }

        public void Reset()
        {
            base.Reset(UPDATE_SEQ_KEY);
            return;
        }
    }

}
