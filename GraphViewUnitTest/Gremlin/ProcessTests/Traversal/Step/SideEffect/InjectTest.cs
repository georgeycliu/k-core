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
    public class InjectTest : AbstractGremlinTest
    {
        /// <summary>
        /// Original test
        /// Gremlin: g.V().has("name","marko").values("name").inject("daniel");
        /// </summary>
        [TestMethod]
        public void BasicInject()
        {
            using (GraphViewCommand graphCommand = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = graphCommand.g().V()
                    .Has("name", "marko")
                    .Values("name")
                    .Inject("daniel");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new[] { "marko", "daniel" }, result);
            }
        }
    }
}
