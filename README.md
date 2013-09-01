NouchDB

I really like the CouchDB NoSQL database but I needed a lightweight embeddable database that would sync with a 
master CouchDB database (the Erlang framework that CouchbDB relies on is a non-starter as an embedded system IMHO).
PouchDB is great but is web only (javascript). I could not find .NET version of PouchDB so this project is
designed to fill my (probably) very niche requirement. I also needed Mono 2.6 compatability to work 
within a Unity3D project. My plan is to use this module as the basis of network synchronisable objects in a 
Unity3D MMO game client (so no funky .NET 4+ async calls allowed). I will probably release a Unity version of this
in a few weeks....


* .NET 3.5+/Mono 2.6+ compatible
* Embedded high performance key/value store based upon levelDB
* Master -> Slave replication with a CouchDB server
* MVCC - multiversion concurrency control - each update causes a new record commit. No data is lost we just keep track  
  of the latest revision
* Atomic commit support via leveldb (supports batch commits)
* Simple POCO class persistance model (uses service stack POCO/json serialisation methods)

In principle this is portable to all platforms that can support Mono but I am currently focused on Windows as a platform.

Dependendant projects:

leveldb from google as a high performance low level key/value store (cross platform 'c' module) - https://code.google.com/p/leveldb/
leveldb-sharp to convert level db 'c' module into a .NET compatible assembly (not yet tested on Mono) - https://github.com/meebey/leveldb-sharp
servicestack.text for fast Json serialisation/deserialisation - https://github.com/ServiceStack/ServiceStack.Text


Eventual consistency/Offline operation/Master-Slave synchronisation

This project aims to build high performance no-sql database that can be embedded locally within .NET/mono projects.
The master-slave MVCC replication model guarantees eventual data consistency. This is handy if you would like a DB
to continue operating with a local database while a network connection to a remote master database is offline 
and then automatically synchronise once the master database becomes available once more.

https://docs.google.com/a/kirosoft.co.uk/drawings/d/szsQ3jNOUSQQ1g1blxzTeAw/image?w=657&h=460&rev=228&ac=1


V0.1 - Limitations

* it is an early alpha so most things have not been fully tested and many probably desireable features are missing
* Currently only master-slave replication is supported.
* The master-slave replication model will basically prioritise server data over any local data.
* Although it is planned to supported nested conflicting revisions - this is currently not fully implemented (see the Node class)


Currently Supported features:


Data commits from the client are written directly to the remote DB instance and then the localDB is updated
locally via replication. So, in this model the server is the master and many slaves remain consistent with the server
data (as required). Locally committed data is currently lost when writtem locally (no slave master replication), but the
master server can be updated via the couchdb http api.


Local database commit example

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

 NouchDB.NouchDB nouchDB = new NouchDB.NouchDB("fred");
//nouchDB.Delete(new Options());
nouchDB.Open(new Options(), "fred");

Debug.WriteLine("Documents synchronised: "+Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984","stars")));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars",true)));
Debug.WriteLine("Documents synchronised: " + Convert.ToString(nouchDB.ReplicateWith("http://127.0.0.1:5984", "stars")));
//Debug.WriteLine(nouchDB.AllDocs());

