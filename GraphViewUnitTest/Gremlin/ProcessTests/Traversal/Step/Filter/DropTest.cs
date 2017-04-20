﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Filter
{
    [TestClass]
    public class DropTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_outE_drop UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/DropTest.java.
        /// Equivalent gremlin: "g.V.outE.drop"
        /// </summary>
        [TestMethod]
        public void OutEdgesDrop()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = graphCommand.g().V().OutE().Drop();
                List<string> result = traversal.Next();

                GraphTraversal2 vertexTraversalAfterDrop = graphCommand.g().V();
                Assert.AreEqual(6, vertexTraversalAfterDrop.Next().Count);

                GraphTraversal2 edgeTraversalAfterDrop = graphCommand.g().E();
                Assert.AreEqual(0, edgeTraversalAfterDrop.Next().Count);
            }
        }
        /// <summary>
        /// Port of the g_V_drop UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/DropTest.java.
        /// Equivalent gremlin: "g.V.drop"
        /// </summary>
        [TestMethod]
        public void VerticesAndEdgesDrop()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = graphCommand.g().V().Drop();
                List<string> result = traversal.Next();

                GraphTraversal2 vertexTraversalAfterDrop = graphCommand.g().V();
                Assert.AreEqual(0, vertexTraversalAfterDrop.Next().Count);

                GraphTraversal2 edgeTraversalAfterDrop = graphCommand.g().E();
                Assert.AreEqual(0, edgeTraversalAfterDrop.Next().Count);
            }
        }
        [TestMethod]
        public void DropEdge()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                //GraphTraversal2 traversal = command.g().E().Drop();


                //GraphTraversal2 traversal = command.g().V().Has("name", "marko").Property("name", "twet");

                //command.OutputFormat = OutputFormat.GraphSON;
                List<string> result = command.g().V().Has("name", "marko").Property("name", "twet").Values("name").Next();
                
                foreach (string s in result)
                {
                    Console.WriteLine(s);
                }

                //Assert.AreEqual(0, GetEdgeCount(command));
                //Assert.AreEqual(6, GetVertexCount(command));
            }
        }

        [TestMethod]
        public void DropVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Drop();
                List<string> result = traversal.Next();
                Assert.AreEqual(0, this.GetEdgeCount(command));
                Assert.AreEqual(0, this.GetVertexCount(command));
            }
        }

        [TestMethod]
        public void DropEdgeProperties()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                List<string> result = command.g().E().Properties().Next();
                Assert.AreEqual(6, result.Count);
                result = command.g().E().Properties().Drop().Next();
                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void DropMetaProperties()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                List<string> result = command.g().AddV().Property("name", "jinjin", "meta1", "metavalue")
                                                        .Property("name", "jinjin", "meta1", "metavalue").Next();
               
                result = command.g().V().Has("name", "jinjin").Properties("name").Properties().Next();
                Assert.AreEqual(1, result.Count);

                command.g().V().Has("name", "jinjin").Properties("name").Properties().Drop().Next();
                result = command.g().V().Has("name", "jinjin").Properties("name").Properties().Next();

                //var temp = command.g().V().Has("name", "jinjin").Next();
                //Console.WriteLine(temp);

                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void DropVertexProperties()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Coin(1.0).Values("name");
                List<string> result = traversal.Next();
                CheckUnOrderedResults(new[] { "marko", "vadas", "lop", "josh", "ripple", "peter" }, result);
            }
        }
    }
}
