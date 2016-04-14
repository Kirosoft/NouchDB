using System;
using System.Diagnostics;
using LevelDB;
using LitJson;
using System.Collections.Generic;

namespace NDB
{
    public class NouchDB 
    {
        private const string DOC_STORE = "document-store";
        private const string BY_SEQ_STORE = "by-sequence";
        private const string ATTACH_STORE = "attach-store";
        private const string ATTACH_BINARY_STORE = "attach-binary-store";
        public const string BASE_PATH = "\\temp\\NouchDB.Data\\";

        bool opened = false;
        private Options defaultDBOptions = new Options();
        public string storeName { get; private set; }

        private long sequenceCount = 0;
        private long docCount = 0;

        private string storeBasePath;
        private DB docStore = null;
        private DB attachStore = null;
        private DB attachBinaryStore = null;
        private DB sequenceStore = null;

        private DocCounter docCounter = null;
        private SequenceCounter sequenceCounter = null;

        public NouchDB()
        {
            storeName = "NDB-" + new Random().Next(0, int.MaxValue).ToString();
            Open(storeName);
        }

        public NouchDB(string storeName, string basePath = BASE_PATH)
        {
            Open(storeName, basePath);
        }

        public void Open(string storeName, string basePath = BASE_PATH, Dictionary<string, string> options = null)
        {
            this.storeName = storeName;
            DBLockManager manager = DBLockManager.Instance;

            if (!basePath.EndsWith("\\"))
            {
                basePath += "\\";
            }
            this.storeBasePath = basePath;

            // stores the revision history for a given document
            docStore = manager.GetDB(basePath + storeName+"\\docStore");
  
            // stores the specific revision id and data for a given document
            sequenceStore = manager.GetDB(basePath + storeName + "\\sequenceStore");

            attachStore = manager.GetDB(basePath + storeName + "\\attachStore");
            attachBinaryStore = manager.GetDB(basePath + storeName + "\\attachBinaryStore");

            docCounter = new DocCounter( basePath + storeName);
            docCount = docCounter.Get();
            sequenceCounter = new SequenceCounter(basePath + storeName);
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
            //DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\docStore");
            //DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\sequenceStore");
            //DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\attachStore");
            //DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName + "\\attachBinaryStore");
            //DB.Destroy(options.GetDBOptions(), BASE_PATH + storeName);

        }

        public string Changes(long lastUpdateSequence = 0)
        {
            string result = "";
            int count = 0;

            for (long f = lastUpdateSequence; f < sequenceCount; f++)
            {
                string seq = sequenceStore.Get(ReadOptions.Default, f.ToString()).ToString();
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
                Slice docInfoString = "";
                // If possible try and retrieve any existing doc info for this id

                if (!docStore.TryGet(ReadOptions.Default, docId, out docInfoString)) 
                {
                    // Insert mode: create a new root document
                    docInfo = new Node(docId, data, sequenceCount);
                    docCount = docCounter.Next();
                }
                else
                {
                    // Update mode: create a new revision entry
                    docInfo = Node.Parse(docInfoString.ToString());
                    // add a new document version
                    docInfo.addVersion(data, sequenceCount);
                }

                // Store info about the document in the doc store
                //Debug.WriteLine(docInfo.Dump());
                string docInfoData = docInfo.ToJson();
                docInfoData = docInfoData.Replace("\"", "");

                docStore.Put(WriteOptions.Default, docId, docInfoData);

                // store a unique uuid linked with the doc id data
                sequenceStore.Put(WriteOptions.Default, sequenceCount.ToString(), docInfo.getLatestSig() + "+" + data);
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
                string docInfoString = docStore.Get(ReadOptions.Default,docId).ToString();

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

                docStore.Put(WriteOptions.Default, docId, docInfoData);

                // store a unique uuid linked with the doc id data
                sequenceStore.Put(WriteOptions.Default, sequenceCount.ToString(), docInfo.getLatestSig() + "+" + data);
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

            return JsonMapper.ToJson(infoObj);
        }

        public string GetDoc(string docId)
        {
            string result = "";

            string docInfoString = docStore.Get(ReadOptions.Default, docId).ToString();
            Node docInfo = Node.Parse(docInfoString);

            long latestSequence = docInfo.getLatestSequence();
            result = sequenceStore.Get(ReadOptions.Default, latestSequence.ToString()).ToString();
            result = result.Split('+')[1];

            return result;
        }


        public long GetLastSync(string uri)
        {
            long result = 0;

            try
            {
                DBLockManager lockManager = DBLockManager.Instance;

                DB store = lockManager.GetDB(BASE_PATH +this.storeName);

                result = Convert.ToInt64(store.Get(ReadOptions.Default, uri));
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
            JsonData docs = remoteDB.GetChanges(server, database, true, lastSync);

            foreach (JsonData result in docs["results"])
            {
                string id = (string)result["id"];
                string rev = (string)result["changes"][0]["rev"];
                //    //result.Remove("_rev");
                //    //result.Remove("_id");

                string doc = result["doc"].ToJson();

                try
                {
                    PutDoc(id, doc, rev);
                }
                catch (Exception ee)
                {
                    Debug.WriteLine(ee.ToString());
                }

            }
            docsSync = docs["results"].Count;
            ///Debug.WriteLine("Document sync: " + docs.results.Count);
            ////Debug.WriteLine(AllDocs());
            SetLastSync(server+"/"+database,(int)docs["last_seq"]);
            
            docs = null;
            remoteDB = null;

            return docsSync;
        }

        public void SetLastSync(string uri,long lastSync)
        {
            try
            {
                DBLockManager lockManager = DBLockManager.Instance;

                DB store = lockManager.GetDB(BASE_PATH + this.storeName);

                store.Put(WriteOptions.Default, uri,lastSync.ToString());
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.ToString());
            }

        }

        public Node GetDocInfo(string docId, string rev_id = "")
        {
            Node docInfo = null;

            string docInfoString = docStore.Get(ReadOptions.Default, docId).ToString();

            if (docInfoString != null) { 
                docInfo = Node.Parse(docInfoString);
            }
            
            return docInfo;
        }


        public string AllDocs()
        {
            string result = "";
            LevelDB.Iterator sequenceList = sequenceStore.NewIterator(ReadOptions.Default);

            while (sequenceList.Valid())
            {
                result += sequenceList.Value();
            }

            // We have to close the iterator
            sequenceList = null;

            return result;
        }

    }
}

