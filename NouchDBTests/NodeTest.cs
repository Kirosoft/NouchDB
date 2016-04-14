using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Text;
using NDB;

namespace NouchDBTests
{
    [TestClass]
    public class NodeTest
    {

        [TestMethod]
        public void Test1()
        {
            // Test case 1 - simple merge (no conflict)
            //var rootNode1 = new Node();
            //rootNode1.addVersion("0","data");
            //rootNode1.addVersion("1","data1");
            //rootNode1.addVersion("2","data2");
            //Console.WriteLine(rootNode1.toString());

            //var rootNode2 = new Node();
            //rootNode2.addVersion("0","data");
            //rootNode2.addVersion("1","data1");
            //rootNode2.addVersion("2","data2");
            //rootNode2.addVersion("3","data3");
            //Console.WriteLine(rootNode2.toString());


            //rootNode1.merge(rootNode2);

            //Console.WriteLine(rootNode1.toString());
        }
        [TestMethod]
        public void Test2()
        {
            // Test case 2 - merge with conflicting edits
            var rootNode1 = new Node("docId","data",0);
            rootNode1.addVersion("data1",1);
            rootNode1.addVersion("data2",2);
            //Console.WriteLine(rootNode1.toString());

            var rootNode2 = new Node("docId2", "data",3);
            rootNode2.addVersion("data1",4);
            rootNode2.addVersion("rootnode2-data2",5);
            rootNode2.addVersion("rootnode2-data3",6);
            //Console.WriteLine(rootNode2.toString());


            List<Node> conflicts = rootNode1.merge(rootNode2);

            Console.WriteLine();
            Console.WriteLine("Conflicts");
            Console.WriteLine("=========");

            foreach (Node node in conflicts)
            {
                Console.WriteLine(node.rev_id);
            }

            Console.WriteLine();
            Console.WriteLine("Result");
            Console.WriteLine("======");

            //Console.WriteLine(rootNode1.toString());


            Console.WriteLine(new[] { 1, 2, 3 }.ToJson());
            Console.WriteLine(rootNode1.revision.ToJson<Dictionary<string, Node>>());
            //Console.WriteLine(rootNode1.ToString());
        }


    }
}
