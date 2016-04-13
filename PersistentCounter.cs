using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LevelDB;

namespace NouchDB
{
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

        public PersistentCounter(string storePath)
        {
            Init(new Options(), storePath);
        }

        public PersistentCounter(Options options, string storePath)
        {

            Init(options, storePath);
        }

        public void Init(Options options, string storePath)
        {
            DBLockManager lockManager = DBLockManager.Instance;

            store = lockManager.GetDB(options, storePath);

        }

        protected virtual long Get(string key)
        {

            string res = store.Get(ReadOptions.Default, key).ToString();

            counter = Convert.ToInt64(res);

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


        public DocCounter(Options options, string storePath)
            : base(options, storePath)
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

        public SequenceCounter(Options options, string storePath)
            : base(options, storePath)
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
