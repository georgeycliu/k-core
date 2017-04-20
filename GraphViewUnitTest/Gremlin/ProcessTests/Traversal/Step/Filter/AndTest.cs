using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Linq;
using GraphView;
using Newtonsoft.Json;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Filter
{
    [TestClass]
    public class AndTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_V_andXhasXage_gt_27X__outE_count_gt_2X_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/AndTest.java
        /// Gremlin: g.V().and(has("age", P.gt(27)), outE().count().is(P.gte(2l))).values("name");
        /// </summary>
        [TestMethod]
        public void AndWithParameters()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .And(
                        GraphTraversal2.__().Has("age", Predicate.gt(27)),
                        GraphTraversal2.__().OutE().Count().Is(Predicate.gte(2)))
                    .Values("name");
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new string[] { "marko", "josh" }, result);
            }
        }

        /// <summary>
        /// g_V_andXout__hasXlabel_personX_and_hasXage_gte_32XX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/AndTest.java
        /// Gremlin: g.V().and(outE(), has(T.label, "person").and().has("age", P.gte(32))).values("name");
        /// </summary>
        [TestMethod]
        public void AndAsInfixNotation()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .And(
                        GraphTraversal2.__().OutE(),
                        GraphTraversal2.__().HasLabel("person").And().Has("age", Predicate.gte(32)))
                    .Values("name");
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new string[] { "josh", "peter" }, result);
            }
        }

        /// <summary>
        /// g_V_asXaX_outXknowsX_and_outXcreatedX_inXcreatedX_asXaX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/AndTest.java
        /// Gremlin: g.V().as("a").out("knows").and().out("created").in("created").as("a").values("name");
        /// </summary>
        [TestMethod]
        public void AndWithAs()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertex = this.ConvertToVertexId(command, "marko");
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .As("a")
                    .Out("knows")
                    .And()
                    .Out("created")
                    .In("created")
                    .As("a")
                    .Values("name");
                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.Next().FirstOrDefault()).First;

                Assert.AreEqual(vertex, (string)result.id);
            }
        }

        /// <summary>
        /// g_V_asXaX_andXselectXaX_selectXaXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/AndTest.java
        /// Gremlin: g.V().as("a").and(select("a"), select("a"));
        /// </summary>
        [TestMethod]
        public void AndWithSelect()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .As("a")
                    .And(
                        GraphTraversal2.__().Select("a"),
                        GraphTraversal2.__().Select("a"));
                List<string> result = traversal.Next();

                Assert.AreEqual(6, result.Count());
            }
        }
    }
}
