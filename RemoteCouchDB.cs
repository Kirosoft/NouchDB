using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using LitJson;

namespace NouchDB
{

    /// <summary>
    /// Used to return metadata about a document.
    /// </summary>
    public class DocInfo    
    {
        public string ID;
        public string Revision;
        public string Data;
    }

    public class Doc
    {
        public string _id { set; get; }
        public string _rev { set; get; }
    //    public string desc { set; get; }
    }

    public class Revs
    {
        public string rev { set; get; }
    }

    public class Changes
    {
        public List<Revs> revs { set; get; }
    }

    public class Results
    {
        public int seq { set; get; }
        public string id { set; get; }
        public List<Changes> changes { set; get; }
        //public List<Doc> doc { set; get; }
        public JsonData doc { set; get; }
    }
    
    public class ChangesSync
    {
        public List<Results> results { set; get; }
        public int last_seq { set; get; }
    }

    /// <summary>
    /// A simple wrapper class for the CouchDB HTTP API. No
    /// initialisation is necessary, just create an instance and
    /// call the appropriate methods to interact with CouchDB.
    /// All methods throw exceptions when things go wrong.
    /// </summary>
    public class RemoteDB
    {
        public RemoteDB()
        {
        }

        /// <summary>
        /// Get a list of database on the server.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <returns>A string array containing the database names
        /// </returns>
        public string[] GetDatabases(string server)
        {
            string result = DoRequest(server + "/_all_dbs", "GET");

            List<string> list = new List<string>();
            if (result != "[]")
            {
                JsonData d = JsonMapper.ToObject(result);
                foreach (JsonData db in d)
                    list.Add(db.ToString());
            }
            return (list.ToArray());     
        }

        /// <summary>
        /// Get the document count for the given database.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <returns>The number of documents in the database</returns>
        public int CountDocuments(string server, string db)
        {
            // Get information about the database...
            string result = DoRequest(server + "/" + db, "GET");

            // The document count is a field within...
            JsonData d = JsonMapper.ToObject(result);
            int count = int.Parse(d["doc_count"].ToString());
            return count;
        }

