using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class CoalesceTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_V_coalesceXoutXfooX_outXbarXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CoalesceTest.java
        /// Gremlin: g.V().coalesce(out("foo"), out("bar"));
        /// </summary>
        [TestMethod]
        public void CoalesceWithNonexistentTraversals()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .Coalesce(
                        GraphTraversal2.__().Out("foo"),
                        GraphTraversal2.__().Out("bar"));
                List<string> result = traversal.Next();

                Assert.IsFalse(result.Any());
            }
        }

        /// <summary>
        /// g_VX1X_coalesceXoutXknowsX_outXcreatedXX_valuesXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CoalesceTest.java
        /// Gremlin: g.V(v1Id).coalesce(out("knows"), out("created")).values("name");
        /// </summary>
        [TestMethod]
        public void CoalesceWithTwoTraversals()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .HasId(this.ConvertToVertexId(command, "marko"))
                    .Coalesce(
                        GraphTraversal2.__().Out("knows"),
                        GraphTraversal2.__().Out("created"))
                    .Values("name");
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new string[] { "josh", "vadas" }, result);
            }
        }

        /// <summary>
        /// g_VX1X_coalesceXoutXcreatedX_outXknowsXX_valuesXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CoalesceTest.java
        /// Gremlin: g.V(v1Id).coalesce(out("created"), out("knows")).values("name");
        /// </summary>
        [TestMethod]
        public void CoalesceWithTraversalsInDifferentOrder()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .HasId(this.ConvertToVertexId(command, "marko"))
                    .Coalesce(
                        GraphTraversal2.__().Out("created"),
                        GraphTraversal2.__().Out("knows"))
                    .Values("name");
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new string[] { "lop" }, result);
            }
        }

        /// <summary>
        /// g_V_coalesceXoutXlikesX_outXknowsX_inXcreatedXX_groupCount_byXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CoalesceTest.java
        /// Gremlin: g.V().coalesce(out("likes"), out("knows"), out("created")).<String>groupCount().by("name");
        /// </summary>
        [TestMethod]
        public void CoalesceWithGroupCount()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .Coalesce(
                        GraphTraversal2.__().Out("likes"),
                        GraphTraversal2.__().Out("knows"),
                        GraphTraversal2.__().Out("created"))
                    .GroupCount()
                    .By("name");

                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.Next().FirstOrDefault());
                Assert.AreEqual(1, (int)result[0]["josh"]);
                Assert.AreEqual(2, (int)result[0]["lop"]);
                Assert.AreEqual(1, (int)result[0]["ripple"]);
                Assert.AreEqual(1, (int)result[0]["vadas"]);
            }
        }

        /// <summary>
        /// g_V_coalesceXoutEXknowsX_outEXcreatedXX_otherV_path_byXnameX_byXlabelX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CoalesceTest.java
        /// Gremlin: g.V().coalesce(outE("knows"), outE("created")).otherV().path().by("name").by(T.label);
        /// </summary>

        [TestMethod]
        public void CoalesceWithPath()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .Coalesce(
                        GraphTraversal2.__().OutE("knows"),
                        GraphTraversal2.__().OutE("created"))
                    .OtherV()
                    .Path()
                    .By("name")
                    .By("label");

                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<string> expect = new List<string>
                {
                    string.Join(",", new [] { "marko", "knows", "vadas"}),
                    string.Join(",", new [] { "marko", "knows", "josh" }),
                    string.Join(",", new [] { "josh", "created", "ripple"}),
                    string.Join(",", new [] { "josh", "created", "lop" }),
                    string.Join(",", new [] { "peter", "created", "lop" }),
                };
                List<string> ans = new List<string>();
                foreach (dynamic result in results) {
                    ans.Add(string.Join(",", ((JArray)result["objects"]).Select(p => p.ToString()).ToList()));
                }
                CheckOrderedResults(expect, ans);
            }
        }
    }
}