﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    [TestClass]
    public class ChooseTest : AbstractGremlinTest
    {
        /// <summary>
        /// get_g_V_chooseXout_countX_optionX2L__nameX_optionX3L__valueMapX()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/ChooseTest.java
        /// Gremlin: g.V().choose(out().count()).option(2, __.values("name")).option(3, __.valueMap())
        /// </summary>
        [TestMethod]
        public void get_g_V_chooseXout_countX_optionX2L__nameX_optionX3L__valueMapX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .Choose(GraphTraversal2.__().Out().Count())
                    .Option(2, GraphTraversal2.__().Values("name"))
                    .Option(3, GraphTraversal2.__().ValueMap());
                List<string> result = traversal.Next();

                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("josh", result[0]);
                Assert.AreEqual("[name:[marko], age:[29]]", result[1]);
            }
        }

        /// <summary>
        /// get_g_V_chooseXhasLabelXpersonX_and_outXcreatedX__outXknowsX__identityX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/ChooseTest.java
        /// Gremlin: g.V().choose(hasLabel("person").and().out("created"), out("knows"), identity()).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_chooseXhasLabelXpersonX_and_outXcreatedX__outXknowsX__identityX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .Choose(GraphTraversal2.__().HasLabel("person").And().Out("created"),
                        GraphTraversal2.__().Out("knows"), GraphTraversal2.__().Identity()).Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new [] { "lop", "ripple", "josh", "vadas", "vadas" }, result);
            }
        }

        /// <summary>
        /// get_g_V_chooseXlabelX_optionXblah__outXknowsXX_optionXbleep__outXcreatedXX_optionXnone__identityX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/ChooseTest.java
        /// Gremlin: g.V().choose(label()).option("blah", out("knows")).option("bleep", out("created")).option(Pick.none, identity()).values("name")
        /// </summary>
        [TestMethod]
        public void get_g_V_chooseXlabelX_optionXblah__outXknowsXX_optionXbleep__outXcreatedXX_optionXnone__identityX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .Choose(GraphTraversal2.__().Label())
                    .Option("blah", GraphTraversal2.__().Out("knows"))
                    .Option("bleep", GraphTraversal2.__().Out("created"))
                    .Option(GremlinKeyword.Pick.None, GraphTraversal2.__().Identity())
                    .Values("name");
                List<string> result = traversal.Next();

                AbstractGremlinTest.CheckUnOrderedResults(new [] { "marko", "vadas", "peter", "josh", "lop", "ripple" }, result);
            }
        }

        /// <summary>
        /// get_g_V_chooseXoutXknowsX_count_isXgtX0XX__outXknowsXX_name()
        /// from org/apache/tinkerpop/gremlin/process/traversal/step/branch/ChooseTest.java
        /// Gremlin: g.V().choose(out("knows").count().is(gt(0)), out("knows")).values("name");
        /// </summary>
        [TestMethod]
        public void get_g_V_chooseXoutXknowsX_count_isXgtX0XX__outXknowsXX_name()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V()
                    .Choose(GraphTraversal2.__().Out("knows").Count().Is(Predicate.gt(0)), GraphTraversal2.__().Out("knows"))
                    .Values("name");
                List<string> result = traversal.Next();

                CheckUnOrderedResults(new [] { "vadas", "josh", "vadas", "josh", "peter", "lop", "ripple" }, result);
            }
        }
    }
}