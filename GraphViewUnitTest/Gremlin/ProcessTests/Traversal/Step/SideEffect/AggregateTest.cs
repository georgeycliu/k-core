using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class AggregateTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_name_aggregateXxX_capXxX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/OptionalTest.java
        /// Gremlin: g.V().values("name").aggregate("x").cap("x");
        /// </summary>
        [TestMethod]
        public void get_g_V_name_aggregateXxX_capXxX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Values("name").Aggregate("x").Cap("x");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                CheckUnOrderedResults(new [] {"marko", "vadas", "lop", "josh", "ripple", "peter"}, ((JArray)results[0]).Select(p=>p.ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_aggregateXxX_byXnameX_capXxX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/OptionalTest.java
        /// Gremlin: g.V().aggregate("x").by("name").cap("x");
        /// </summary>
        [TestMethod]
        public void get_g_V_aggregateXxX_byXnameX_capXxX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Aggregate("x").By("name").Cap("x");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                CheckUnOrderedResults(new[] { "marko", "vadas", "lop", "josh", "ripple", "peter" }, ((JArray)results[0]).Select(p => p.ToString()).ToList());
            }
        }

        /// <summary>
        /// get_g_V_out_aggregateXaX_path()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/OptionalTest.java
        /// Gremlin: g.V().out().aggregate("a").path();
        /// </summary>
        [TestMethod]
        public void get_g_V_out_aggregateXaX_path()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection)) {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out().Aggregate("a").Path();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                string markoId = this.ConvertToVertexId(command, "marko");
                string lopId = this.ConvertToVertexId(command, "lop");
                string vadasId = this.ConvertToVertexId(command, "vadas");
                string joshId = this.ConvertToVertexId(command, "josh");
                string rippleId = this.ConvertToVertexId(command, "ripple");
                string peterId = this.ConvertToVertexId(command, "peter");

                List<string> path1 = new List<string> { markoId, lopId };
                List<string> path2 = new List<string> { markoId, vadasId };
                List<string> path3 = new List<string> { markoId, joshId };
                List<string> path4 = new List<string> { joshId, rippleId };
                List<string> path5 = new List<string> { joshId, lopId };
                List<string> path6 = new List<string> { peterId, lopId };

                List<string> expect = new List<string>
                {
                    string.Join(",", path1),
                    string.Join(",", path2),
                    string.Join(",", path3),
                    string.Join(",", path4),
                    string.Join(",", path5),
                    string.Join(",", path6),
                };

                List<string> ans = new List<string>();
                foreach (dynamic result in results) {
                    List<string> steps = new List<string>();
                    foreach (dynamic step in result["objects"])
                    {
                        steps.Add(step["id"].ToString());
                    }
                    ans.Add(string.Join(",", steps));
                }

                CheckUnOrderedResults(expect, ans);
            }
        }

        /// <summary>
        /// get_g_V_hasLabelXpersonX_aggregateXxX_byXageX_capXxX_asXyX_selectXyX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/sideEffect/OptionalTest.java
        /// Gremlin: g.V().hasLabel("person").aggregate("x").by("age").cap("x").as("y").select("y");
        /// </summary>
        [TestMethod]
        public void get_g_V_hasLabelXpersonX_aggregateXxX_byXageX_capXxX_asXyX_selectXyX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal =
                    command.g().V().HasLabel("person").Aggregate("x").By("age").Cap("x").As("y").Select("y");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                CheckUnOrderedResults(new [] {29, 27, 32, 35}, ((JArray)results[0]).Select(p=>int.Parse(p.ToString())).ToList());
            }
        }
    }
}