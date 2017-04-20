using System.Collections.Generic;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Filter
{
    [TestClass]
    public sealed class SampleTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_E_sampleX1X() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SampleTest.java.
        /// Equivalent gremlin: "g.E().sample(1)"
        /// </summary>
        [TestMethod]
        public void g_E_sampleX1X()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = graphCommand.g().E().Sample(1);

                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
            }
        }

        /// <summary>
        /// Port of g_E_sampleX2X_byXweightX() UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SampleTest.java.
        /// Equivalent gremlin: "g.E().sample(2).by("weight")"
        /// </summary>
        [TestMethod]
        public void g_E_sampleX2X_byXweightX()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection)) {
                GraphTraversal2 traversal = graphCommand.g().E().Sample(2).By("weight");

                List<string> result = traversal.Next();
                Assert.AreEqual(2, result.Count);
            }
        }

        /// <summary>
        /// Port of g_V_localXoutE_sampleX1X_byXweightXX UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SampleTest.java.
        /// Equivalent gremlin: "g.V().local(outE().sample(1).by("weight"))"
        /// </summary>
        [TestMethod]
        public void g_V_localXoutE_sampleX1X_byXweightXX()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection)) {
                GraphTraversal2 traversal = graphCommand.g().V().Local(GraphTraversal2.__().OutE().Sample(1).By("weight"));

                List<string> results = traversal.Next();
                Assert.AreEqual(3, results.Count);
            }
        }

        /// <summary>
        /// Port of g_V_group_byXlabelX_byXbothE_weight_sampleX2X_foldX UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SampleTest.java.
        /// Equivalent gremlin: "g.V().group().by(label).by(bothE().values("weight").sample(2).fold())"
        /// </summary>
        [TestMethod]
        public void g_V_group_byXlabelX_byXbothE_weight_sampleX2X_foldX()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection)) {
                graphCommand.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal =
                    graphCommand.g()
                        .V()
                        .Group()
                        .By("label")
                        .By(GraphTraversal2.__().BothE().Values("weight").Sample(2).Fold());

                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual(2, result[0]["software"].Count);
                Assert.AreEqual(2, result[0]["person"].Count);
            }
        }

        /// <summary>
        /// Port of g_V_group_byXlabelX_byXbothE_weight_fold_sampleXlocal_5XX UT from org/apache/tinkerpop/gremlin/process/traversal/step/filter/SampleTest.java.
        /// Equivalent gremlin: "g.V().group().by(label).by(bothE().values("weight").fold().sample(local, 5))"
        /// </summary>
        [TestMethod]
        public void g_V_group_byXlabelX_byXbothE_weight_fold_sampleXlocal_5XX()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection))
            {
                graphCommand.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = graphCommand.g().V().Group().By("label").By(GraphTraversal2.__().BothE().Values("weight").Fold().Sample(GremlinKeyword.Scope.Local, 5));

                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual(4, result[0]["software"].Count);
                Assert.AreEqual(5, result[0]["person"].Count);
            }
        }
    }
}
