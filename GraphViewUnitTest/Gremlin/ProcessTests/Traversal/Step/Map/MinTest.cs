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
    public class MinTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_age_min()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MinTest.java
        /// Gremlin: g.V().values("age").min()
        /// </summary>
        [TestMethod]
        public void get_g_V_age_min()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Min();
                List<string> result = traversal.Next();
                Assert.AreEqual(27, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_repeatXbothX_timesX5X_age_min()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MinTest.java
        /// Gremlin: g.V().repeat(both()).times(5).values("age").min();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXbothX_timesX5X_age_min()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both()).Times(5).Values("age").Min();
                List<string> result = traversal.Next();
                Assert.AreEqual(27, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_minX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MinTest.java
        /// Gremlin: g.V().hasLabel("software").group().by("name").by(bothE().values("weight").min());
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_minX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("software").Group().By("name").By(GraphTraversal2.__().BothE().Values("weight").Min());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(1.0 == double.Parse(results[0]["ripple"].ToString()) && 0.2 == double.Parse(results[0]["lop"].ToString()));
            }
        }
    }
}
