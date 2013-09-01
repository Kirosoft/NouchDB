﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NouchDB;
using ServiceStack.Text;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace NouchDBTests
{
    [TestClass]
    public class NouchDBTests
    {
        [TestMethod]
        public void SimpleInsertAndUpdate()
        {


            NouchDB.NouchDB nouchDB = new NouchDB.NouchDB("fred");

            nouchDB.Delete(new Options());

            nouchDB.Open(new Options(), "fred");

            var customer = new Customer { name = "Joe Bloggs", age = 31 };

            string input = customer.ToJson();
            nouchDB.PutDoc("doc_id1", input);
            string output = nouchDB.GetDoc("doc_id1");
            Assert.AreEqual(input, output);

            string res = nouchDB.Info();

            var customer2 = new Customer { name = "Fred", age = 55 };
            input = customer2.ToJson();
            nouchDB.PutDoc("doc_id1", input);
            output = nouchDB.GetDoc("doc_id1");
            Assert.AreEqual(input, output);

            long[] obj = nouchDB.Info().FromJson<long[]>() ;
            Assert.AreEqual(1, obj[0]); // 1 doc
            Assert.AreEqual(2, obj[1]); // 2 sequences
        }

        [TestMethod]
        public void RemoteTest()
        {
            RemoteDB remoteDB = new RemoteDB();
            remoteDB.GetAllDocuments("http://127.0.0.1:5984", "test");

        }

        [TestMethod]
        public void ReplicationTest()
        {
            RemoteDB remoteDB = new RemoteDB();

            //DocInfo[] info = remoteDB.GetAllDocuments("http://127.0.0.1:5984", "test");

            NouchDB.NouchDB nouchDB = new NouchDB.NouchDB("fred");
            //nouchDB.Delete(new Options());
            nouchDB.Open(new Options(), "fred");
            //Debug.WriteLine(nouchDB.Changes());
            //string res = RevsDiff(nouchDB, remoteDB, info);
            //res = "{\"keys\":" + res + "}";

            long lastSync = nouchDB.GetLastSync("http://127.0.0.1:5984/test");

            ChangesSync docs = remoteDB.GetChanges("http://127.0.0.1:5984", "test", true, lastSync);

            //JsonData data = JsonMapper.ToObject(allDocs);

            foreach (Results result in docs.results)
            {
                string id = result.id;
                string rev = result.doc[0]["_rev"];
                result.doc[0].Remove("_rev");
                result.doc[0].Remove("_id");

                string doc = result.doc[0].ToJson() ;

                try
                {
                    nouchDB.PutDoc(id, doc, rev);
                }
                catch (Exception ee)
                {
                    Debug.WriteLine(ee.ToString());
                }
            }

            Debug.WriteLine("Document sync: " + docs.results.Count);
            Debug.WriteLine(nouchDB.AllDocs());
            nouchDB.SetLastSync("http://127.0.0.1:5984/test",docs.last_seq);

            //nouchDB.Close();
        }

        [TestMethod]
        public void ReplicationTest1()
        {
            NouchDB.NouchDB nouchDB = new NouchDB.NouchDB("fred");
            //nouchDB.Delete(new Options());
            nouchDB.Open(new Options(), "fred");

            Debug.WriteLine("Documents synchronised: "+Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984","stars")));
            Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
            Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars",true)));
            Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
            //Debug.WriteLine(nouchDB.AllDocs());
        }

        public string RevsDiff(NouchDB.NouchDB db, RemoteDB remoteDB, DocInfo[] docInfo)
        {
            var result = "[";
            Dictionary<string,string> docList = new Dictionary<string,string>();

            for (int f = 0; f < docInfo.Length; f++ )
            {
                DocInfo remoteDoc = docInfo[f];
                long revision = Convert.ToInt64(remoteDoc.Revision.Split('-')[0]);
                Node localDoc = db.GetDocInfo(remoteDoc.ID);

                if (localDoc == null)
                {
                    if (Convert.ToInt64(localDoc.currentVersion) < revision)
                    {
                        if (!docList.ContainsKey(remoteDoc.ID))
                        {
                            docList.Add(remoteDoc.ID,"");
                        }
                    }

                }
                else
                {
                    if (!docList.ContainsKey(remoteDoc.ID))
                    {
                        docList.Add(remoteDoc.ID, "");
                    }
                }

            }

            return docList.Keys.SerializeToString();
        }

        [TestMethod]
        public void TestTree1()
        {

            //string docId = "test";
            //Customer customer = new Customer { name = "Joe Bloggs", age = 31 };
            //string documentData = customer.ToJson();

            //// Create a new root node for a document tree
            //DocInfo rootNode = new DocInfo(docId, documentData,0);

            ////rootNode.children.Add(0, new DocInfo("test", 0));


            //Dictionary<int, DocInfo> children = new Dictionary<int, DocInfo>();

            //var txt = rootNode.ToJson();

            //DocInfo back = txt.FromJson<DocInfo>();


        }

    }

    public class Customer 
    {
        public string name { set; get; }
        public int age { set; get; }
    }

}