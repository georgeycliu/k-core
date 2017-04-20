using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    /// <summary>
    /// Test for Vertex step, which is g.V().
    /// </summary>
    [TestClass]
    public class VertexTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_VXlistX1_2_3XX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(Arrays.asList(v1Id, v2Id, v3Id)).values("name");
        /// </summary>
        [TestMethod]
        public void GetVertexByIdList()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string[] expectedNames = new[] { "marko", "vadas", "lop" };
                GraphTraversal2 traversal = command.g()
                    .V(expectedNames.Select(n => this.ConvertToVertexId(command, n)).ToArray<object>())
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(expectedNames, result);
            }
        }

        // Gremlin test g_VXlistXv1_v2_v3XX_name() is skipped since we don't have vertex model.

        /// <summary>
        /// g_V()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V();
        /// </summary>
        [TestMethod]
        public void GetAllVertexes()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V();
                List<string> result = traversal.Next();

                Assert.AreEqual(6, result.Count);
            }
        }

        /// <summary>
        /// g_VX1X_out()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).out();
        /// </summary>
        [TestMethod]
        public void GetOutVertexes()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .Out()
                    .Values("name");
                List<string> result = traversal.Next();

                AssertMarkoOut(result);
            }
        }

        /// <summary>
        /// get_g_VX2X_in()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v2Id).in();
        /// </summary>
        [TestMethod]
        public void GetInVertexes()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "vadas"))
                    .In()
                    .Values("name");
                List<string> result = traversal.Next();

                AssertVadasIn(result);
            }
        }


        /// <summary>
        /// g_VX4X_both()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v4Id).both();
        /// </summary>
        [TestMethod]
        public void GetBothVertexes()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "josh"))
                    .Both()
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "marko", "ripple", "lop" }, result);
            }
        }

        /// <summary>
        /// g_E()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.E();
        /// </summary>
        [TestMethod]
        public void GetAllEdges()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().E();
                List<string> result = traversal.Next();

                Assert.AreEqual(6, result.Count);
            }
        }

        /// <summary>
        /// g_EX11X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.E(e11Id);
        /// </summary>
        [TestMethod]
        public void GetEdgeById()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // E(Id), E().HasId() and E().Id() does not work

                string expectedEdgeId = this.ConvertToEdgeId(command, "josh", "created", "lop");
                GraphTraversal2 traversal = command.g().E(expectedEdgeId).Id();
                List<string> result = traversal.Next();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(expectedEdgeId, result.First());
            }
        }

        /// <summary>
        /// g_VX1X_outE()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).outE();
        /// </summary>
        [TestMethod]
        public void GetOutEdges()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .OutE()
                    .Label();
                List<string> result = traversal.Next();

                Assert.AreEqual(3, result.Count);
                CheckUnOrderedResults(new[] { "knows", "knows", "created" }, result);
            }
        }

        /// <summary>
        /// g_VX2X_inE()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v2Id).inE();
        /// </summary>
        [TestMethod]
        public void GetInEdges()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "vadas"))
                    .InE()
                    .Label();
                List<string> result = traversal.Next();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("knows", result.First());
            }
        }

        /// <summary>
        /// g_VX4X_bothEXcreatedX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v4Id).bothE("created");
        /// </summary>
        [TestMethod]
        public void GetBothEdgesFiltedByLabel()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string expectedId = this.ConvertToVertexId(command, "josh");
                GraphTraversal2 traversal = command.g()
                    .V(expectedId)
                    .BothE("created");
                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.Next().FirstOrDefault());

                foreach (dynamic edge in result)
                {
                    Assert.AreEqual("created", (string)edge.label);
                    Assert.AreEqual(expectedId, (string)edge.outV);
                }

                Assert.AreEqual(2, result.Count);
            }
        }

        /// <summary>
        /// g_VX4X_bothE()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v4Id).bothE();
        /// </summary>
        [TestMethod]
        public void GetBothEdges()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // TODO: V().BothE() does not work

                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "josh"))
                    .BothE()
                    .Label();
                List<string> result = traversal.Next();

                Assert.AreEqual(3, result.Count);
                CheckUnOrderedResults(new[] { "knows", "created", "created" }, result);
            }
        }

        /// <summary>
        /// g_VX1X_outE_inV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).outE().inV();
        /// </summary>
        [TestMethod]
        public void GetOutEdgeInVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .OutE()
                    .InV()
                    .Values("name");
                List<string> result = traversal.Next();

                AssertMarkoOut(result);
            }
        }


        /// <summary>
        /// g_VX2X_inE_outV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v2Id).inE().outV();
        /// </summary>
        [TestMethod]
        public void GetInEdgeOutVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "vadas"))
                    .InE()
                    .OutV()
                    .Values("name");
                List<string> result = traversal.Next();

                AssertVadasIn(result);
            }
        }

        /// <summary>
        /// g_V_outE_hasXweight_1X_outV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V().outE().has("weight", 1.0d).outV();
        /// </summary>
        [TestMethod]
        public void GetOutEdgeOutVertexFilteredByProperty()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V()
                    .OutE()
                    .Has("weight", 1.0d)
                    .OutV()
                    .Values("name");
                List<string> result = traversal.Next();

                Assert.AreEqual(2, result.Count);
                CheckUnOrderedResults(new[] { "marko", "josh" }, result);
            }
        }

        /// <summary>
        /// g_V_out_outE_inV_inE_inV_both_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V().out().outE().inV().inE().inV().both().values("name");
        /// </summary>
        [TestMethod]
        public void CombinationOfInOutBoth()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                IEnumerable<string> expected = Enumerable.Repeat("josh", 4).Concat(
                    Enumerable.Repeat("marko", 3)).Concat(
                    Enumerable.Repeat("peter", 3));

                GraphTraversal2 traversal = command.g()
                    .V()
                    .Out()
                    .OutE()
                    .InV()
                    .InE()
                    .InV()
                    .Both()
                    .Values("name");
                List<string> result = traversal.Next();

                Assert.AreEqual(10, result.Count);
                CheckUnOrderedResults(expected, result);
            }
        }

        /// <summary>
        /// g_VX1X_outEXknowsX_bothV_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).outE("knows").bothV().values("name");
        /// </summary>
        [TestMethod]
        public void GetOutEdgeBothVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // TODO: V().BothV() does not work

                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .OutE("knows")
                    .BothV()
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "marko", "marko", "josh", "vadas" }, result);
            }
        }

        /// <summary>
        /// g_VX1X_outE_otherV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).outE().otherV();
        /// </summary>
        [TestMethod]
        public void GetOutEdgeOtherVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .OutE()
                    .OtherV()
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "josh", "vadas", "lop" }, result);
            }
        }

        /// <summary>
        /// g_VX4X_bothE_otherV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v4Id).bothE().otherV();
        /// </summary>
        [TestMethod]
        public void GetBothEdgeOtherVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "josh"))
                    .BothE()
                    .OtherV()
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "marko", "ripple", "lop" }, result);
            }
        }

        /// <summary>
        /// g_VX4X_bothE_hasXweight_lt_1X_otherV()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v4Id).bothE().has("weight", P.lt(1d)).otherV();
        /// </summary>
        [TestMethod]
        public void GetBothEdgeOtherVertexFilteredByProperty()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // TODO: V().BothE() does not work

                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "josh"))
                    .BothE()
                    .Has("weight", Predicate.lt(1d))
                    .OtherV()
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "lop" }, result);
            }
        }

        // Gremlin test g_VX1X_outXknowsX() is skipped because in our implementation vertex Id cannot be parsed as int.

        /// <summary>
        /// g_VX1X_outXknowsAsStringIdX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(v1Id).out("knows");
        /// </summary>
        [TestMethod]
        public void GetOutVertexFilteredByEdgeLabel()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g()
                    .V(this.ConvertToVertexId(command, "marko"))
                    .Out("knows")
                    .Values("name");
                List<string> result = traversal.Next();

                string[] expected = new[] { "vadas", "josh" };
                CheckUnOrderedResults(expected, result);
            }
        }

        /// <summary>
        /// Original test
        /// Gremlin: g.V().hasId(Arrays.asList(v1Id, v2Id, v3Id)).values("name");
        /// </summary>
        [TestMethod]
        public void VertexHasIdByIdList()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string[] expectedNames = new[] { "marko", "vadas", "lop" };
                GraphTraversal2 traversal = command.g()
                    .V()
                    .HasId(expectedNames.Select(n => this.ConvertToVertexId(command, n)).ToArray<object>())
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(expectedNames, result);
            }
        }

        /// <summary>
        /// Original test
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V(new Object());
        /// </summary>
        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void GetVertexByInvalidId()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                List<string> result = command.g().V(new object()).Next();
            }
        }


        /// <summary>
        /// Original test
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/VertexTest.java
        /// Gremlin: g.V().hasId(new Object());
        /// </summary>
        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void GetVertexHasIdByInvalidId()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                List<string> result = command.g().V().HasId(new object()).Next();
            }
        }

        private static void AssertMarkoOut(List<string> result)
        {
            string[] expected = new[] { "vadas", "josh", "lop" };
            CheckUnOrderedResults(expected, result);
        }


        private static void AssertVadasIn(List<string> result)
        {
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("marko", result.First());
        }
    }
}