﻿using System;
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
    public class FoldTest : AbstractGremlinTest
    {
        /// <summary>
        /// g_V_fold()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/FoldTest.java
        /// Gremlin: g.V().fold();
        /// </summary>
        [TestMethod]
        public void BasicFold()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Fold();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.Next().FirstOrDefault());
                command.OutputFormat = OutputFormat.Regular;

                Assert.AreEqual(1, ((JArray)result).Count);
                Assert.AreEqual(6, ((JArray)result[0]).Count);
            }
        }

        /// <summary>
        /// g_V_fold_unfold()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/map/FoldTest.java
        /// Gremlin: g.V().fold().unfold();
        /// </summary>
        [TestMethod]
        public void FoldThenUnfold()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Fold().Unfold();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(traversal.Next().FirstOrDefault());
                command.OutputFormat = OutputFormat.Regular;

                Assert.AreEqual(6, ((JArray)result).Count);
            }
        }
    }
}