        /// <summary>
        /// Get information on all the documents in the given database.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <returns>An array of DocInfo instances</returns>
        public DocInfo[] GetAllDocuments(string server, string db)
        {
            string result = DoRequest(server + "/" + db + "/_all_docs", "GET");

            List<DocInfo> list = new List<DocInfo>();

            JsonData d = JsonMapper.ToObject(result);
            foreach (JsonData row in d["rows"])
            {
                DocInfo doc = new DocInfo();
                doc.ID = row["id"].ToString();
                doc.Revision = (row["value"])["rev"].ToString();
                list.Add(doc);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Get all changes from the database since a given change revision
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="since">sequence number for the last checkpoint, defaults to 0 i.e. all changes</param>
        /// <returns>An array of DocInfo instances</returns>
        public JsonData GetChanges(string server, string db,bool includeDocs = false,long since=0)
        {
            JsonData sync = null;
            string result = DoRequest(server + "/" + db + "/_changes?since=" + since.ToString() + "&include_docs=" + Convert.ToString(includeDocs).ToLower(), "GET");

            sync= JsonMapper.ToObject(result);

            return sync;
        }

        /// <summary>
        /// Create a new database.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        public void CreateDatabase(string server, string db)
        {
            string result = DoRequest(server + "/" + db, "PUT");
            if (result != "{\"ok\":true}")
                throw new ApplicationException("Failed to create database: " + result);
        }

        /// <summary>
        /// Delete a database
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The name of the database to delete</param>
        public void DeleteDatabase(string server, string db)
        {
            string result = DoRequest(server + "/" + db, "DELETE");
            if (result != "{\"ok\":true}")
                throw new ApplicationException("Failed to delete database: " + result);
        }

        /// <summary>
        /// Execute a temporary view and return the results.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="map">The javascript map function</param>
        /// <param name="reduce">The javascript reduce function or
        /// null if not required</param>
        /// <param name="startkey">The startkey or null not to use</param>
        /// <param name="endkey">The endkey or null not to use</param>
        /// <returns>The result (JSON format)</returns>
        public string ExecTempView(string server, string db, string map, string reduce, string startkey, string endkey)
        {
            // Generate the JSON view definition from the supplied
            // map and optional reduce functions...
            string viewdef = "{ \"map\":\"" + map + "\"";
            if (reduce != null)
                viewdef += ",\"reduce\":\"" + reduce + "\"";
            viewdef += "}";

            string url = server + "/" + db + "/_temp_view";
            if (startkey != null)
            {
                url += "?startkey=" + System.Web.HttpUtility.UrlEncode(startkey);
            }
            if (endkey != null)
            {
                if (startkey == null) url += "?"; else url += "&";
                url += "endkey=" + HttpUtility.UrlEncode(endkey);
            }
            return DoRequest(url, "POST", viewdef, "application/json");
        }

        /// <summary>
        /// Create a new document. If the document has no ID field,
        /// it will be assigned one by the server.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="content">The document contents (JSON).</param>
        public void CreateDocument(string server, string db, string content)
        {
            DoRequest(server + "/" + db, "POST", content, "application/json");
        }

        /// <summary>
        /// Get a document.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="docid">The document ID.</param>
        /// <returns>The document contents (JSON)</returns>
        public string GetDocument(string server, string db, string docid)
        {
            return DoRequest(server + "/" + db + "/" + docid, "GET");
        }

        /// <summary>
        /// Get a document.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="docid">The document ID.</param>
        /// <param name="startkey">The startkey or null not to use</param>
        /// <param name="endkey">The endkey or null not to use</param>
        /// <returns>The document contents (JSON)</returns>
        public string GetDocument(string server, string db, string docid, string startkey, string endkey)
        {
            string url = server + "/" + db + "/" + docid;
            if (startkey != null)
            {
                url += "?startkey=" + HttpUtility.UrlEncode(startkey);
            }
            if (endkey != null)
            {
                if (startkey == null) url += "?"; else url += "&";
                url += "endkey=" + HttpUtility.UrlEncode(endkey);
            }
            return DoRequest(url, "GET");
        }


        /// <summary>
        /// Create a new document. If the document has no ID field,
        /// it will be assigned one by the server.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="content">The document contents (JSON).</param>
        public string BatchGetDocs(string server, string db, string content)
        {
            return DoRequest(server + "/" + db+"/_all_docs?include_docs=true", "POST", content, "application/json");
        }


        /// <summary>
        /// Delete a document.
        /// </summary>
        /// <param name="server">The server URL</param>
        /// <param name="db">The database name</param>
        /// <param name="docid">The document ID.</param>
        public void DeleteDocument(string server, string db, string docid)
        {
            DoRequest(server + "/" + db + "/" + docid, "DELETE");
        }

        /// <summary>
        /// Internal helper to make an HTTP request and return the
        /// response. Throws an exception in the event of any kind
        /// of failure. Overloaded - use the other version if you
        /// need to post data with the request.
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="method">The method, e.g. "GET"</param>
        /// <returns>The server's response</returns>
        private string DoRequest(string url, string method)
        {
            return DoRequest(url, method, null, null);
        }

        /// <summary>
        /// Internal helper to make an HTTP request and return the
        /// response. Throws an exception in the event of any kind
        /// of failure. Overloaded - use the other version if no
        /// post data is required.
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="method">The method, e.g. "GET"</param>
        /// <param name="postdata">Data to be posted with the request,
        /// or null if not required.</param>
        /// <param name="contenttype">The content type to send, or null
        /// if not required.</param>
        /// <returns>The server's response</returns>
        private string DoRequest(string url, string method, string postdata, string contenttype)
        {
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            req.Method = method;
            // Yuk - set an infinite timeout on this for now, because
            // executing a temporary view (for example) can take a very
            // long time...
            req.Timeout = System.Threading.Timeout.Infinite;
            if (contenttype != null)
                req.ContentType = contenttype;

            if (postdata != null)
            {
                byte[] bytes = UTF8Encoding.UTF8.GetBytes(postdata.ToString());
                req.ContentLength = bytes.Length;
                using (Stream ps = req.GetRequestStream())
                {
                    ps.Write(bytes, 0, bytes.Length);
                }
            }

            string result;
            using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
            {
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                }

            }
            return result;
        }

    }
}
