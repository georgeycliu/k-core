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
    public class SumTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_valuesXageX_sum()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/SumTest.java
        /// Gremlin: g.V().values("age").sum()
        /// </summary>
        [TestMethod]
        public void get_g_V_valuesXageX_sum()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Sum();
                List<string> result = traversal.Next();
                Assert.AreEqual(123, int.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_sumX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/SumTest.java
        /// Gremlin: g.V().hasLabel("software").group().by("name").by(bothE().values("weight").sum());
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_sumX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("software").Group().By("name").By(GraphTraversal2.__().BothE().Values("weight").Sum());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.IsTrue(1.0 == double.Parse(results[0]["ripple"].ToString()) && 1.0 == double.Parse(results[0]["lop"].ToString()));
            }
        }
    }
}
