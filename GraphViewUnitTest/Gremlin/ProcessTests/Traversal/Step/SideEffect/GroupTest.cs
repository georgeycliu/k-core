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
    public class GroupTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_group_byXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXnameX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group().By("name");
                this.AssertCommonA(command, traversal);
            }
        }

        /// <summary>
        /// get_g_V_group_byXnameX_by()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by("name").by();
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXnameX_by()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group().By("name").By();
                this.AssertCommonA(command, traversal);
            }
        }

        /// <summary>
        /// get_g_V_groupXaX_byXnameX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group("a").by("name").cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_groupXaX_byXnameX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group("a").By("name").Cap("a");
                this.AssertCommonA(command, traversal);
            }
        }

        /// <summary>
        /// get_g_V_hasXlangX_groupXaX_byXlangX_byXnameX_out_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().has("lang").group("a").by("lang").by("name").out().cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasXlangX_groupXaX_byXlangX_byXnameX_out_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Has("lang").Group("a").By("lang").By("name").Out().Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                CheckUnOrderedResults(new [] {"lop", "ripple"}, ((JArray)results[0]["java"]).Select(p=>p.ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_hasXlangX_group_byXlangX_byXcountX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().has("lang").group().by("lang").by(count());
        /// </summary>
        [TestMethod]
        public void get_g_V_hasXlangX_group_byXlangX_byXcountX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Has("lang").Group().By("lang").By(GraphTraversal2.__().Count());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(2, int.Parse(results[0]["java"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_repeatXout_groupXaX_byXnameX_byXcountX_timesX2X_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().repeat(out().group("a").by("name").by(count())).times(2).cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXout_groupXaX_byXnameX_byXcountX_timesX2X_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out().Group("a").By("name").By(GraphTraversal2.__().Count())).Times(2).Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(2, int.Parse(results[0]["ripple"].ToString()));
                Assert.AreEqual(1, int.Parse(results[0]["vadas"].ToString()));
                Assert.AreEqual(1, int.Parse(results[0]["josh"].ToString()));
                Assert.AreEqual(4, int.Parse(results[0]["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_group_byXoutE_countX_byXnameX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by(outE().count()).by("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXoutE_countX_byXnameX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group().By(GraphTraversal2.__().OutE().Count()).By("name");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                CheckUnOrderedResults(new [] {"vadas", "lop", "ripple"}, ((JArray)results[0]["0"]).Select(p=>p.ToString()).ToList());
                CheckUnOrderedResults(new[] { "peter" }, ((JArray)results[0]["1"]).Select(p => p.ToString()).ToList());
                CheckUnOrderedResults(new[] { "josh" }, ((JArray)results[0]["2"]).Select(p => p.ToString()).ToList());
                CheckUnOrderedResults(new[] { "marko" }, ((JArray)results[0]["3"]).Select(p => p.ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_groupXaX_byXlabelX_byXoutE_weight_sumX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group("a").by(T.label).by(outE().values("weight").sum()).cap("a");
        /// </summary>
        [TestMethod]
        public void get_g_V_groupXaX_byXlabelX_byXoutE_weight_sumX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group("a").By("label").By(GraphTraversal2.__().OutE().Values("weight").Sum()).Cap("a");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(0.0, double.Parse(results[0]["software"].ToString()));
                Assert.AreEqual(3.5, double.Parse(results[0]["person"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_repeatXbothXfollowedByXX_timesX2X_group_byXsongTypeX_byXcountX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().repeat(both("followedBy")).times(2).group().by("songType").by(count());
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_repeatXbothXfollowedByXX_timesX2X_group_byXsongTypeX_byXcountX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //Use GRATEFUL test data
            }
        }

        /// <summary>
        /// get_g_V_repeatXbothXfollowedByXX_timesX2X_groupXaX_byXsongTypeX_byXcountX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().repeat(both("followedBy")).times(2).group("a").by("songType").by(count()).cap("a");
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_repeatXbothXfollowedByXX_timesX2X_groupXaX_byXsongTypeX_byXcountX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //Use GRATEFUL test data
            }
        }

        /// <summary>
        /// get_g_V_group_byXname_substring_1X_byXconstantX1XX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by(v -> v.value("name").substring(0, 1)).by(constant(1l));
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_group_byXname_substring_1X_byXconstantX1XX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                
            }
        }

        /// <summary>
        /// get_g_V_groupXaX_byXname_substring_1X_byXconstantX1XX_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group("a").by(v -> v.value("name").substring(0, 1)).by(constant(1l)).cap("a");
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_groupXaX_byXname_substring_1X_byXconstantX1XX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
            }
        }

        /// <summary>
        /// get_g_V_out_group_byXlabelX_selectXpersonX_unfold_outXcreatedX_name_limitX2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().out().group().by(T.label).select("person").unfold().out("created").values("name").limit(2);
        /// </summary>
        [TestMethod]
        public void get_g_V_out_group_byXlabelX_selectXpersonX_unfold_outXcreatedX_name_limitX2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Out().Group().By("label").Select("person").Unfold().Out("created").Values("name").Limit(2);
                List<string> results = traversal.Next();
                CheckUnOrderedResults(new [] {"ripple", "lop"}, results);
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsongX_group_byXnameX_byXproperties_groupCount_byXlabelXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().hasLabel("song").group().by("name").by(__.properties().groupCount().by(T.label));
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_hasLabelXsongX_group_byXnameX_byXproperties_groupCount_byXlabelXX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //Use GRATEFUL test data
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsongX_groupXaX_byXnameX_byXproperties_groupCount_byXlabelXX_out_capXaX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().hasLabel("song").group("a").by("name").by(__.properties().groupCount().by(T.label)).out().cap("a");
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_hasLabelXsongX_groupXaX_byXnameX_byXproperties_groupCount_byXlabelXX_out_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //Use GRATEFUL test data
            }
        }

        /// <summary>
        /// get_g_V_repeatXunionXoutXknowsX_groupXaX_byXageX__outXcreatedX_groupXbX_byXnameX_byXcountXX_groupXaX_byXnameXX_timesX2X_capXa_bX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().repeat(__.union(__.out("knows").group("a").by("age"), __.out("created").group("b").by("name").by(count())).group("a").by("name")).times(2).cap("a", "b");
        /// </summary>
        /// 
        [TestMethod]
        [Ignore]
        public void get_g_V_repeatXunionXoutXknowsX_groupXaX_byXageX__outXcreatedX_groupXbX_byXnameX_byXcountXX_groupXaX_byXnameXX_timesX2X_capXa_bX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .Repeat(GraphTraversal2.__()
                            .Union(GraphTraversal2.__().Out("knows").Group("a").By("age"),
                                GraphTraversal2.__().Out("created").Group("b").By("name").By(GraphTraversal2.__().Count()))
                            .Group("a").By("name"))
                        .Times(2)
                        .Cap("a", "b");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                dynamic mapA = results[0]["a"];
                Assert.AreEqual(1, mapA[32].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "josh"), mapA[32][0]["id"].ToString());
                Assert.AreEqual(1, mapA[27].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), mapA[27][0]["id"].ToString());
                Assert.AreEqual(2, mapA["ripple"].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), mapA["ripple"][0]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), mapA["ripple"][1]["id"].ToString());
                Assert.AreEqual(1, mapA["vadas"].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), mapA["vadas"][0]["id"].ToString());
                Assert.AreEqual(1, mapA["josh"].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "josh"), mapA["josh"][0]["id"].ToString());
                Assert.AreEqual(4, mapA["lop"].Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), mapA["lop"][0]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), mapA["lop"][1]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), mapA["lop"][2]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), mapA["lop"][3]["id"].ToString());

                dynamic mapB = results[0]["b"];
                Assert.AreEqual(2, int.Parse(mapB["ripple"].ToString()));
                Assert.AreEqual(4, int.Parse(mapB["lop"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_group_byXbothE_countX_byXgroup_byXlabelXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by(bothE().count()).by(__.group().by(T.label));
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXbothE_countX_byXgroup_byXlabelXX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .Group()
                        .By(GraphTraversal2.__().BothE().Count())
                        .By(GraphTraversal2.__().Group().By("label"));
                JArray results = JsonConvert.DeserializeObject<JArray>(traversal.FirstOrDefault());

                Assert.AreEqual(results.Count, 1);
                JObject result = (JObject) results[0];

                Assert.IsTrue(result.Count == 2);
                Assert.IsTrue(result["1"] != null);
                Assert.IsTrue(result["3"] != null);

                JObject submap = (JObject) result["1"];
                Assert.AreEqual(2, submap.Count);
                Assert.IsTrue(submap["software"] != null);
                Assert.IsTrue(submap["person"] != null);
                JArray list = (JArray) submap["software"];
                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), list[0]["id"].ToString());
                list = (JArray) submap["person"];
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Select(p=>p["id"].ToString()).ToList().Contains(this.ConvertToVertexId(command, "vadas")));
                Assert.IsTrue(list.Select(p => p["id"].ToString()).ToList().Contains(this.ConvertToVertexId(command, "peter")));

                submap = (JObject) result["3"];
                Assert.AreEqual(2, submap.Count);
                Assert.IsTrue(submap["software"] != null);
                Assert.IsTrue(submap["person"] != null);
                list = (JArray) submap["software"];
                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), list[0]["id"].ToString());
                list = (JArray) submap["person"];
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Select(p => p["id"].ToString()).ToList().Contains(this.ConvertToVertexId(command, "marko")));
                Assert.IsTrue(list.Select(p => p["id"].ToString()).ToList().Contains(this.ConvertToVertexId(command, "josh")));
            }
        }

        /// <summary>
        /// get_g_V_outXfollowedByX_group_byXsongTypeX_byXbothE_group_byXlabelX_byXweight_sumXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().out("followedBy").group().by("songType").by(bothE().group().by(T.label).by(values("weight").sum()));
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_outXfollowedByX_group_byXsongTypeX_byXbothE_group_byXlabelX_byXweight_sumXX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //use GRATEFUL test data
            }
        }

        /// <summary>
        /// get_g_V_groupXmX_byXnameX_byXinXknowsX_nameX_capXmX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group("m").by("name").by(__.in("knows").values("name")).cap("m");
        /// </summary>
        public void get_g_V_groupXmX_byXnameX_byXinXknowsX_nameX_capXmX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                //Can't get any results in gremlin console
            }
        }

        /// <summary>
        /// get_g_V_group_byXlabelX_byXbothE_groupXaX_byXlabelX_byXweight_sumX_weight_sumX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: g.V().group().by(T.label).by(bothE().group("a").by(T.label).by(values("weight").sum()).values("weight").sum());
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXlabelX_byXbothE_groupXaX_byXlabelX_byXweight_sumX_weight_sumX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .Group()
                        .By("label")
                        .By(
                            GraphTraversal2.__()
                                .BothE()
                                .Group("a")
                                .By("label")
                                .By(GraphTraversal2.__().Values("weight").Sum())
                                .Values("weight")
                                .Sum());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(2.0, double.Parse(results[0]["software"].ToString()));
                Assert.AreEqual(5.0, double.Parse(results[0]["person"].ToString()));
            }
        }

        /// <summary>
        /// get_g_VX1X_outXcreatedX_valueMap()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/GroupTest.java
        /// Gremlin: 
        ///     final Map<String, List<Object>> map = new HashMap<>();
        ///     map.put("marko", new ArrayList<>(Collections.singleton(666)));
        ///     map.put("noone", new ArrayList<>(Collections.singleton("blah")));
        ///     return g.withSideEffect("a", map).V().group("a").by("name").by(outE().label().fold()).cap("a");
        /// </summary>
        public void get_g_withSideEffectXa__marko_666_noone_blahX_V_groupXaX_byXnameX_byXoutE_label_foldX_capXaX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        private void AssertCommonA(GraphViewCommand command, GraphTraversal2 traversal)
        {
            dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), results[0]["ripple"][0]["id"].ToString());
            Assert.AreEqual(this.ConvertToVertexId(command, "peter"), results[0]["peter"][0]["id"].ToString());
            Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), results[0]["vadas"][0]["id"].ToString());
            Assert.AreEqual(this.ConvertToVertexId(command, "josh"), results[0]["josh"][0]["id"].ToString());
            Assert.AreEqual(this.ConvertToVertexId(command, "lop"), results[0]["lop"][0]["id"].ToString());
            Assert.AreEqual(this.ConvertToVertexId(command, "marko"), results[0]["marko"][0]["id"].ToString());
        }
    }
}