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
    public class CountTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().count();
        /// </summary>
        [TestMethod]
        public void get_g_V_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(6, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_out_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().out().count();
        /// </summary>
        [TestMethod]
        public void get_g_V_out_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Out().Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(6, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_both_both_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().both().both().count();
        /// </summary>
        [TestMethod]
        public void get_g_V_both_both_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Both().Both().Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(30, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_repeatXoutX_timesX3X_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().repeat(out()).times(3).count();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXoutX_timesX3X_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out()).Times(3).Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(0, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_repeatXoutX_timesX8X_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().repeat(out()).times(8).count();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXoutX_timesX8X_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Out()).Times(8).Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(0, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_repeatXoutX_timesX5X_asXaX_outXwrittenByX_asXbX_selectXa_bX_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().repeat(out()).times(5).as("a").out("writtenBy").as("b").select("a", "b").count();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXoutX_timesX5X_asXaX_outXwrittenByX_asXbX_selectXa_bX_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection)) {
                GraphTraversal2 traversal =
                    command.g()
                        .V()
                        .Repeat(GraphTraversal2.__().Out())
                        .Times(5)
                        .As("a")
                        .Out("writtenBy")
                        .As("b")
                        .Select("a", "b")
                        .Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(0, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_hasXnoX_count()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().has("no").count();
        /// </summary>
        [TestMethod]
        public void get_g_V_hasXnoX_count()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("no").Count();
                List<string> result = traversal.Next();
                Assert.AreEqual(0, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_fold_countXlocalX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/CountTest.java
        /// Gremlin: g.V().fold().count(Scope.local);
        /// </summary>
        [TestMethod]
        public void get_g_V_fold_countXlocalX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Fold().Count(GremlinKeyword.Scope.Local);
                List<string> result = traversal.Next();
                Assert.AreEqual(6, int.Parse(result[0]));
            }
        }
    }
}
