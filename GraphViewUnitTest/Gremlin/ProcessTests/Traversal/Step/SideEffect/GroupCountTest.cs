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
    public class GroupCountTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_outXcreatedX_groupCount_byXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().out("created").groupCount().by("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_outXcreatedX_groupCount_byXnameX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created").GroupCount().By("name");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(3, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_outXcreatedX_groupCountXaX_byXnameX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().out("created").groupCount("a").by("name").cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_outXcreatedX_groupCountXaX_byXnameX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created").GroupCount("a").By("name").Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(3, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_outXcreatedX_name_groupCount()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().out("created").values("name").groupCount();
        /// </summary>
        [TestMethod]
        public void get_g_V_outXcreatedX_name_groupCount()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created").Values("name").GroupCount();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(3, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_outXcreatedX_groupCountXxX_capXxX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().out("created").groupCount("x").cap("x");
        /// </summary>
        [TestMethod]
        public void get_g_V_outXcreatedX_groupCountXxX_capXxX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created").GroupCount("x").Cap("x");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                string lopId = this.ConvertToVertexId(command, "lop");
                string rippleId = this.ConvertToVertexId(command, "ripple");

                Assert.AreEqual(3, int.Parse(results[0][lopId].ToString()));
                Assert.AreEqual(1, int.Parse(results[0][rippleId].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_outXcreatedX_name_groupCountXaX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().out("created").values("name").groupCount("a").cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_outXcreatedX_name_groupCountXaX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created").Values("name").GroupCount("a").Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(3, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_hasXnoX_groupCount()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().has("no").groupCount();
        /// </summary>
        [TestMethod]
        public void get_g_V_hasXnoX_groupCount()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("no").GroupCount();
                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("[]", result[0]);
            }
        }

        /// <summary>
        /// get_g_V_hasXnoX_groupCountXaX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().has("no").groupCount("a").cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasXnoX_groupCountXaX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("no").GroupCount("a").Cap("a");
                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("[]", result[0]);
            }
        }

        /// <summary>
        /// get_g_V_repeatXout_groupCountXaX_byXnameXX_timesX2X_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().repeat(out().groupCount("a").by("name")).times(2).cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXout_groupCountXaX_byXnameXX_timesX2X_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out().GroupCount("a").By("name")).Times(2).Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(2, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(1, int.Parse(results[0]["vadas"].ToString()));
                Assert.AreEqual(1, int.Parse(results[0]["josh"].ToString()));
                Assert.AreEqual(4, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_unionXrepeatXoutX_timesX2X_groupCountXmX_byXlangXX__repeatXinX_timesX2X_groupCountXmX_byXnameXX_capXmX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().union(repeat(out()).times(2).groupCount("m").by("lang"),
        ///                       repeat(in()).times(2).groupCount("m").by("name")).cap("m");
        /// </summary>
        /// Multi group with a same sideEffect key is an undefined behavior in Gremlin and hence not supported
        [TestMethod]
        [Ignore]
        public void get_g_V_unionXrepeatXoutX_timesX2X_groupCountXmX_byXlangXX__repeatXinX_timesX2X_groupCountXmX_byXnameXX_capXmX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Union(GraphTraversal2.__().Repeat(GraphTraversal2.__().Out()).Times(2).GroupCount("m").By("lang"), GraphTraversal2.__().Repeat(GraphTraversal2.__().In()).Times(2).GroupCount("m").By("name")).Cap("m");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(2, int.Parse(results[0]["java"].ToString()));
                Assert.AreEqual(2, int.Parse(results[0]["marko"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_groupCount_byXbothE_countX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().groupCount().by(bothE().count());
        /// </summary>
        [TestMethod]
        public void get_g_V_groupCount_byXbothE_countX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().GroupCount().By(GraphTraversal2.__().BothE().Count());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(3, int.Parse(results[0]["1"].ToString()));
                Assert.AreEqual(3, int.Parse(results[0]["3"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_unionXoutXknowsX__outXcreatedX_inXcreatedXX_groupCount_selectXvaluesX_unfold_sum()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().union(out("knows"), out("created").in("created")).groupCount().select(Column.values).unfold().sum();
        /// </summary>
        [TestMethod]
        public void get_g_V_unionXoutXknowsX__outXcreatedX_inXcreatedXX_groupCount_selectXvaluesX_unfold_sum()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Union(GraphTraversal2.__().Out("knows"), GraphTraversal2.__().Out("created").In("created")).GroupCount().Select(GremlinKeyword.Column.Values).Unfold().Sum();
                List<string> results = traversal.Next();
                Assert.AreEqual(12, int.Parse(results[0]));
            }
        }

        /// <summary>
        /// get_g_V_both_groupCountXaX_out_capXaX_selectXkeysX_unfold_both_groupCountXaX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().both().groupCount("a").out().cap("a").select(Column.keys).unfold().both().groupCount("a").cap("a");
        /// </summary>
        /// Multi group with a same sideEffect key is an undefined behavior in Gremlin and hence not supported
        [TestMethod]
        [Ignore]
        public void get_g_V_both_groupCountXaX_out_capXaX_selectXkeysX_unfold_both_groupCountXaX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Both().GroupCount("a").Out().Cap("a").Select(GremlinKeyword.Column.Keys).Unfold().Both().GroupCount("a").Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                string markoId = this.ConvertToVertexId(command, "marko");
                string vadasId = this.ConvertToVertexId(command, "vadas");
                string lopId = this.ConvertToVertexId(command, "lop");
                string joshId = this.ConvertToVertexId(command, "josh");
                string rippleId = this.ConvertToVertexId(command, "ripple");
                string peterId = this.ConvertToVertexId(command, "peter");

                Assert.AreEqual(6, int.Parse(results[0][markoId].ToString()));
                Assert.AreEqual(2, int.Parse(results[0][vadasId].ToString()));
                Assert.AreEqual(6, int.Parse(results[0][lopId].ToString()));
                Assert.AreEqual(6, int.Parse(results[0][joshId].ToString()));
                Assert.AreEqual(2, int.Parse(results[0][rippleId].ToString()));
                Assert.AreEqual(2, int.Parse(results[0][peterId].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_both_groupCountXaX_byXlabelX_asXbX_barrier_whereXselectXaX_selectXsoftwareX_isXgtX2XXX_selectXbX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupCountTest.java
        /// Gremlin: g.V().both().groupCount("a").by(T.label).as("b").barrier().where(__.select("a").select("software").is(gt(2))).select("b").values("name");
        /// </summary>
        /// Where should execute after groupCount
        [TestMethod]
        [Ignore]
        public void get_g_V_both_groupCountXaX_byXlabelX_asXbX_barrier_whereXselectXaX_selectXsoftwareX_isXgtX2XXX_selectXbX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Both().GroupCount("a").By("label").As("b").Barrier().Where(GraphTraversal2.__().Select("a").Select("software").Is(Predicate.gt(2))).Select("b").Values("name");
                List<string> results = traversal.Next();

                CheckUnOrderedResults(new [] {"lop", "lop", "lop", "vadas", "josh", "josh", "josh", "marko", "marko", "marko", "peter", "ripple"}, results);
            }
        }
    }
}