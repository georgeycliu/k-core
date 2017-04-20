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
    public class MaxTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_age_max()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MaxTest.java
        /// Gremlin: g.V().values("age").max();
        /// </summary>
        [TestMethod]
        public void get_g_V_age_max()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Max();
                List<string> result = traversal.Next();
                Assert.AreEqual(35, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_repeatXbothX_timesX5X_age_max()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MaxTest.java
        /// Gremlin: g.V().repeat(both()).times(5).values("age").max();
        /// </summary>
        [TestMethod]
        public void get_g_V_repeatXbothX_timesX5X_age_max()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Repeat(GraphTraversal2.__().Both()).Times(5).Values("age").Max();
                List<string> result = traversal.Next();
                Assert.AreEqual(35, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_maxX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MaxTest.java
        /// Gremlin: g.V().hasLabel("software").group().by("name").by(bothE().values("weight").max());
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_maxX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("software").Group().By("name").By(GraphTraversal2.__().BothE().Values("weight").Max());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                foreach (dynamic result in results)
                {
                    Assert.IsTrue(1.0 == double.Parse(result["ripple"].ToString()) && 0.4 == double.Parse(result["lop"].ToString()));
                }
            }
        }
    }
}
