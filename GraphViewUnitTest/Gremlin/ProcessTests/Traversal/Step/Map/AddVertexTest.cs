using System.Collections.Generic;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin
{
    [TestClass]
    public class AddVertexTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_VX1X_addVXanimalX_propertyXage_selectXaX_byXageXX_propertyXname_puppyX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.V(v1Id).as("a").addV("animal").property("age", select("a").by("age")).property("name", "puppy");
        /// </summary>
        /// Can' support property("age", select("a").by("age")) : a traversal as the property value
        [TestMethod]
        [Ignore]
        public void get_g_VX1X_addVXanimalX_propertyXage_selectXaX_byXageXX_propertyXname_puppyX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string markoId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoId).As("a").AddV("animal").Property("age", GraphTraversal2.__().Select("a").By("age")).Property("name", "puppy");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

            }
        }

        /// <summary>
        /// get_g_V_addVXanimalX_propertyXage_0X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.V().addV("animal").property("age", 0);
        /// </summary>
        [TestMethod]
        public void get_g_V_addVXanimalX_propertyXage_0X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().AddV("animal").Property("age", 0);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                int count = 0;
                foreach (dynamic result in results)
                {
                    Assert.AreEqual("animal", result[GremlinKeyword.Label].ToString());
                    Assert.AreEqual(0, int.Parse(result["properties"]["age"][0]["value"].ToString()));
                    count++;
                }
                Assert.AreEqual(6, count);

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(12, command.g().V().Next().Count);
            }
        }

        /// <summary>
        /// get_g_addVXpersonX_propertyXname_stephenX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.addV("person").property("name", "stephen");
        /// </summary>
        [TestMethod]
        public void get_g_addVXpersonX_propertyXname_stephenX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().AddV("person").Property("name", "stephen");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual("person", results[0][GremlinKeyword.Label].ToString());
                Assert.AreEqual("stephen", results[0]["properties"]["name"][0]["value"].ToString());

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(1, command.g().V().Has("name", "stephen").Properties().Next().Count);
                Assert.AreEqual(7, command.g().V().Next().Count);
            }
        }

        /// <summary>
        /// get_g_addVXpersonX_propertyXname_stephenX_propertyXname_stephenmX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.addV("person").property("name", "stephen").property("name", "stephenm");
        /// </summary>
        [TestMethod]
        public void get_g_addVXpersonX_propertyXname_stephenX_propertyXname_stephenmX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().AddV("person").Property("name", "stephen").Property("name", "stephenm");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual("person", results[0][GremlinKeyword.Label].ToString());
                Assert.AreEqual("stephen", results[0]["properties"]["name"][0]["value"].ToString());

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(2, command.g().V().Has("name", "stephen").Properties().Next().Count);
                Assert.AreEqual(7, command.g().V().Next().Count);
            }
        }

        /// <summary>
        /// get_g_addVXpersonX_propertyXsingle_name_stephenX_propertyXsingle_name_stephenmX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.addV("person").property(VertexProperty.Cardinality.single, "name", "stephen").property(VertexProperty.Cardinality.single, "name", "stephenm");
        /// </summary>
        [TestMethod]
        public void get_g_addVXpersonX_propertyXsingle_name_stephenX_propertyXsingle_name_stephenmX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().AddV("person").Property(GremlinKeyword.PropertyCardinality.Single, "name", "stephen").Property(GremlinKeyword.PropertyCardinality.Single, "name", "stephenm");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual("person", results[0][GremlinKeyword.Label].ToString());
                Assert.AreEqual("stephenm", results[0]["properties"]["name"][0]["value"].ToString());

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(1, command.g().V().Has("name", "stephenm").Properties().Next().Count);
                Assert.AreEqual(7, command.g().V().Next().Count);
            }
        }

        /// <summary>
        /// get_g_addVXpersonX_propertyXsingle_name_stephenX_propertyXsingle_name_stephenm_since_2010X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.addV("person").property(VertexProperty.Cardinality.single, "name", "stephen").property(VertexProperty.Cardinality.single, "name", "stephenm", "since", 2010);
        /// </summary>
        [TestMethod]
        public void get_g_addVXpersonX_propertyXsingle_name_stephenX_propertyXsingle_name_stephenm_since_2010X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().AddV("person").Property(GremlinKeyword.PropertyCardinality.Single, "name", "stephen").Property(GremlinKeyword.PropertyCardinality.Single, "name", "stephenm", "since", 2010);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual("person", results[0][GremlinKeyword.Label].ToString());
                Assert.AreEqual("stephenm", results[0]["properties"]["name"][0]["value"].ToString());

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(2010, int.Parse(command.g().V().Has("name", "stephenm").Properties("name").Properties().Value().Next().First()));
                Assert.AreEqual(1, command.g().V().Has("name", "stephenm").Properties().Next().Count);
                Assert.AreEqual(1, command.g().V().Has("name", "stephenm").Properties().Properties().Next().Count);
                Assert.AreEqual(7, command.g().V().Next().Count);
            }
        }

        /// <summary>
        /// get_g_V_hasXname_markoX_propertyXfriendWeight_outEXknowsX_weight_sum__acl_privateX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.V().has("name", "marko").property("friendWeight", __.outE("knows").values("weight").sum(), "acl", "private");
        /// </summary>
        /// Can' support property("age", select("a").by("age")) : a traversal as the property value
        [TestMethod]
        [Ignore]
        public void get_g_V_hasXname_markoX_propertyXfriendWeight_outEXknowsX_weight_sum__acl_privateX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Has("name", "marko").Property("friendWeight", GraphTraversal2.__().OutE("knows").Values("weight").Sum(), "acl", "private");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

            }
        }

        /// <summary>
        /// get_g_addVXanimalX_propertyXname_mateoX_propertyXname_gateoX_propertyXname_cateoX_propertyXage_5X()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.addV("animal").property("name", "mateo").property("name", "gateo").property("name", "cateo").property("age", 5);
        /// </summary>
        [TestMethod]
        public void get_g_addVXanimalX_propertyXname_mateoX_propertyXname_gateoX_propertyXname_cateoX_propertyXage_5X()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().AddV("animal").Property("name", "mateo").Property("name", "gateo").Property("name", "cateo").Property("age", 5);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual("animal", results[0][GremlinKeyword.Label].ToString());
                
                List<string> ansProperties = new List<string>();
                foreach (dynamic property in results[0]["properties"]["name"]) {
                    ansProperties.Add(property["value"].ToString());
                }
                List<string> expectProperties = new List<string> {"mateo", "gateo", "cateo" };
                CheckUnOrderedResults(expectProperties, ansProperties);

                command.OutputFormat = OutputFormat.Regular;
                Assert.AreEqual(3, command.g().V().HasLabel("animal").Properties("name").Next().Count);
                Assert.AreEqual(5, int.Parse(command.g().V().HasLabel("animal").Values("age").Next().First()));
            }
        }

        /// <summary>
        /// get_g_V_addVXanimalX_propertyXname_valuesXnameXX_propertyXname_an_animalX_propertyXvaluesXnameX_labelX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.V().addV("animal").property("name", __.values("name")).property("name", "an animal").property(__.values("name"), __.label());
        /// </summary>
        /// Can' support property("age", select("a").by("age")) : a traversal as the property key and value
        [TestMethod]
        [Ignore]
        public void get_g_V_addVXanimalX_propertyXname_valuesXnameXX_propertyXname_an_animalX_propertyXvaluesXnameX_labelX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                //command.OutputFormat = OutputFormat.GraphSON;
                //GraphTraversal2 traversal = command.g().V().AddV("animal").Property("name", GraphTraversal2.__().Values("name")).Property("name", "an animal").Property(GraphTraversal2.__().Values("name"), GraphTraversal2.__().Label());
                //dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

            }
        }

        /// <summary>
        /// get_g_withSideEffectXa_testX_V_hasLabelXsoftwareX_propertyXtemp_selectXaXX_valueMapXname_tempX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.withSideEffect("a", "test").V().hasLabel("software").property("temp", select("a")).valueMap("name", "temp");
        /// </summary>
        /// Can't support withSideEffect
        [TestMethod]
        [Ignore]
        public void get_g_withSideEffectXa_testX_V_hasLabelXsoftwareX_propertyXtemp_selectXaXX_valueMapXname_tempX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

            }
        }

        /// <summary>
        /// get_g_withSideEffectXa_markoX_addV_propertyXname_selectXaXX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/AddVertexTest.java
        /// Gremlin: g.withSideEffect("a", "marko").addV().property("name", select("a")).values("name");
        /// </summary>
        /// Can't support withSideEffect
        [TestMethod]
        [Ignore]
        public void get_g_withSideEffectXa_markoX_addV_propertyXname_selectXaXX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

            }
        }

    }
}
