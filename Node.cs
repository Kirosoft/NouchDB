using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;
using LitJson;

namespace NouchDB
{
    // Recursive Node definition designed to hold a revision tree for a given document id
    public class Node : IEnumerable
    {
        /// <summary>
        /// Version number of the latest document revision
        /// </summary>
        public string currentVersion { set; get; }
        /// <summary>
        /// Holds the list of revisions for a given document id 
        /// </summary>
        public Dictionary<string, Node> revision = new Dictionary<string, Node>(); 
        
        /// <summary>
        /// If this is a subversion parent will be set
        /// </summary>
        private Node parent = null;
        
        public string id { set; get; } //document id
        public string rev_id { set; get; } // revision id
        public long sequence { set; get; } // link to the sequence store

        public Node()
        {
        }

        // Basic node creation - used during de-serialisation
        public Node(string id)
        {
            this.id = id;
        }

        // Create the initial root revision (called during insert)
        public Node(string id, string data, long sequence) 
        {
            string rev_id = revision.Count.ToString() + "-" + Utils.CalculateMD5Hash(data);

            // create the new subnode containing this version
            Node newNode = new Node(this, rev_id,sequence);
            
            newNode.sequence = sequence;        // reference to the actual data in the full sequence store
            this.id = id;
            this.rev_id = rev_id;
            this.sequence = sequence;

            revision.Add("0", newNode);
            currentVersion = "0";
        }

        // Create the initial root revision (called during insert from replication)
        public Node(string id, string data, long sequence, string rev_id)
        {
            //rev_id = revision.Count.ToString() + "-" + Utils.CalculateMD5Hash(data);

            // create the new subnode containing this version
            Node newNode = new Node(this, rev_id, sequence);

            newNode.sequence = sequence;        // reference to the actual data in the full sequence store
            this.id = id;
            this.rev_id = rev_id;
            this.sequence = sequence;
            currentVersion = rev_id.Split('-')[0];
            revision.Add(currentVersion, newNode);
            
        }


        // Called to create a new subnode under a parent
        public Node(Node parent, string rev_id, long sequence)
        {
            this.parent = parent;
            this.rev_id = rev_id;            
            this.sequence = sequence;
        }


        public Node addVersion(string previousVersion, string rev_id, long sequence)
        {
            Node newNode = null;

            Console.WriteLine("Previous version: " + previousVersion + " , current version: " + currentVersion);

            // Update this doc with a new version
            if (previousVersion == currentVersion)
            {

                Debug.WriteLine("versions match");


                // create the new subnode containing this version
                newNode = new Node(this, rev_id,sequence);

                this.addVersion(currentVersion, newNode);

            }
            else
            {
                Debug.WriteLine("Trying to add a node with an unexpected version number: " + Convert.ToString(currentVersion));
                // create the new subnode containing this version
                newNode = new Node(this, rev_id, sequence);

                this.addVersion(previousVersion, newNode);
                
            }

            return newNode;
        }

        // Adds the supplied document update data as the latest version
        public Node addVersion(string data, long sequence)
        {
            string rev_id = revision.Count.ToString() + "-" + Utils.CalculateMD5Hash(data);

            return addVersion(currentVersion, rev_id, sequence);
        }

        // Adds the supplied document update and revision 
        public Node addVersion(string data, long sequence, string rev_id)
        {
            Node result = null;
            
            string version = rev_id.Split('-')[0];

            if (revision.ContainsKey(version))
            {
                Node target = revision[version];

                if (target.rev_id != rev_id)
                {
                    throw new Exception("Attempt to overwrite revision with incompatible revision");
                }
                else
                {
                    // already exists do nothing else
                    result = null;
                    //throw new Exception("Identical revision already exists");
                }
            }
            else
            {
                result = addVersion(version, rev_id, sequence);

            }

            return result;
        }


        // Called during deserialisation
        public Node addVersion(string version, string sequence, string rev_id)
        {
            Node newNode = new Node(this, rev_id, Convert.ToInt64(sequence));
            revision.Add(version, newNode);
            currentVersion = version;

            return newNode;
        }

        public Node addVersion(string previousVersion, Node newNode)
        {

            // Update this doc with a new version
            if (previousVersion == currentVersion)
            {

                string nextVersion = Convert.ToString(Convert.ToInt32(currentVersion) + 1);
                Debug.WriteLine("Version incremented to: " + nextVersion);


                revision.Add(nextVersion, newNode);
                currentVersion = nextVersion;

            }
            else
            {
                Debug.WriteLine("addVersion(node) - incompatible version being added: ");
                Debug.WriteLine("Previous version: " + previousVersion + " , current version: " + currentVersion);

            }

            return newNode;
        }

