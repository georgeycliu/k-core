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
    public class OptionalTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_VX2X_optionalXoutXknowsXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/OptionalTest.java
        /// Gremlin: g.V(2).optional(out("know"))
        /// </summary>
        [TestMethod]
        public void get_g_VX2X_optionalXoutXknowsXX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection)) {

                command.OutputFormat = OutputFormat.GraphSON;
                string vadasId = this.ConvertToVertexId(command, "vadas");
                GraphTraversal2 traversal = command.g().V(vadasId).Optional(GraphTraversal2.__().Out("know"));
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual(vadasId, results[0]["id"].ToString());
            }
        }

        /// <summary>
        /// get_g_VX2X_optionalXinXknowsXX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/OptionalTest.java
        /// Gremlin: g.V(v2Id).optional(in("knows"));
        /// </summary>
        [TestMethod]
        public void get_g_VX2X_optionalXinXknowsXX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                string vadasId = this.ConvertToVertexId(command, "vadas");
                GraphTraversal2 traversal = command.g().V(vadasId).Optional(GraphTraversal2.__().In("know"));
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                Assert.AreEqual(vadasId, results[0]["id"].ToString());
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXpersonX_optionalXoutXknowsX_optionalXoutXcreatedXXX_path()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/OptionalTest.java
        /// Gremlin: g.V().hasLabel("person").optional(out("knows").optional(out("created"))).path();
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXpersonX_optionalXoutXknowsX_optionalXoutXcreatedXXX_path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("person").Optional(GraphTraversal2.__().Out("knows").Optional(GraphTraversal2.__().Out("created"))).Path();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                List<string> ans = new List<string>();
                foreach (dynamic result in results) {
                    Console.WriteLine(result);
                    List<string> steps = new List<string>();
                    foreach (dynamic step in result["objects"]) {
                        steps.Add(step["id"].ToString());
                    }
                    ans.Add(string.Join(",", steps));
                }

                string markoId = this.ConvertToVertexId(command, "marko");
                string lopId = this.ConvertToVertexId(command, "lop");
                string vadasId = this.ConvertToVertexId(command, "vadas");
                string joshId = this.ConvertToVertexId(command, "josh");
                string rippleId = this.ConvertToVertexId(command, "ripple");
                string peterId = this.ConvertToVertexId(command, "peter");

                List<string> path1 = new List<string> { markoId, vadasId };
                List<string> path2 = new List<string> { markoId, joshId, rippleId };
                List<string> path3 = new List<string> { markoId, joshId, lopId };
                List<string> path4 = new List<string> { vadasId };
                List<string> path5 = new List<string> { joshId };
                List<string> path6 = new List<string> { peterId };
                List<string> expect = new List<string>
                {
                    string.Join(",", path1),
                    string.Join(",", path2),
                    string.Join(",", path3),
                    string.Join(",", path4),
                    string.Join(",", path5),
                    string.Join(",", path6),
                };
                CheckUnOrderedResults(expect, ans);
            }
        }

        /// <summary>
        /// get_g_V_optionalXout_optionalXoutXX_path()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/OptionalTest.java
        /// Gremlin: g.V().optional(out().optional(out())).path();
        /// </summary>
        [TestMethod]
        public void get_g_V_optionalXout_optionalXoutXX_path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Optional(GraphTraversal2.__().Out().Optional(GraphTraversal2.__().Out())).Path();
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

                string markoId = this.ConvertToVertexId(command, "marko");
                string lopId = this.ConvertToVertexId(command, "lop");
                string vadasId = this.ConvertToVertexId(command, "vadas");
                string joshId = this.ConvertToVertexId(command, "josh");
                string rippleId = this.ConvertToVertexId(command, "ripple");
                string peterId = this.ConvertToVertexId(command, "peter");

                List<string> path1 = new List<string> { markoId, lopId };
                List<string> path2 = new List<string> { markoId, vadasId };
                List<string> path3 = new List<string> { markoId, joshId, rippleId };
                List<string> path4 = new List<string> { markoId, joshId, lopId };
                List<string> path5 = new List<string> { vadasId };
                List<string> path6 = new List<string> { lopId };
                List<string> path7 = new List<string> { joshId, rippleId };
                List<string> path8 = new List<string> { joshId, lopId };
                List<string> path9 = new List<string> { rippleId };
                List<string> path10 = new List<string> { peterId, lopId };
                List<string> expect = new List<string>
                {
                    string.Join(",", path1),
                    string.Join(",", path2),
                    string.Join(",", path3),
                    string.Join(",", path4),
                    string.Join(",", path5),
                    string.Join(",", path6),
                    string.Join(",", path7),
                    string.Join(",", path8),
                    string.Join(",", path9),
                    string.Join(",", path10),
                };
                CheckUnOrderedResults(expect, ans);
            }
        }
    }
}