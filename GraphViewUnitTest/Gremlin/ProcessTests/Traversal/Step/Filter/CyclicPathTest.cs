using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class CyclicPathTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_VX1X_outXcreatedX_inXcreatedX_cyclicPath()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/CyclicPathTest.java
        /// Gremlin: g.V(v1Id).out("created").in("created").cyclicPath();
        /// </summary>
        [TestMethod]
        public void g_VX1X_outXcreatedX_inXcreatedX_cyclicPath()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection)) {
                command.OutputFormat = OutputFormat.GraphSON;
                string markoId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoId).Out("created").In("created").CyclicPath();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(markoId, results[0]["id"].ToString());
            }
        }

        /// <summary>
        /// get_g_VX1X_outXcreatedX_inXcreatedX_cyclicPath_path()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/filter/CyclicPathTest.java
        /// Gremlin: g.V(v1Id).out("created").in("created").cyclicPath().path();
        /// </summary>
        [TestMethod]
        public void get_g_VX1X_outXcreatedX_inXcreatedX_cyclicPath_path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string markoId = this.ConvertToVertexId(command, "marko");
                GraphTraversal2 traversal = command.g().V(markoId).Out("created").In("created").CyclicPath().Path();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<string> ans = new List<string>();
                foreach (dynamic result in results)
                {
                    Console.WriteLine(result);
                    List<string> steps = new List<string>();
                    foreach (dynamic step in result["objects"])
                    {
                        steps.Add(step["id"].ToString());
                    }
                    ans.Add(string.Join(",", steps));
                }

                string lopId = this.ConvertToVertexId(command, "lop");

                List<string> path1 = new List<string> { markoId, lopId, markoId };
                List<string> expect = new List<string>
                {
                    string.Join(",", path1)
                };
                CheckUnOrderedResults(expect, ans);
            }
        }
    }
}