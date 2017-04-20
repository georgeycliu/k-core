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
    public class MeanTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_age_mean()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MeanTest.java
        /// Gremlin: g.V().values("age").mean();
        /// </summary>
        [TestMethod]
        public void get_g_V_age_mean()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Mean();
                List<string> result = traversal.Next();
                Assert.AreEqual(30.75, double.Parse(result[0]));
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_meanX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/MeanTest.java
        /// Gremlin: g.V().hasLabel("software").group().by("name").by(bothE().values("weight").mean());
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXsoftwareX_group_byXnameX_byXbothE_weight_meanX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("software").Group().By("name").By(GraphTraversal2.__().BothE().Values("weight").Mean());
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(1.0 == double.Parse(results[0]["ripple"].ToString()) && Math.Abs(1.0/3.0 - double.Parse(results[0]["lop"].ToString())) < 0.0001);
            }
        }
        
    }
}
