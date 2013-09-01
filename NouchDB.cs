using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LevelDB;
using System.Diagnostics;
using System.Collections;

// JSON serialisation services
using ServiceStack.Text;

namespace NouchDB
{
    public class NouchDB 
    {
        const string DOC_STORE = "document-store";
        const string BY_SEQ_STORE = "by-sequence";
        const string ATTACH_STORE = "attach-store";
        const string ATTACH_BINARY_STORE = "attach-binary-store";
        const string BASE_PATH = "c:\\temp\\";

        bool opened = false;
        private Options options = null;
        string storeName = "";

        private long sequenceCount = 0;
        private long docCount = 0;

        private DB docStore = null;
        private DB attachStore = null;
        private DB attachBinaryStore = null;
        private DB sequenceStore = null;

        DocCounter docCounter = null;
        SequenceCounter sequenceCounter = null;

        public NouchDB()
        {
        }

        public NouchDB(string storeName)
        {
            this.storeName = storeName;

        }

        public void Open(Options options, string storeName) {
            this.options = options;
            this.storeName = storeName;

            DBLockManager manager = DBLockManager.Instance;
            
            // stores the revision history for a given document
            docStore = manager.GetDB(options, BASE_PATH+storeName+"\\docStore");
  
            // stores the specific revision id and data for a given document
            sequenceStore = manager.GetDB(options, BASE_PATH + storeName + "\\sequenceStore");

            attachStore = manager.GetDB(options, BASE_PATH + storeName + "\\attachStore");
            attachBinaryStore = manager.GetDB(options, BASE_PATH + storeName + "\\attachBinaryStore");

            docCounter = new DocCounter(options, BASE_PATH + storeName);
            docCount = docCounter.Get();
            sequenceCounter = new SequenceCounter(options, BASE_PATH + storeName);
            sequenceCount= sequenceCounter.Get();

            opened = true;
        }

        public void Close()
        {
            DBLockManager manager = DBLockManager.Instance;
            manager.Close( BASE_PATH + storeName + "\\docStore");
            manager.Close(BASE_PATH + storeName + "\\sequenceStore");
            manager.Close(BASE_PATH + storeName + "\\attachStore");
            manager.Close(BASE_PATH + storeName + "\\attachBinaryStore");
            manager.Close(BASE_PATH + storeName);
        }

        public void Delete(Options options)
        {
            DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\docStore");
            DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\sequenceStore");
            DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\attachStore");
            DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\attachBinaryStore");
            DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName);

        }


        public string Changes(long lastUpdateSequence = 0)
        {
            string result = "";
            int count = 0;

            for (long f = lastUpdateSequence; f < sequenceCount; f++)
            {
                string seq = sequenceStore.Get(f.ToString());
                string change = seq.Split('+')[0];
                string id = change.Split(',')[0];
                string rev = change.Split(',')[1];

                if (count++ > 0)
                    result += ",";
                result += "{seq:"+f.ToString()+",id:"+id+",changes:[{rev:"+rev+"}]}";

            }
            
            return result;
        }


        /// <summary>
        /// Update or insert a new document into the database
        /// </summary>
        /// <param name="docId">ID of the document to store or update</param>
        /// <param name="data">string data for the associated document</param>
        /// <param name="previousRev">Optionally specify the revision to update. By default will assume a new revision
        /// if the revision document specifies and older revision then a sub version of the old document revision will be
        /// created </param>
        public void PutDoc(string docId, string data)
        {
            Node docInfo = null;

            try
            {
                // If possible try and retrieve any existing doc info for this id
                string docInfoString = docStore.Get(docId);

                if (docInfoString == null)
                {
                    // Insert mode: create a new root document
                    docInfo = new Node(docId, data, sequenceCount);
                    docCount = docCounter.Next();
                }
                else
                {
                    // Update mode: create a new revision entry
                    docInfo = Node.Parse(docInfoString);
                    // add a new document version
                    docInfo.addVersion(data, sequenceCount);
                }

                // Store info about the document in the doc store
                //Debug.WriteLine(docInfo.Dump());
                string docInfoData = docInfo.ToJson();
                docInfoData = docInfoData.Replace("\"", "");

                docStore.Put(docId, docInfoData);

                // store a unique uuid linked with the doc id data
                sequenceStore.Put(sequenceCount.ToString(), docInfo.getLatestSig()+"+"+data);
                sequenceCount = sequenceCounter.Next();

            }
            catch (Exception ee)
            {
                throw ee;
            }
            finally
            {
            }


        }

