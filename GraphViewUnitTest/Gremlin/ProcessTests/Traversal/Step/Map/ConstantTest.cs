using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class ConstantTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_V_constantX123X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/ConstantTest.java
        /// Gremlin: g.V().constant(123);
        /// </summary>
        [TestMethod]
        public void ConstantWithVertex()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Constant(123);
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(Enumerable.Repeat("123", 6), result);
            }
        }

        /// <summary>
        /// g_V_chooseXhasLabelXpersonX_valuesXnameX_constantXinhumanXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/ConstantTest.java
        /// Gremlin: g.V().choose(hasLabel("person"), values("name"), constant("inhuman"));
        /// </summary>
        [TestMethod]
        public void ConstantWithChoose()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Choose(
                    GraphTraversal2.__().HasLabel("person"),
                    GraphTraversal2.__().Values("name"),
                    GraphTraversal2.__().Constant("inhuman"));
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new string[] { "marko", "vadas", "inhuman", "josh", "inhuman", "peter" }, result);
            }
        }
    }
}