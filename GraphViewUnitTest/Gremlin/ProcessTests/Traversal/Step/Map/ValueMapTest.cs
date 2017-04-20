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
    public class ValueMapTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_valueMap()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/ValueMapTest.java
        /// Gremlin: g.V().valueMap()
        /// </summary>
        [TestMethod]
        public void get_g_V_valueMap()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().ValueMap();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(6, results.Count);
                foreach (dynamic result in results) {
                    string name = result["name"][0].ToString();
                    if (name.Equals("marko")) {
                        Assert.AreEqual(29, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("josh")) {
                        Assert.AreEqual(32, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("peter")) {
                        Assert.AreEqual(35, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("vadas")) {
                        Assert.AreEqual(27, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("lop")) {
                        Assert.AreEqual("java", result["lang"][0].ToString());
                    }
                    else if (name.Equals("ripple")) {
                        Assert.AreEqual("java", result["lang"][0].ToString());
                    }
                    else {
                        Assert.Fail("It is not possible to reach here: ");
                    }
                }
            }
        }

        /// <summary>
        /// get_g_V_valueMapXname_ageX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/ValueMapTest.java
        /// Gremlin: g.V().valueMap("name", "age")
        /// </summary>
        [TestMethod]
        public void get_g_V_valueMapXname_ageX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().ValueMap("name", "age");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(6, results.Count);
                int count = 0;
                foreach (dynamic result in results)
                {
                    string name = result["name"][0].ToString();
                    if (name.Equals("marko")) {
                        Assert.AreEqual(29, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("josh")) {
                        Assert.AreEqual(32, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("peter")) {
                        Assert.AreEqual(35, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("vadas")) {
                        Assert.AreEqual(27, int.Parse(result["age"][0].ToString()));
                    }
                    else if (name.Equals("lop")) {
                        count++;
                    }
                    else if (name.Equals("ripple")) {
                        count++;
                    }
                    else {
                        Assert.Fail("It is not possible to reach here: ");
                    }
                }
                Assert.AreEqual(2, count);
            }
        }

        /// <summary>
        /// get_g_VX1X_outXcreatedX_valueMap()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/ValueMapTest.java
        /// Gremlin: g.V(v1Id).out("created").valueMap();
        /// </summary>
        [TestMethod]
        public void get_g_VX1X_outXcreatedX_valueMap()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string markoId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoId).Out("created").ValueMap();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.IsTrue("lop" == results[0]["name"][0].ToString() && "java" == results[0]["lang"][0].ToString());
            }
        }
    }
}