        /// <summary>
        /// Update or insert a new document into the database
        /// </summary>
        /// <param name="docId">ID of the document to store or update</param>
        /// <param name="data">string data for the associated document</param>
        /// <param name="revId">specific version add by revId </param>
        public void PutDoc(string docId, string data, string revId)
        {
            Node docInfo = null;

            try
            {
                // If possible try and retrieve any existing doc info for this id
                string docInfoString = docStore.Get(docId);

                if (docInfoString == null)
                {
                    // Insert mode: create a new root document
                    docInfo = new Node(docId, data, sequenceCount,revId);
                    docCount = docCounter.Next();
                }
                else
                {
                    // Update mode: create a new revision entry
                    docInfo = Node.Parse(docInfoString);
                    // add a new document version
                    if (docInfo.addVersion(data, sequenceCount, revId) == null)
                    {
                        return;
                    }
                }

                // Store info about the document in the doc store
                //Debug.WriteLine(docInfo.Dump());
                string docInfoData = docInfo.ToJson();
                docInfoData = docInfoData.Replace("\"", "");

                docStore.Put(docId, docInfoData);

                // store a unique uuid linked with the doc id data
                sequenceStore.Put(sequenceCount.ToString(), docInfo.getLatestSig() + "+" + data);
                sequenceCount = sequenceCounter.Next();

            }
            catch (Exception ee)
            {
                throw ee;
            }
            finally
            {
            }


        }

        public string Info()
        {
            var infoObj = new []{ docCount, sequenceCount };

            return infoObj.ToJson();
        }

        public string GetDoc(string docId)
        {
            string result = "";

            string docInfoString = docStore.Get(docId);
            Node docInfo = Node.Parse(docInfoString);

            long latestSequence = docInfo.getLatestSequence();
            result = sequenceStore.Get(latestSequence.ToString());
            result = result.Split('+')[1];

            return result;
        }


        public long GetLastSync(string uri)
        {
            long result = 0;

            try
            {
                DBLockManager lockManager = DBLockManager.Instance;

                DB store = lockManager.GetDB(new Options(), BASE_PATH +this.storeName);

                result = Convert.ToInt64(store.Get(uri));

                

            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.ToString());
            }

            return result;
        }

        public long ReplicateWith(string server, string database, bool reset = false)
        {
            long docsSync = 0;

            RemoteDB remoteDB = new RemoteDB();

            long lastSync = GetLastSync(server+"/"+database);
            if (reset)
            {
                lastSync = 0;
            }
            ChangesSync docs = remoteDB.GetChanges(server, database, true, lastSync);

            foreach (Results result in docs.results)
            {
                string id = result.id;
                string rev = result.doc[0]["_rev"];
                result.doc[0].Remove("_rev");
                result.doc[0].Remove("_id");

                string doc = result.doc[0].ToJson() ;

                try
                {
                    PutDoc(id, doc, rev);
                }
                catch (Exception ee)
                {
                    Debug.WriteLine(ee.ToString());
                }
            }

            docsSync = docs.results.Count;
            //Debug.WriteLine("Document sync: " + docs.results.Count);
            //Debug.WriteLine(AllDocs());
            SetLastSync(server+"/"+database,docs.last_seq);
            
            docs = null;
            remoteDB = null;

            return docsSync;
        }

        public void SetLastSync(string uri,long lastSync)
        {

            try
            {
                DBLockManager lockManager = DBLockManager.Instance;

                DB store = lockManager.GetDB(new Options(), BASE_PATH + this.storeName);

                store.Put(uri,lastSync.ToString());

            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.ToString());
            }

        }

        public Node GetDocInfo(string docId, string rev_id = "")
        {
            Node docInfo = null;

            string docInfoString = docStore.Get(docId);

            if (docInfoString != null) { 
                docInfo = Node.Parse(docInfoString);
            }
            
            return docInfo;
        }


        public string AllDocs()
        {
            string result = "";

            IEnumerator<KeyValuePair<string, string>> sequenceList = sequenceStore.GetEnumerator();

            while (sequenceList.MoveNext())
            {
                Debug.WriteLine(sequenceList.Current.Value);
            }
            
            return result;
        }
    
    }
    
    public class Options
    {
        public bool createIfMissing = true;
        public string valueEncoding = "json";
        public string name = "";

        // Build a LevelDb.Options object from this one
        public LevelDB.Options GetDBOptions()
        {
            LevelDB.Options opts = new LevelDB.Options();

            if (createIfMissing)
                opts.CreateIfMissing = true;

            return opts;
        }


    }


}