        public long getLatestSequence()
        {
            return revision[currentVersion].sequence;
        }


        public string getLatestSig()
        {
            Node currentRev = revision[currentVersion];

            return id + "," + currentRev.rev_id;
        }

        //public string toString()
        //{
        //    string result = "";


        //    foreach (KeyValuePair<string, Node> kvPair in revision)
        //    {

        //        result += "Version: " + Convert.ToString(kvPair.Key) + " - Data: " + kvPair.Value.data + "\n";
        //    }
        //    return result;
        //}

        //public override string ToString()
        //{

        //    string result = "";
        //    int count = 0;


        //    foreach (KeyValuePair<string, Node> kvPair in revision)
        //    {

        //        if (count++ > 0)
        //        {
        //            result += ",";
        //        }

        //        result += "'Version': '" + Convert.ToString(kvPair.Key) + "' , 'Data': '" + kvPair.Value.data + "'";

        //    }

        //    return result.Replace("'", "\"");
        //}

        // Returns the latest node for a given version number
        public Node getVersion(string version)
        {
            Node node = null;

            try
            {
                node = revision[version];
            }
            catch (Exception ee)
            {
            }


            return node;
        }

        // Enumerator for document revisions
        public IEnumerator GetEnumerator()
        {

            foreach (var version in revision)
            { 
                Node node = version.Value;

                string nodeStr = "{";
                nodeStr += "sequence" + ":" + node.sequence+ ",";
                nodeStr += "version" + ":" + version.Key + ",";
                nodeStr += "rev_id" + ":" + node.rev_id ;
                nodeStr += "}";

                yield return nodeStr;
            }

        }

        public string ToJson()
        {
            string result = "{";

            result += "doc_id:" + this.id + ",version:[";
            IEnumerator enumerator = GetEnumerator();

            while (enumerator.MoveNext())
            {
                string version = (string) enumerator.Current;
                result += version;
            }

            result += "]}";

            return result;

        }

        public static Node Parse(string json)
        {

            string tempStr= json.Split(',')[0];
            string docId = tempStr.Split(':')[1];
            Node newNode = new Node(docId);

            json = json.Split('[')[1];
            json = json.Split(']')[0];

            var nodeList = json.Split('{');
            var versionList = new Dictionary<string,Dictionary<string,string>>();

            foreach (string node in nodeList)
            {
                if (node != "")
                {
                    var propertyList = node.Split(',');

                    var propList = new Dictionary<string, string>();

                    foreach (string property in propertyList)
                    {
                        string propStr = property.Split('}')[0];

                        string propName = propStr.Split(':')[0];
                        string propValue = propStr.Split(':')[1];

                        propList.Add(propName, propValue);
                    }

                    versionList.Add(propList["version"], propList);
                    newNode.addVersion(propList["version"],propList["sequence"],propList["rev_id"]);
                }
            }

            return newNode;
        }

        // Merge this revision tree with the one passed in as rootNode
        // This implementation overwrites local nodes with the supplied version
        public List<Node> merge(Node rootNode)
        {
            List<Node> conflicts = new List<Node>();
            bool finished = false;
            int currentVersion = 1;
            Console.WriteLine("Starting merge: ");

            do
            {
                Console.WriteLine("Merge version: " + Convert.ToString(currentVersion));

                Node node = rootNode.getVersion(Convert.ToString(currentVersion));

                if (node == null)
                {
                    Console.WriteLine("Merge complete..");
                    finished = true;
                }
                else
                {

                    if (revision.ContainsKey(Convert.ToString(currentVersion)))
                    {
                        Console.WriteLine("Target contains same version as source ...");
                        Console.WriteLine("Source contains: " + revision[Convert.ToString(currentVersion)].rev_id);
                        Console.WriteLine("Target contains: " + node.rev_id);

                        if (revision[Convert.ToString(currentVersion)].rev_id != node.rev_id)
                        {
                            // Conflict - two version of the same value exist
                            Console.WriteLine("Conflict detected: " + node.rev_id);

                            // work out which has the longer tail - this will take priority
                            conflicts.Add(node);
                        }

                    }
                    else
                    {
                        // Version does not exist - add the whole subtree (potentially)
                        Console.WriteLine("New version being added: " + node.rev_id);
                        this.addVersion(Convert.ToString(currentVersion - 1), node);
                    }


                }

                currentVersion++;

            } while (!finished);


            return conflicts;
        }

    }
}
