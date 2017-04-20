using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Filter
{
    /// <summary>
    /// Tests for Has Step.
    /// </summary>
    [TestClass]
    public sealed class HasTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_outXcreatedX_hasXname__mapXlengthX_isXgtX3XXX_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.out('created').has('name',map{it.length()}.is(gt(3))).name"
        /// </summary>
        [TestMethod]
        [Ignore]
        public void OutCreatedHasNameLengthGT3()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                Assert.Fail();
                //var traversal = GraphViewCommand.g().V().Out("created").Has("name", Predicate.gt(3));
                //var result = traversal.Values("name").Next();
                //Assert.AreEqual(1, result.Count);
                //Assert.AreEqual("ripple", result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_VX1X_hasXkeyX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).has(k)", "v1Id", v1Id, "k", key
        /// </summary>
        [TestMethod]
        public void HasVIdHasName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V().HasId(vertexId).Has("name");

                List<string> result = traversal.Values("name").Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("marko", result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_VX1X_hasXname_markoX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).has('name', 'marko')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVIdHasNameMarko()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V().HasId(vertexId).Has("name", "marko");

                List<string> result = traversal.Values("name").Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("marko", result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_V_hasXname_markoX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('name', 'marko')"
        /// </summary>
        [TestMethod]
        public void HasNameMarko()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("name", "marko");

                List<string> result = traversal.Values("name").Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("marko", result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_V_hasXname_blahX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('name', 'blah')"
        /// </summary>
        [TestMethod]
        public void HasNameBlah()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("name", "blah");

                string result = traversal.Next().FirstOrDefault();
                Assert.IsNull(result);
            }
        }

        /// <summary>
        /// Port of the g_V_hasXage_gt_30X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('age',gt(30))"
        /// </summary>
        [TestMethod]
        public void HasAgeGT30()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("age", Predicate.gt(30));

                List<string> result = traversal.Values("age").Next();
                Assert.AreEqual(2, result.Count);
                foreach (string age in result)
                {
                    Assert.IsTrue(int.Parse(age) > 30);
                }
            }
        }

        /// <summary>
        /// Port of the g_V_hasXage_isXgt_30XX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('age', __.is(gt(30)))"
        /// </summary>
        [TestMethod]
        public void HasAgeIsGT30()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("age", GraphTraversal2.__().Is(Predicate.gt(30)));

                List<string> result = traversal.Values("age").Next();
                Assert.AreEqual(2, result.Count);
                foreach (string age in result)
                {
                    Assert.IsTrue(int.Parse(age) > 30);
                }
            }
        }

        /// <summary>
        /// Port of the g_VX1X_hasXage_gt_30X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).has('age',gt(30))", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVIdHasAgeGT30()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");
                string vertexId2 = this.ConvertToVertexId(command, "josh");

                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).Has("age", Predicate.gt(30));
                GraphTraversal2 traversal2 = command.g().V().HasId(vertexId2).Has("age", Predicate.gt(30));

                List<string> result = traversal.Next();
                Assert.AreEqual(0, result.Count);

                List<string> result2 = traversal2.Next();
                Assert.AreEqual(1, result2.Count);
            }
        }

        /// <summary>
        /// Port of the g_VXv1X_hasXage_gt_30X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(g.V(v1Id).next()).has('age',gt(30))", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasIdTraversalHasVIdHasAgeGT30()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");
                string vertexId2 = this.ConvertToVertexId(command, "josh");

                GraphTraversal2 traversal = command.g().V(vertexId1)
                    .Has("age", Predicate.gt(30));

                List<string> result = traversal.Next();
                Assert.AreEqual(0, result.Count);

                GraphTraversal2 traversal2 = command.g().V(vertexId2)
                    .Has("age", Predicate.gt(30));

                List<string> result2 = traversal2.Next();
                Assert.AreEqual(1, result2.Count);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_out_hasXid_2X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).out.hasId(v2Id)", "v1Id", v1Id, "v2Id", v2Id
        /// </summary>
        [TestMethod]
        public void HasVIdOutHasVId()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");
                string vadasVertexId = this.ConvertToVertexId(command, "vadas");

                GraphTraversal2 traversal = command.g().V().HasId(markoVertexId)
                    .Out().HasId(vadasVertexId);

                this.AssertVadasAsOnlyValueReturned(command, traversal);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_out_hasXid_2_3X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).out.hasId(v2Id, v3Id)", "v1Id", v1Id, "v2Id", v2Id, "v3Id", v3Id
        /// </summary>
        [TestMethod]
        public void HasVIdOutHasVIds()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string id1 = this.ConvertToVertexId(command, "marko");
                string id2 = this.ConvertToVertexId(command, "vadas");
                string id3 = this.ConvertToVertexId(command, "lop");

                GraphTraversal2 traversal = command.g().V().HasId(id1).Out().HasId(id2, id3);

                this.Assert_g_VX1X_out_hasXid_2_3X(id2, id3, traversal);
            }
        }

        /// <summary>
        /// Port of the g_V_hasXblahX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('blah')"
        /// </summary>
        [TestMethod]
        public void HasBlah()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("blah");

                List<string> result = traversal.Next();
                //Assert.IsNull(result);
                Assert.AreEqual(0, result.Count);
            }
        }

        /// <summary>
        /// Port of the g_EX7X_hasXlabelXknowsX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.E(e7Id).hasLabel('knows')", "e7Id", e7Id
        /// </summary>
        [TestMethod]
        public void EdgesHasEIdHasLabelKnows()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string edgeId = this.ConvertToEdgeId(command, "marko", "knows", "vadas");

                GraphTraversal2 traversal = command.g().E().HasId(edgeId).HasLabel("knows");

                List<string> result = traversal.Label().Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("knows", result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_E_hasXlabelXknowsX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.E.hasLabel('knows')"
        /// </summary>
        [TestMethod]
        public void EdgesHasLabelKnows()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().E().HasLabel("knows");
                List<string> result = traversal.Label().Next();
                Assert.AreEqual(2, result.Count);
                foreach (string res in result)
                {
                    Assert.AreEqual("knows", res);
                }
            }
        }

        /// <summary>
        /// Port of the g_V_hasXperson_name_markoX_age() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.has('person', 'name', 'marko').age"
        /// </summary>
        [TestMethod]
        public void HasPersonNameMarkoAge()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("person", "name", "marko").Values("age");

                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(29, int.Parse(result.FirstOrDefault()));
            }
        }

        /// <summary>
        /// Port of the g_VX1X_outE_hasXweight_inside_0_06X_inV() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V(v1Id).outE.has('weight', inside(0.0d, 0.6d)).inV", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVIdOutEHasWeightInside0dot0d0dot6dInV()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");

                GraphTraversal2 traversal = command.g().V(vertexId).OutE()
                .Has("weight", Predicate.inside(0.0d, 0.6d)).InV();

                List<string> result = traversal.Values("name").Next();
                Assert.AreEqual(2, result.Count);
                foreach (string res in result)
                {
                    Assert.IsTrue(string.Equals(res, "vadas") || string.Equals(res, "lop"));
                }
            }
        }

        /// <summary>
        /// Port of the g_EX11X_outV_outE_hasXid_10X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.E(e11Id).outV.outE.has(T.id, e8Id)", "e11Id", e11Id, "e8Id", e8Id
        /// </summary>
        [TestMethod]
        public void EdgesHasEIdOutVOutEHasEId()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string edgeId1 = this.ConvertToEdgeId(command, "josh", "created", "lop");
                string edgeId2 = this.ConvertToEdgeId(command, "josh", "created", "ripple");

                GraphTraversal2 traversal = command.g().E().HasId(edgeId1).OutV().OutE().HasId(edgeId2);
                List<string> result = traversal.Id().Next();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(edgeId2, result.FirstOrDefault());
            }
        }

        /// <summary>
        /// Port of the g_V_hasLabelXpersonX_hasXage_notXlteX10X_andXnotXbetweenX11_20XXXX_andXltX29X_orXeqX35XXXX_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.hasLabel('person').has('age', P.not(lte(10).and(P.not(between(11,20)))).and(lt(29).or(eq(35)))).name"
        /// </summary>
        [TestMethod]
        public void HasLabelPersonHasAgeNotLTE10AndNotBetween11n20ANDLT29OrEQ35()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().HasLabel("person")
                    .Has("age",
                        Predicate.not(
                            Predicate.lte(10)
                            .And(Predicate.not(Predicate.between(11, 20))))
                        .And(Predicate.lt(29)
                        .Or(Predicate.eq(35))))
                        .Values("name");

                List<string> result = traversal.Next();
                Assert.IsTrue(result.Contains("peter") && result.Contains("vadas"));
            }
        }

        /// <summary>
        /// Port of the g_V_in_hasIdXneqX1XX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/HasTest.java.
        /// Equivalent gremlin: "g.V.hasLabel('person').has('age', P.not(lte(10).and(P.not(between(11,20)))).and(lt(29).or(eq(35)))).name"
        /// </summary>
        [TestMethod]
        public void InHasIdNEQVId()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");

                GraphTraversal2 traversal = command.g().V().In().Has("id", Predicate.neq(vertexId));
                List<string> result = traversal.Values("name").Next();

                Assert.AreEqual(3, result.Count);
                Assert.IsTrue(result.Contains("josh") && result.Contains("peter"));
            }
        }

        private void Assert_g_VX1X_out_hasXid_2_3X(string id2, string id3, GraphTraversal2 traversal)
        {
            List<string> result = traversal.Id().Next();
            Assert.IsTrue(result.Contains(id2) || result.Contains(id3));
        }

        private void AssertVadasAsOnlyValueReturned(GraphViewCommand command, GraphTraversal2 traversal)
        {
            List<string> results = traversal.Id().Next();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), results.FirstOrDefault());
        }
    }
}
