NouchDB
=======

Embedded .NET NoSQL database that will sync with a CouchDB server


* .NET 3.5+/Mono 2.6+ compatible
* Embedded high performance key/value store based upon levelDB
* Master -> Slave replication with a CouchDB server
* MVCC (multiversion concurrency control)
* Atomic commit support
* Simple POCO class persistance model


Uses:

leveldb from google as a high performance low level key/value store
leveldb-sharp to convert level db 'c' module into a .net compatible assembly
servicestack.text for fast Json serialisation/deserialisation


Eventual consistency/Offline operation/Master-Slave synchronisation

This project aims to build high performance no-sql database that can be embedded locally within .NET/mono projects.
The master-slave MVCC replication model guarantees eventual data consistency. This is handy if you would like a DB
to continue operating while a network connection to the remote master database is offline and then automatically 
synchronise once the master database becomes available once more.



V0.1 - Limitations (apart from being a very early alpha release)

* Currently only master-slave replication is supported.
* The master-slave replication model will basically prioritise server data over any local data.
* Although it is planned to supported nested conflicting revisions - this is currently not fully supported


Currently Supported Model:

----------------
|Couchdb server|<----------|
----------------           |
      |                    |
      |                    |
      | replication        |  couchdb - http API
      |                    |
      V                    |
----------------           |
| Client       | --------->|
----------------

Data commits from the client are written directly to the remote DB instance and then the localDB is updated
locally via replication.


Local database commit example
-----------------------------

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
            
            
Replication with remote server example (local database name is "fred", remote is stars)
----------------------------------------------------------------------------------------

 NouchDB.NouchDB nouchDB = new NouchDB.NouchDB("fred");
//nouchDB.Delete(new Options());
nouchDB.Open(new Options(), "fred");

Debug.WriteLine("Documents synchronised: "+Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984","stars")));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars",true)));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
//Debug.WriteLine(nouchDB.AllDocs());


















