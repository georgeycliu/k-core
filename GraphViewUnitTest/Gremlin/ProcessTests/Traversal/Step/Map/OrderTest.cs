using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class OrderTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_name_order()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().values("name").order()
        /// </summary>
        [TestMethod]
        public void get_g_V_name_order()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("name").Order();
                List<string> results = traversal.Next();
                CheckOrderedResults(new [] {"josh", "lop", "marko", "peter", "ripple", "vadas"}, results);
            }
        }

        /// <summary>
        /// get_g_V_name_order_byXa1_b1X_byXb2_a2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().values("name").order().by((a, b) -> a.substring(1, 2).compareTo(b.substring(1, 2))).by((a, b) -> b.substring(2, 3).compareTo(a.substring(2, 3)));
        /// </summary>
        public void get_g_V_name_order_byXa1_b1X_byXb2_a2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        /// <summary>
        /// get_g_V_order_byXname_incrX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().order().by("name", Order.incr).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_order_byXname_incrX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Order().By("name", GremlinKeyword.Order.Incr).Values("name");
                List<string> results = traversal.Next();
                CheckOrderedResults(new[] { "josh", "lop", "marko", "peter", "ripple", "vadas" }, results);
            }
        }

        /// <summary>
        /// get_g_V_order_byXnameX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().order().by("name").values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_order_byXnameX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Order().By("name").Values("name");
                List<string> results = traversal.Next();
                CheckOrderedResults(new[] { "josh", "lop", "marko", "peter", "ripple", "vadas" }, results);
            }
        }

        /// <summary>
        /// get_g_V_outE_order_byXweight_decrX_weight()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().outE().order().by("weight", Order.decr).values("weight");
        /// </summary>
        [TestMethod]
        public void get_g_V_outE_order_byXweight_decrX_weight()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().OutE().Order().By("weight", GremlinKeyword.Order.Decr).Values("weight");
                List<string> results = traversal.Next();
                CheckOrderedResults(new[] { 1.0, 1.0, 0.5, 0.4, 0.4, 0.2 }, results.Select(double.Parse));
            }
        }

        /// <summary>
        /// get_g_V_order_byXname_a1_b1X_byXname_b2_a2X_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().order().by("name", (a, b) -> a.substring(1, 2).compareTo(b.substring(1, 2))).
        ///                        by("name", (a, b) -> b.substring(2, 3).compareTo(a.substring(2, 3))).values("name");
        /// </summary>
        public void get_g_V_order_byXname_a1_b1X_byXname_b2_a2X_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        /// <summary>
        /// get_g_V_asXaX_outXcreatedX_asXbX_order_byXshuffleX_selectXa_bX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().as("a").out("created").as("b").order().by(Order.shuffle).select("a", "b");
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_outXcreatedX_asXbX_order_byXshuffleX_selectXa_bX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Out("created").As("b").Order().By(GremlinKeyword.Order.Shuffle).Select("a", "b");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                int markoCounter = 0;
                int joshCounter = 0;
                int peterCounter = 0;
                foreach (dynamic result in results) {
                    if (result["a"]["id"].ToString() == this.ConvertToVertexId(command, "marko")) {
                        Assert.AreEqual(this.ConvertToVertexId(command, "lop"), result["b"]["id"].ToString());
                        markoCounter++;
                    }
                    else if (result["a"]["id"].ToString() == this.ConvertToVertexId(command, "josh")) {
                        Assert.IsTrue(result["b"]["id"].ToString() == this.ConvertToVertexId(command, "lop")
                                    || result["b"]["id"].ToString() == this.ConvertToVertexId(command, "ripple"));
                        joshCounter++;
                    } else if (result["a"]["id"].ToString() == this.ConvertToVertexId(command, "peter")) {
                        Assert.AreEqual(this.ConvertToVertexId(command, "lop"), result["b"]["id"].ToString());
                        peterCounter++;
                    }
                    else {
                        Assert.Fail("This state should not have been reachable");
                    }
                }
                Assert.AreEqual(4, markoCounter + joshCounter + peterCounter);
                Assert.AreEqual(1, markoCounter);
                Assert.AreEqual(1, peterCounter);
                Assert.AreEqual(2, joshCounter);
            }
        }

        /// <summary>
        /// get_g_VX1X_hasXlabel_personX_mapXmapXint_ageXX_orderXlocalX_byXvalues_decrX_byXkeys_incrX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V(v1Id).hasLabel("person").map(v -> {
        /// final Map<Integer, Integer> map = new HashMap<>();
        /// map.put(1, (int) v.get().value("age"));
        /// map.put(2, (int) v.get().value("age") * 2);
        /// map.put(3, (int) v.get().value("age") * 3);
        /// map.put(4, (int) v.get().value("age"));
        /// return map}).order(Scope.local).by(Column.values, Order.decr).by(Column.keys, Order.incr)
        /// </summary>
        public void get_g_VX1X_hasXlabel_personX_mapXmapXint_ageXX_orderXlocalX_byXvalues_decrX_byXkeys_incrX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        /// <summary>
        /// get_g_V_order_byXoutE_count__decrX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().order().by(outE().count(), Order.decr);
        /// </summary>
        [TestMethod]
        public void get_g_V_order_byXoutE_count__decrX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Order().By(GraphTraversal2.__().OutE().Count(), GremlinKeyword.Order.Decr);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(this.ConvertToVertexId(command, "marko"), results[0]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "josh"), results[1]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "peter"), results[2]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), results[3]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), results[4]["id"].ToString());
                Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), results[5]["id"].ToString());
            }
        }

        /// <summary>
        /// get_g_V_group_byXlabelX_byXname_order_byXdecrX_foldX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().group().by(T.label).by(__.values("name").order().by(Order.decr).fold());
        /// </summary>
        [TestMethod]
        public void get_g_V_group_byXlabelX_byXname_order_byXdecrX_foldX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Group().By("label").By(GraphTraversal2.__().Values("name").Order().By(GremlinKeyword.Order.Decr).Fold());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<string> softwareList = new List<string>();
                foreach (string item in results[0]["software"]) {
                    softwareList.Add(item);
                }

                List<string> personList = new List<string>();
                foreach (string item in results[0]["person"])
                {
                    personList.Add(item);
                }

                CheckOrderedResults(new [] {"ripple", "lop"}, softwareList);
                CheckOrderedResults(new [] {"vadas", "peter", "marko", "josh"}, personList);
            }
        }

        /// <summary>
        /// get_g_V_localXbothE_weight_foldX_order_byXsumXlocalX_decrX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().local(__.bothE().values("weight").fold()).order().by(__.sum(Scope.local), Order.decr)
        /// </summary>
        [TestMethod]
        public void get_g_V_localXbothE_weight_foldX_order_byXsumXlocalX_decrX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Local(GraphTraversal2.__().BothE().Values("weight").Fold()).Order().By(GraphTraversal2.__().Sum(GremlinKeyword.Scope.Local), GremlinKeyword.Order.Decr);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<double> ans = new List<double>();
                foreach (dynamic result in results) {
                    double sum = 0;
                    foreach (dynamic num in result) {
                        sum += double.Parse(num.ToString());
                    }
                    ans.Add(sum);
                }

                CheckOrderedResults(new [] {2.4, 1.9, 1.0, 1.0, 0.5, 0.2}, ans);
            }
        }

        /// <summary>
        /// get_g_V_asXvX_mapXbothE_weight_foldX_sumXlocalX_asXsX_selectXv_sX_order_byXselectXsX_decrX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().as("v").map(__.bothE().values("weight").fold()).sum(Scope.local).as("s").select("v", "s").order().by(__.select("s"), Order.decr);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXvX_mapXbothE_weight_foldX_sumXlocalX_asXsX_selectXv_sX_order_byXselectXsX_decrX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("v").Map(GraphTraversal2.__().BothE().Values("weight").Fold()).Sum(GremlinKeyword.Scope.Local).As("s").Select("v", "s").Order().By(GraphTraversal2.__().Select("s"), GremlinKeyword.Order.Decr);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(this.ConvertToVertexId(command, "josh"), results[0]["v"]["id"].ToString());
                Assert.AreEqual(2.4, double.Parse(results[0]["s"].ToString()));

                Assert.AreEqual(this.ConvertToVertexId(command, "marko"), results[1]["v"]["id"].ToString());
                Assert.AreEqual(1.9, double.Parse(results[1]["s"].ToString()));

                Assert.AreEqual(this.ConvertToVertexId(command, "lop"), results[2]["v"]["id"].ToString());
                Assert.AreEqual(1.0, double.Parse(results[2]["s"].ToString()));

                Assert.AreEqual(this.ConvertToVertexId(command, "ripple"), results[3]["v"]["id"].ToString());
                Assert.AreEqual(1.0, double.Parse(results[3]["s"].ToString()));

                Assert.AreEqual(this.ConvertToVertexId(command, "vadas"), results[4]["v"]["id"].ToString());
                Assert.AreEqual(0.5, double.Parse(results[4]["s"].ToString()));

                Assert.AreEqual(this.ConvertToVertexId(command, "peter"), results[5]["v"]["id"].ToString());
                Assert.AreEqual(0.2, double.Parse(results[5]["s"].ToString()));
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXpersonX_order_byXageX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().hasLabel("person").order().by("age");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXpersonX_order_byXageX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("person").Order().By("age");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<string> expect = new List<string>
                {
                    this.ConvertToVertexId(command, "vadas"),
                    this.ConvertToVertexId(command, "marko"),
                    this.ConvertToVertexId(command, "josh"),
                    this.ConvertToVertexId(command, "peter")
                };
                CheckOrderedResults(expect, ((JArray)results).Select(p=>p["id"].ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXpersonX_fold_orderXlocalX_byXageX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().hasLabel("person").fold().order(Scope.local).by("age");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXpersonX_fold_orderXlocalX_byXageX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("person").Fold().Order(GremlinKeyword.Scope.Local).By("age");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                
                Assert.AreEqual(1, results.Count);

                List<string> expect = new List<string>
                {
                    this.ConvertToVertexId(command, "vadas"),
                    this.ConvertToVertexId(command, "marko"),
                    this.ConvertToVertexId(command, "josh"),
                    this.ConvertToVertexId(command, "peter")
                };
                CheckOrderedResults(expect, ((JArray)results[0]).Select(p => p["id"].ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXpersonX_order_byXvalueXageX__decrX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().hasLabel("person").order().by(v -> v.value("age"), Order.decr).values("name");
        /// </summary>
        public void get_g_V_hasLabelXpersonX_order_byXvalueXageX__decrX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        /// <summary>
        /// get_g_V_properties_order_byXkey_decrX_key()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().properties().order().by(T.key, Order.decr).key();
        /// </summary>
        [TestMethod]
        [Ignore]
        public void get_g_V_properties_order_byXkey_decrX_key()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // Haven't support T.key
            }
        }

        /// <summary>
        /// get_g_V_hasXsong_name_OHBOYX_outXfollowedByX_outXfollowedByX_order_byXperformancesX_byXsongType_incrX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().has("song", "name", "OH BOY").out("followedBy").out("followedBy").order().by("performances").by("songType", Order.decr);
        /// </summary>
        public void get_g_V_hasXsong_name_OHBOYX_outXfollowedByX_outXfollowedByX_order_byXperformancesX_byXsongType_incrX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
            }
        }

        /// <summary>
        /// get_g_V_both_hasLabelXpersonX_order_byXage_decrX_limitX5X_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().both().hasLabel("person").order().by("age", Order.decr).limit(5).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_both_hasLabelXpersonX_order_byXage_decrX_limitX5X_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Both().HasLabel("person").Order().By("age", GremlinKeyword.Order.Decr).Limit(5).Values("name");
                List<string> results = traversal.Next();
                CheckOrderedResults(new [] {"peter", "josh", "josh", "josh", "marko"}, results);
            }
        }

        /// <summary>
        /// get_g_V_both_hasLabelXpersonX_order_byXage_decrX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().both().hasLabel("person").order().by("age", Order.decr).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_both_hasLabelXpersonX_order_byXage_decrX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Both().HasLabel("person").Order().By("age", GremlinKeyword.Order.Decr).Values("name");
                List<string> results = traversal.Next();
                CheckOrderedResults(new[] { "peter", "josh", "josh", "josh", "marko", "marko", "marko", "vadas" }, results);

            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsongX_order_byXperfomances_decrX_byXnameX_rangeX110_120X_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/OrderTest.java
        /// Gremlin: g.V().hasLabel("song").order().by("performances", Order.decr).by("name").range(110, 120).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXsongX_order_byXperfomances_decrX_byXnameX_rangeX110_120X_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Both().HasLabel("song").Order().By("performances", GremlinKeyword.Order.Decr).By("name").Range(110, 120).Values("name");
                List<string> results = traversal.Next();
                Assert.AreEqual(0, results.Count);
            }
        }
    }
}
