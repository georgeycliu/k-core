using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using GraphView;
using Newtonsoft.Json;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class StoreTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_storeXa_nameX_out_capXaX UT from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/StoreTest.java.
        /// Equivalent gremlin: "g.V().store('a').by('name').out().cap('a')"
        /// </summary>
        [TestMethod]
        public void VerticesStoreAByNameOutCapA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Store("a").By("name").Out().Cap("a");

                IEnumerable<string> result = traversal.Next().First().Trim('[', ']').Split(',').Select(r => r.Trim(' '));
                List<string> expectedResult = new List<string> { "marko", "josh", "peter", "lop", "ripple", "vadas" };
                CheckUnOrderedResults(expectedResult, result);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_storeXaX_byXnameX_out_storeXaX_byXnameX_name_capXaX UT from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/StoreTest.java.
        /// Equivalent gremlin: "g.V(v1Id).store('a').by('name').out().store('a').by('name').name.cap('a')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void VertexWithIdStoreAByNameOutStoreAByNameValuesNameCapA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoVertexId).Store("a").By("name").Out().Store("a").By("name").Values("name").Cap("a");

                IEnumerable<string> result = traversal.Next().First().Trim('[', ']').Split(',').Select(r => r.Trim(' '));
                List<string> expectedResult = new List<string> { "marko", "josh", "vadas", "lop" };
                CheckUnOrderedResults(expectedResult, result);
            }
        }

        /// <summary>
        /// Port of the g_V_storeXaX_byXoutEXcreatedX_countX_out_out_storeXaX_byXinEXcreatedX_weight_sumX UT from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/StoreTest.java.
        /// Equivalent gremlin: "g.V.store('a').by(__.outE('created').count).out.out.store('a').by(__.inE('created').weight.sum).cap('a')"
        /// </summary>
        [TestMethod]
        public void VerticesStoreAByOutECountOutOutStoreAByInECreatedValuesWeightSumCapA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Store("a").By(GraphTraversal2.__().OutE("created").Count()).Out().Out().Store("a").By(GraphTraversal2.__().InE("created").Values("weight").Sum()).Cap("a");

                IEnumerable<string> result = traversal.Next().First().Trim('[', ']').Split(',').Select(r => r.Trim(' '));
                List<string> expectedResult = new List<string> { "2", "1", "1", "1", "1", "0", "0", "0" };
                CheckUnOrderedResults(expectedResult, result);
            }
        }
    }
}
