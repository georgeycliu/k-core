﻿using System.Collections.Generic;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Filter
{
    /// <summary>
    /// Tests for Is Step.
    /// </summary>
    [TestClass]
    public class IsTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_valuesXageX_isX32X UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/IsTest.java.
        /// Equivalent gremlin: "g.V.age.is(32)"
        /// </summary>
        [TestMethod]
        public void VerticesValuesAgeIs32()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                // NOTE: original test checks for result returned to be type of Integer, but we aren't doing so.
                GraphTraversal2 traversal = command.g().V().Values("age").Is(32);

                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("32", result.First());
            }
        }

        /// <summary>
        /// Port of the g_V_valuesXageX_isXlte_30X UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/IsTest.java.
        /// Equivalent gremlin: "gremlin-groovy", "g.V.age.is(lte(30))"
        /// </summary>
        [TestMethod]
        public void VerticesValuesAgeIsLTE30()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Is(Predicate.lte(30));

                List<string> result = traversal.Next();
                Assert.AreEqual(2, result.Count);
                CheckUnOrderedResults(new List<string> { "27", "29" }, result);
            }
        }

        /// <summary>
        /// Port of the g_V_valuesXageX_isXgte_29X_isXlt_34X UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/IsTest.java.
        /// Equivalent gremlin: "gremlin-groovy", "g.V.age.is(gte(29)).is(lt(34))"
        /// </summary>
        [TestMethod]
        public void VerticesValuesAgeIsGTE29IsLT34()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Values("age").Is(Predicate.gte(29)).Is(Predicate.lt(34));

                List<string> result = traversal.Next();
                Assert.AreEqual(2, result.Count);
                CheckUnOrderedResults(new List<string> { "29", "32" }, result);
            }
        }

        /// <summary>
        /// Port of the g_V_whereXinXcreatedX_count_isX1XX_valuesXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/IsTest.java.
        /// Equivalent gremlin: "g.V.where(__.in('created').count.is(1)).name"
        /// </summary>
        [TestMethod]
        public void VerticesWhereInCreatedCountIs1ValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Where(GraphTraversal2.__().In("created").Count().Is(1)).Values("name");

                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("ripple", result.First());
            }
        }

        /// <summary>
        /// Port of the g_V_whereXinXcreatedX_count_isXgte_2XX_valuesXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/IsTest.java.
        /// Equivalent gremlin: "gremlin-groovy", "g.V.where(__.in('created').count.is(gte(2l))).name"
        /// </summary>
        [TestMethod]
        public void VerticesWhereInCreatedCountIsGTE2ValuesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Where(GraphTraversal2.__().In("created").Count().Is(Predicate.gte(2L))).Values("name");

                List<string> result = traversal.Next();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("lop", result.First());
            }
        }
    }
}
