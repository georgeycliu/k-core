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
    public class SimplePathTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_VX1X_outXcreatedX_inXcreatedX_simplePath()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SimplePathTest.java
        /// Gremlin: g.V(1).out("created").in("created").simplePath();
        /// </summary>
        [TestMethod]
        public void get_g_VX1X_outXcreatedX_inXcreatedX_simplePath()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string markoId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoId).Out("created").In("created").SimplePath();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                string joshId = this.ConvertToVertexId(command, "josh");
                string peterId = this.ConvertToVertexId(command, "peter");

                Assert.AreEqual(2, results.Count);
                foreach (dynamic result in results) {
                    Assert.IsTrue(joshId == result["id"].ToString() || peterId == result["id"].ToString());
                }
            }
        }

        /// <summary>
        /// get_g_V_repeatXboth_simplePathX_timesX3X_path()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SimplePathTest.java
        /// Gremlin: g.V().repeat(both().simplePath()).times(3).path();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXboth_simplePathX_timesX3X_path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both().SimplePath()).Times(3).Path();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(18, results.Count);

                foreach (dynamic result in results) {
                    HashSet<string> set = new HashSet<string>();
                    foreach (dynamic step in result["objects"]) {
                        set.Add(step["id"].ToString());
                    }
                    Assert.AreEqual(4, set.Count);
                    set.Clear();
                }
            }
        }
    }
}