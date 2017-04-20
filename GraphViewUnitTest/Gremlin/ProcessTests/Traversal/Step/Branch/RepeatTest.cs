using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Branch
{
    [TestClass]
    public sealed class RepeatTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_repeatXoutX_timesX2X_emit_path() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(out()).times(2).emit().path()"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatOutTimes2EmitPath()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out()).Times(2).Emit().Path();

                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.Next()[0]);
                Dictionary<int, long> pathLengths = new Dictionary<int, long>();
                foreach (dynamic path in results)
                {
                    long count;
                    pathLengths.TryGetValue(path["objects"].Count, out count);
                    pathLengths[path["objects"].Count] = count + 1;
                }
                Assert.AreEqual(2, pathLengths.Count);
                Assert.AreEqual(8, results.Count);
                Assert.AreEqual(6, pathLengths[2]);
                Assert.AreEqual(2, pathLengths[3]);
            }
        }

        /// <summary>
        /// Port of the g_V_repeatXoutX_timesX2X_repeatXinX_timesX2X_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(out()).times(2).repeat(__.in()).times(2).values("name")"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatOutTimes2RepeatInTimes2ValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .Repeat(GraphTraversal2.__().Out())
                        .Times(2)
                        .Repeat(GraphTraversal2.__().In())
                        .Times(2)
                        .Values("name");

                List<string> results = traversal.Next();
                CheckUnOrderedResults(new string[] { "marko", "marko" }, results);
            }
        }

        /// <summary>
        /// Port of the g_V_repeatXoutX_timesX2X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(out()).times(2)"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatOutTimes2()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out()).Times(2);

                List<string> results = traversal.Values("name").Next();
                foreach (string name in results)
                {
                    Assert.IsTrue(string.Equals(name, "lop") || string.Equals(name, "ripple"));
                }
                Assert.AreEqual(2, results.Count);
            }
        }

        /// <summary>
        /// Port of the g_V_repeatXoutX_timesX2X_emit() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(out()).times(2).emit()"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatOutTimes2Emit()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out()).Times(2).Emit();

                List<string> results = traversal.Values("name").Next();
                Dictionary<string, long> map = new Dictionary<string, long>();
                foreach (string name in results)
                {
                    long count;
                    map.TryGetValue(name, out count);
                    map[name] = count + 1;
                }

                Assert.AreEqual(4, map.Count);
                Assert.IsTrue(map.ContainsKey("vadas"));
                Assert.IsTrue(map.ContainsKey("josh"));
                Assert.IsTrue(map.ContainsKey("ripple"));
                Assert.IsTrue(map.ContainsKey("lop"));
                Assert.AreEqual(1, map["vadas"]);
                Assert.AreEqual(1, map["josh"]);
                Assert.AreEqual(2, map["ripple"]);
                Assert.AreEqual(4, map["lop"]);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_timesX2X_repeatXoutX_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V(v1Id).times(2).repeat(out()).values("name")"
        /// </summary>
        [TestMethod]
        public void HasVertexIdTimes2RepeatOutValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V().HasId(vertexId).Times(2).Repeat(GraphTraversal2.__().Out()).Values("name");

                List<string> results = traversal.Next();
                CheckUnOrderedResults(new string[] { "lop", "ripple" }, results);
            }
        }

        private void AssertPath(GraphTraversal2 traversal)
        {
            dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.Next()[0]);
            int path1 = 0, path2 = 0, path3 = 0;
            foreach (dynamic path in results)
            {
                switch ((int)path["objects"].Count)
                {
                    case 1:
                        ++path1;
                        break;
                    case 2:
                        ++path2;
                        break;
                    case 3:
                        ++path3;
                        break;
                    default:
                        Assert.Fail("Only path lengths of 1, 2, or 3 should be seen");
                        break;
                }
            }
            Assert.AreEqual(6, path1);
            Assert.AreEqual(6, path2);
            Assert.AreEqual(2, path3);
        }

        /// <summary>
        /// Port of the g_V_emit_timesX2X_repeatXoutX_path() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().emit().times(2).repeat(out()).path()"
        /// </summary>
        [TestMethod]
        public void VerticesEmitTimes2RepeatOutPath()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Emit().Times(2).Repeat(GraphTraversal2.__().Out()).Path();

                this.AssertPath(traversal);
            }
        }

        /// <summary>
        /// Port of the g_V_emit_repeatXoutX_timesX2X_path() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().emit().repeat(out()).times(2).path()"
        /// </summary>
        [TestMethod]
        public void VerticesEmitRepeatOutTimes2Path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Emit().Repeat(GraphTraversal2.__().Out()).Times(2).Path();

                this.AssertPath(traversal);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_emitXhasXlabel_personXX_repeatXoutX_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V(v1Id).emit(has(T.label, "person")).repeat(out()).values("name")"
        /// </summary>
        [TestMethod]
        public void HasVertexIdEmitHasLabelPersonRepeatOutValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .HasId(vertexId)
                        .Emit(GraphTraversal2.__().HasLabel("person"))
                        .Repeat(GraphTraversal2.__().Out())
                        .Values("name");

                List<string> results = traversal.Next();
                CheckUnOrderedResults(new string[] { "marko", "josh", "vadas" }, results);
            }
        }

        /// <summary>
        /// Port of the g_V_repeatXgroupCountXmX_byXnameX_outX_timesX2X_capXmX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(groupCount("m").by("name").out()).times(2).cap("m")"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatGroupCountMByNameOutTimes2CapM()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().GroupCount("m").By("name").Out()).Times(2).Cap("m");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.Next()[0]);
                Assert.AreEqual(1, results.Count);
                dynamic result = results[0];
                Assert.AreEqual(2, (int)result["ripple"]);
                Assert.AreEqual(1, (int)result["peter"]);
                Assert.AreEqual(2, (int)result["vadas"]);
                Assert.AreEqual(2, (int)result["josh"]);
                Assert.AreEqual(4, (int)result["lop"]);
                Assert.AreEqual(1, (int)result["marko"]);
            }
        }

        /// <summary>
        /// Port of the g_V_repeatXbothX_timesX10X_asXaX_out_asXbX_selectXa_bX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V().repeat(both()).times(10).as("a").out().as("b").select("a", "b")"
        /// </summary>
        [TestMethod]
        public void VerticesRepeatBothTimes10AsAOutAsBSelectAB()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both()).Times(10).As("a").Out().As("b").Select("a", "b");

                int counter = 0;
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.Next()[0]);
                foreach (dynamic result in results)
                {
                    Assert.IsTrue(result["a"] != null);
                    Assert.IsTrue(result["b"] != null);
                    counter++;
                }
                Assert.IsTrue(counter == 43958);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_repeatXoutX_untilXoutE_count_isX0XX_name() UT from org/apache/tinkerpop/gremlin/process/traversal/step/branch/RepeatTest.java.
        /// Equivalent gremlin: "g.V(v1Id).repeat(out()).until(outE().count().is(0)).values("name")"
        /// </summary>
        [TestMethod]
        public void HasVertexIdRepeatOutUntilOutECountIs0ValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .HasId(vertexId)
                        .Repeat(GraphTraversal2.__().Out())
                        .Until(GraphTraversal2.__().OutE().Count().Is(0))
                        .Values("name");

                List<string> results = traversal.Next();
                CheckUnOrderedResults(new string[] { "lop", "lop", "ripple", "vadas" }, results);
            }
        }
    }
}
