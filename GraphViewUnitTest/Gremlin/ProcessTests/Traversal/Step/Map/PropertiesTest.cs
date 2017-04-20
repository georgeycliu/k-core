﻿using System.Collections.Generic;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    /// <summary>
    /// Tests for the Properties Step.
    /// </summary>
    [TestClass]
    public class PropertiesTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_V_hasXageX_propertiesXname_ageX_value UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/PropertiesTest.java.
        /// Equivalent gremlin: "g.V.has('age').properties('name', 'age').value"
        /// </summary>
        [TestMethod]
        public void VerticesHasAgePropertiesNameAgeValue()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("age").Properties("name", "age").Value();

                List<string> result = traversal.Next();

                List<string> expected = new List<string> { "marko", "29", "vadas", "27", "josh", "32", "peter", "35" };
                CheckOrderedResults(expected, result);
            }
        }

        /// <summary>
        /// Port of the g_V_hasXageX_propertiesXage_nameX_value UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/PropertiesTest.java.
        /// Equivalent gremlin: "g.V.has('age').properties('age', 'name').value"
        /// </summary>
        [TestMethod]
        public void VerticesHasAgePropertiesAgeNameValue()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("age").Properties("age", "name").Value();

                List<string> result = traversal.Next();

                List<string> expected = new List<string> { "29", "marko", "27", "vadas", "32", "josh", "35", "peter" };
                CheckOrderedResults(expected, result);
            }
        }

        /// <summary>
        /// Port of the g_V_hasXageX_properties_hasXid_nameIdX_value UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/PropertiesTest.java.
        /// Equivalent gremlin: "g.V.has('age').properties().has(T.id, nameId).value()", "nameId", nameId
        /// </summary>
        [TestMethod]
        public void VerticesHasAgePropertiesHasIdNameIdValue()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoNameVertexPropertyId = this.ConvertToPropertyId(command, "marko", "name", "marko");

                GraphTraversal2 traversal = command.g().V().Has("age").Properties().Has("id", markoNameVertexPropertyId).Value();

                List<string> result = traversal.Next();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("marko", result.First());
            }
        }

        /// <summary>
        /// Port of the g_V_hasXageX_propertiesXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/PropertiesTest.java.
        /// Equivalent gremlin: "g.V.has('age').properties('name')"
        /// </summary>
        [TestMethod]
        public void VerticesHasAgePropertiesName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Has("age").Properties("name");

                List<string> result = traversal.Next();

                Assert.AreEqual(4, result.Count);

                List<string> expected = new List<string> { "vp[name->marko]", "vp[name->vadas]", "vp[name->josh]", "vp[name->peter]" };
                CheckUnOrderedResults(expected, result);
            }
        } 
    }
}
