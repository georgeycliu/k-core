using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class TailTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_valuesXnameX_order_tailXglobal_2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().values("name").order().tail(global, 2);
        /// </summary>
        [TestMethod]
        public void get_g_V_valuesXnameX_order_tailXglobal_2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("name").Order().Tail(GremlinKeyword.Scope.Global, 2);
                List<string> results = traversal.Next();

                CheckOrderedResults(new [] {"ripple", "vadas"}, results);
            }
        }

        /// <summary>
        /// get_g_V_valuesXnameX_order_tailX2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().values("name").order().tail(2);
        /// </summary>
        [TestMethod]
        public void get_g_V_valuesXnameX_order_tailX2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("name").Order().Tail(2);
                List<string> results = traversal.Next();

                CheckOrderedResults(new[] { "ripple", "vadas" }, results);
            }
        }

        /// <summary>
        /// get_g_V_valuesXnameX_order_tail()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().values("name").order().tail();
        /// </summary>
        [TestMethod]
        public void get_g_V_valuesXnameX_order_tail()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("name").Order().Tail();
                List<string> results = traversal.Next();

                CheckOrderedResults(new[] {"vadas" }, results);
            }
        }
        /// <summary>
        /// get_g_V_valuesXnameX_order_tailX7X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().values("name").order().tail(7);
        /// </summary>
        [TestMethod]
        public void get_g_V_valuesXnameX_order_tailX7X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("name").Order().Tail(7);
                List<string> results = traversal.Next();

                CheckOrderedResults(new[] { "josh", "lop", "marko", "peter", "ripple", "vadas" }, results);
            }
        }

        /// <summary>
        /// get_g_V_repeatXbothX_timesX3X_tailX7X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().repeat(both()).times(3).tail(7);
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXbothX_timesX3X_tailX7X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both()).Times(3).Tail(7);
                List<string> results = traversal.Next();

                Assert.AreEqual(7, results.Count);
            }
        }

        /// <summary>
        /// get_g_V_repeatXin_outX_timesX3X_tailX7X_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().repeat(in().out()).times(3).tail(7).count();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXin_outX_timesX3X_tailX7X_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both()).Times(3).Tail(7).Count();
                List<string> results = traversal.Next();

                Assert.AreEqual(7, int.Parse(results[0]));
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocal_2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("a").out().as("a").select("a").by(unfold().values("name").fold()).tail(local, 2);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocal_2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("a").Out().As("a").Select("a").By(GraphTraversal2.__().Unfold().Values("name").Fold()).Tail(GremlinKeyword.Scope.Local, 2);
                List<string> results = traversal.Next();

                List<string> expect = new List<string>
                {
                    "[josh, ripple]",
                    "[josh, lop]"
                };
                CheckUnOrderedResults(expect, results);
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocal_1X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("a").out().as("a").select("a").by(unfold().values("name").fold()).tail(local, 1);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocal_1X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("a").Out().As("a").Select("a").By(GraphTraversal2.__().Unfold().Values("name").Fold()).Tail(GremlinKeyword.Scope.Local, 1);
                List<string> results = traversal.Next();

                CheckUnOrderedResults(new [] {"ripple", "lop"}, results);
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocalX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("a").out().as("a").select("a").by(unfold().values("name").fold()).tail(local);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXunfold_valuesXnameX_foldX_tailXlocalX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("a").Out().As("a").Select("a").By(GraphTraversal2.__().Unfold().Values("name").Fold()).Tail(GremlinKeyword.Scope.Local);
                List<string> results = traversal.Next();

                CheckUnOrderedResults(new[] { "ripple", "lop" }, results);
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXlimitXlocal_0XX_tailXlocal_1X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("a").out().as("a").select("a").by(limit(local, 0)).tail(local, 1);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXaX_out_asXaX_selectXaX_byXlimitXlocal_0XX_tailXlocal_1X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("a").Out().As("a").Select("a").By(GraphTraversal2.__().Limit(GremlinKeyword.Scope.Local, 0)).Tail(GremlinKeyword.Scope.Local, 1);
                List<string> results = traversal.Next();

                Assert.AreEqual(0, results.Count);
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXbX_out_asXcX_selectXa_b_cX_byXnameX_tailXlocal_2X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("b").out().as("c").select("a","b","c").by("name").tail(local, 2);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXbX_out_asXcX_selectXa_b_cX_byXnameX_tailXlocal_2X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("b").Out().As("c").Select("a", "b", "c").By("name").Tail(GremlinKeyword.Scope.Local, 2);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                foreach (dynamic result in results) {
                    Assert.AreEqual("josh", result["b"].ToString());
                    Assert.IsTrue("ripple" == result["c"].ToString() || "lop" == result["c"].ToString());
                }
            }
        }

        /// <summary>
        /// get_g_V_asXaX_out_asXbX_out_asXcX_selectXa_b_cX_byXnameX_tailXlocal_1X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/TailTest.java
        /// Gremlin: g.V().as("a").out().as("b").out().as("c").select("a","b","c").by("name").tail(local, 1);
        /// </summary>
        [TestMethod]
        public void get_g_V_asXaX_out_asXbX_out_asXcX_selectXa_b_cX_byXnameX_tailXlocal_1X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Out().As("b").Out().As("c").Select("a", "b", "c").By("name").Tail(GremlinKeyword.Scope.Local, 1);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                foreach (dynamic result in results)
                {
                    Assert.IsTrue("ripple" == result["c"].ToString() || "lop" == result["c"].ToString());
                }
            }
        }
    }
}