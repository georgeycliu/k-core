using System.Collections.Generic;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace GraphViewUnitTest.Gremlin.ProcessTests.Traversal.Step.Map
{
    /// <summary>
    /// Tests for the Select Step.
    /// </summary>
    [TestClass]
    public class SelectTest : AbstractGremlinTest
    {
        /// <summary>
        /// Port of the g_VX1X_asXaX_outXknowsX_asXbX_selectXa_bX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).as('a').out('knows').as('b').select('a','b')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdAsAOutKnowsAsBSelectAB()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");
                string vadasVertexId = this.ConvertToVertexId(command, "vadas");
                string joshVertexId = this.ConvertToVertexId(command, "josh");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .HasId(markoVertexId).As("a")
                    .Out("knows").As("b")
                    .Select("a", "b");

                List<string> result = traversal.Next();

                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual(markoVertexId, res["a"]["id"].ToString());
                    Assert.IsTrue(vadasVertexId.Equals(res["b"]["id"].ToString()) || joshVertexId.Equals(res["b"]["id"].ToString()));
                }
            }
        }

        /// <summary>
        /// Port of the g_VX1X_asXaX_outXknowsX_asXbX_selectXa_bX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).as('a').out('knows').as('b').select('a','b').by('name')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdAsAOutKnowsAsBSelectABByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .HasId(markoVertexId).As("a")
                    .Out("knows").As("b")
                    .Select("a", "b").By("name");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual("marko", res["a"].ToString());
                    Assert.IsTrue("vadas".Equals(res["b"].ToString()) || "josh".Equals(res["b"].ToString()));
                }
            }
        }

        /// <summary>
        /// Port of the g_VX1X_asXaX_outXknowsX_asXbX_selectXaX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).as('a').out('knows').as('b').select('a')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdAsAOutKnowsAsBSelectA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .HasId(markoVertexId).As("a")
                    .Out("knows").As("b")
                    .Select("a");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual(markoVertexId, res["id"].ToString());
                }
            }
        }

        /// <summary>
        /// Port of the g_VX1X_asXaX_outXknowsX_asXbX_selectXaX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).as('a').out('knows').as('b').select('a').by('name')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdAsAOutKnowsAsBSelectAByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string markoVertexId = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .HasId(markoVertexId).As("a")
                    .Out("knows").As("b")
                    .Select("a").By("name");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual("marko", res.ToString());
                }
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_out_asXbX_selectXa_bX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.as('a').out.as('b').select('a','b').by('name')"
        /// </summary>
        [TestMethod]
        public void VerticesAsAOutAsBSelectABByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .As("a")
                    .Out().As("b")
                    .Select("a", "b").By("name");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                List<string> ans = new List<string>();
                foreach (dynamic temp in dynamicResult)
                {
                    ans.Add("a," + temp["a"].ToString() + ";b," + temp["b"].ToString());
                }
                this.AssertCommonA(ans);
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_out_aggregateXxX_asXbX_selectXa_bX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.as('a').out.aggregate('x').as('b').select('a','b').by('name')"
        /// </summary>
        [TestMethod]
        public void VerticesAsAOutAggregateXAsBSelectABByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .As("a")
                    .Out().Aggregate("x").As("b")
                    .Select("a", "b").By("name");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                List<string> ans = new List<string>();
                foreach (dynamic temp in dynamicResult)
                {
                    ans.Add("a," + temp["a"].ToString() + ";b," + temp["b"].ToString());
                }
                this.AssertCommonA(ans);
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_name_order_asXbX_selectXa_bX_byXnameX_by_XitX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V().as('a').name.order().as('b').select('a','b').by('name').by"
        /// </summary>
        [TestMethod]
        public void VerticesAsAValuesNameOrderAsBSelectABByNameBy()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .As("a")
                    .Values("name")
                    .Order().As("b").
                        Select("a", "b").By("name")
                    .By();

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                List<string> ans = new List<string>();
                foreach (dynamic temp in dynamicResult)
                {
                    ans.Add("a," + temp["a"].ToString() + ";b," + temp["b"].ToString());
                }
                List<string> expected = new List<string>
                {
                    "a,marko;b,marko",
                    "a,vadas;b,vadas",
                    "a,josh;b,josh",
                    "a,ripple;b,ripple",
                    "a,lop;b,lop",
                    "a,peter;b,peter"
                };

                CheckUnOrderedResults(expected, ans);
            }
        }

        /// <summary>
        /// Port of the g_V_hasXname_isXmarkoXX_asXaX_selectXaX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.has('name',__.is('marko')).as('a').select('a')"
        /// </summary>
        [TestMethod]
        public void HasNameIsMarkoAsASelectA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .Has("name", GraphTraversal2.__().Is("marko"))
                    .As("a").Select("a");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                Assert.AreEqual(1, dynamicResult.Count);
                Assert.AreEqual(this.ConvertToVertexId(command, "marko"), (string)dynamicResult[0]["id"]);
            }
        }

        /// <summary>
        /// Port of the g_V_label_groupCount_asXxX_selectXxX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V().label().groupCount().as('x').select('x')"
        /// </summary>
        [TestMethod]
        public void VerticesLabelGroupCountAsXSelectX()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Label().GroupCount().As("x").Select("x");
                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());
                
                Assert.AreEqual(2, int.Parse(results[0]["software"].ToString()));
                Assert.AreEqual(4, int.Parse(results[0]["person"].ToString()));
            }
        }

        /// <summary>
        /// Port of the g_V_hasLabelXpersonX_asXpX_mapXbothE_label_groupCountX_asXrX_selectXp_rX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.hasLabel('person').as('p').map(__.bothE.label.groupCount()).as('r').select('p','r')"
        /// </summary>
        [TestMethod]
        public void HasLabelPersonAsPMapBothELabelGroupCountAsRSelectPR()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V()
                    .HasLabel("person").As("p")
                    .Map(GraphTraversal2.__().BothE().Label().GroupCount()).As("r").Select("p", "r");

                List<string> results = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(results.FirstOrDefault());

                Assert.AreEqual(4, dynamicResult.Count);
                foreach (dynamic result in dynamicResult)
                {
                    if ((string) result.p.id == this.ConvertToVertexId(command, "marko"))
                    {
                        Assert.AreEqual(1, (int)result.r["created"]);
                        Assert.AreEqual(2, (int)result.r["knows"]);
                    }
                    else if ((string)result.p.id == this.ConvertToVertexId(command, "vadas"))
                    {
                        Assert.AreEqual(1, (int)result.r["knows"]);
                    }
                    else if ((string)result.p.id == this.ConvertToVertexId(command, "josh"))
                    {
                        Assert.AreEqual(2, (int)result.r["created"]);
                        Assert.AreEqual(1, (int)result.r["knows"]);
                    }
                    else if ((string) result.p.id == this.ConvertToVertexId(command, "peter"))
                    {
                        Assert.AreEqual(1, (int) result.r["created"]);
                    }
                    else
                    {
                        Assert.Fail("Unknown vertex result");
                    }
                }
            }
        }

        /// <summary>
        /// Port of the g_V_chooseXoutE_count_isX0X__asXaX__asXbXX_chooseXselectXaX__selectXaX__selectXbXX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.choose(__.outE().count().is(0L), __.as('a'), __.as('b')).choose(select('a'),select('a'),select('b'))"
        /// </summary>
        [TestMethod]
        public void ChooseOutECountIs0AsAsBChooseSelectASelectASelectB()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Choose(
                                                        GraphTraversal2.__().OutE().Count().Is(0L),
                                                        GraphTraversal2.__().As("a"),
                                                        GraphTraversal2.__().As("b"))
                                                    .Choose(
                                                        GraphTraversal2.__().Select("a"),
                                                        GraphTraversal2.__().Select("a"),
                                                        GraphTraversal2.__().Select("b"));

                List<string> results = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(results.FirstOrDefault());

                int counter = 0;
                int xCounter = 0;
                int yCounter = 0;

                foreach (dynamic result in dynamicResult)
                {
                    counter++;
                    if ((string)result.id == this.ConvertToVertexId(command, "vadas")
                        || (string)result.id == this.ConvertToVertexId(command, "lop")
                        || (string)result.id == this.ConvertToVertexId(command, "ripple"))
                        xCounter++;
                    else if ((string)result.id == this.ConvertToVertexId(command, "marko")
                        || (string)result.id == this.ConvertToVertexId(command, "josh")
                        || (string)result.id == this.ConvertToVertexId(command, "peter"))
                        yCounter++;
                    else
                        Assert.Fail("This state should not have occurred");
                }

                Assert.AreEqual(6, counter);
                Assert.AreEqual(3, yCounter);
                Assert.AreEqual(3, xCounter);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_asXhereX_out_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).as('here').out.select('here')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdAsHereOutSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).As("here").Out().Select("here");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(3, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual("marko", res["properties"]["name"][0]["value"].ToString());
                }
            }
        }

        /// <summary>
        /// Port of the g_VX4X_out_asXhereX_hasXlang_javaX_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v4Id).out.as('here').has('lang', 'java').select('here')", "v4Id", v4Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdOutAsHereHasLangJavaSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "josh");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).Out().As("here").Has("lang", "java").Select("here");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual("java", res["properties"]["lang"][0]["value"].ToString());
                    string propertiesNameVal = res["properties"]["name"][0]["value"].ToString();
                    Assert.IsTrue(string.Equals("ripple", propertiesNameVal) || string.Equals("lop", propertiesNameVal));
                }
            }
        }

        /// <summary>
        /// Port of the g_VX1X_outE_asXhereX_inV_hasXname_vadasX_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).outE.as('here').inV.has('name', 'vadas').select('here')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdOutEAsHereInVHasNameVadasSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");
                string vertexId2 = this.ConvertToVertexId(command, "vadas");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).OutE().As("here").InV().Has("name", "vadas").Select("here");
                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(1, dynamicResult.Count);
                dynamic edge = dynamicResult[0];
                Assert.AreEqual("knows", edge["label"].ToString());
                Assert.AreEqual(vertexId2, edge["inV"].ToString());
                Assert.AreEqual(vertexId1, edge["outV"].ToString());
                Assert.AreEqual("0.5", edge["properties"]["weight"].ToString());
            }
        }

        /// <summary>
        /// Port of the g_VX4X_out_asXhereX_hasXlang_javaX_selectXhereX_name UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v4Id).out.as('here').has('lang', 'java').select('here').name", "v4Id", v4Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdOutAsHereHasLangJavaSelectHereName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "josh");
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).Out().As("here").Has("lang", "java").Select("here").Values("name");

                List<string> result = traversal.Next();
                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.Contains("ripple"));
                Assert.IsTrue(result.Contains("lop"));
            }
        }

        /// <summary>
        /// Port of the g_VX1X_outEXknowsX_hasXweight_1X_asXhereX_inV_hasXname_joshX_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).outE('knows').has('weight', 1.0d).as('here').inV.has('name', 'josh').select('here')", "v1Id", v1Id
        /// </summary>
        [TestMethod]
        public void HasVertexIdOutEKnowsHasWeight1AsHereInVHasNameJoshSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).OutE("knows").Has("weight", 1.0d).As("here").InV().Has("name", "josh").Select("here");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                AssertCommonB(dynamicResult);
            }
        }

        /// <summary>
        /// Port of the g_VX1X_outEXknowsX_asXhereX_hasXweight_1X_asXfakeX_inV_hasXname_joshX_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V(v1Id).outE('knows').as('here').has('weight', 1.0d).as('fake').inV.has('name', 'josh').select('here')", "v1Id", v1Id)
        /// </summary>
        [TestMethod]
        public void HasVertexIdOutEKnowsAsHereHasWeight1AsFakeInVHasNameJoshSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                string vertexId1 = this.ConvertToVertexId(command, "marko");

                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasId(vertexId1).OutE("knows").As("here").Has("weight", 1.0d).As("fake").InV().Has("name", "josh").Select("here");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                AssertCommonB(dynamicResult);
            }
        }

        /// <summary>
        /// Port of the g_V_asXhereXout_name_selectXhereX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V().as('here').out.name.select('here')"
        /// </summary>
        [TestMethod]
        public void VerticesAsHereOutValuesNameSelectHere()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().As("here").Out().Values("name").Select("here");

                // NOTE: actual tests for complete vertice, but here we just validate that the names are correct. We can do so since the names are unique.
                List<string> result = traversal.Values("name").Next();
                List<string> expectedResult = new List<string> { "marko", "marko", "marko", "josh", "josh", "peter" };
                CheckUnOrderedResults(expectedResult, result);
            }
        }

        /// <summary>
        /// Port of the g_V_outXcreatedX_unionXasXprojectX_inXcreatedX_hasXname_markoX_selectXprojectX__asXprojectX_inXcreatedX_inXknowsX_hasXname_markoX_selectXprojectXX_groupCount_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.out('created')
        ///                         .union(__.as('project').in('created').has('name', 'marko').select('project'),
        ///                         __.as('project').in('created').in('knows').has('name', 'marko').select('project')).groupCount().by('name')"
        /// </summary>
        [TestMethod]
        public void VerticesOutCreatedUnionAsProjectInCreatedHasNameMarkoSelectProjectAsProjectInCreatedInKnowsHasNameMarkoSelectProjectGroupCountByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Out("created")
                    .Union(GraphTraversal2.__().As("project").In("created").Has("name", "marko").Select("project"),
                           GraphTraversal2.__().As("project").In("created").In("knows").Has("name", "marko").Select("project")).GroupCount().By("name");

                dynamic results = JsonConvert.DeserializeObject<dynamic>(traversal.FirstOrDefault());

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(6, int.Parse(results[0]["lop"].ToString()));
                Assert.AreEqual(1, int.Parse(results[0]["ripple"].ToString()));
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_hasXname_markoX_asXbX_asXcX_selectXa_b_cX_by_byXnameX_byXageX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.as('a').has('name', 'marko').as('b').as('c').select('a','b','c').by().by('name').by('age')"
        /// </summary>
        [TestMethod]
        public void VerticesAsAHasNameMarkoAsBAsCSelectABCByByNameByAge()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Has("name", "marko").As("b").As("c").Select("a", "b", "c").By().By("name").By("age");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(1, dynamicResult.Count);
                foreach (dynamic res in dynamicResult)
                {
                    Assert.AreEqual(this.ConvertToVertexId(command, "marko"), res["a"]["id"].ToString());
                    Assert.AreEqual("marko", res["b"].ToString());
                    Assert.AreEqual(29, (int)res["c"]);
                }
            }
        }

        /// <summary>
        /// Port of the g_V_hasLabelXsoftwareX_asXnameX_asXlanguageX_asXcreatorsX_selectXname_language_creatorsX_byXnameX_byXlangX_byXinXcreatedX_name_fold_orderXlocalXX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.hasLabel('software').as('name').as('language').as('creators').select('name','language','creators').by('name').by('lang').
        ///                         by(__.in('created').values('name').fold().order(local))"
        /// </summary>
        [TestMethod]
        public void HasLabelSoftwareAsNameAsLanguageAsCreatorsSelectNameLanguageCreatorsByNameByLangByInCreatedValuesNameFoldOrderLocal()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().HasLabel("software").As("name").As("language").As("creators").Select("name", "language", "creators").By("name").By("lang").
                    By(GraphTraversal2.__().In("created").Values("name").Fold().Order(GremlinKeyword.Scope.Local));

                List<string> results = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(results.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic result in dynamicResult)
                {
                    if ((string)result["name"] == "lop")
                    {
                        Assert.AreEqual("java", (string)result["language"]);
                        CheckOrderedResults(new [] {"josh", "marko", "peter"}, ConvertToList(result["creators"]));
                    }
                    else if ((string)result["name"] == "ripple")
                    {
                        Assert.AreEqual("java", (string)result["language"]);
                        CheckOrderedResults(new[] { "josh"}, ConvertToList(result["creators"]));
                    }
                    else
                    {
                        Assert.Fail("Unknown result");
                    }
                }
            }
        }

        /// <summary>
        /// Port of the g_V_selectXaX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V." + (null == pop ? "select('a')" : "select(${pop}, 'a')"
        /// </summary>
        [TestMethod]
        public void VerticesSelectA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GremlinKeyword.Pop?[] pops = { null, GremlinKeyword.Pop.All, GremlinKeyword.Pop.First, GremlinKeyword.Pop.Last };
                foreach (GremlinKeyword.Pop? pop in pops)
                {
                    GraphTraversal2 root = command.g().V();
                    GraphTraversal2 traversal = (!pop.HasValue) ? root.Select("a") : root.Select(pop.Value, "a");

                    List<string> result = traversal.Next();
                    Assert.AreEqual(0, result.Count);
                }
            }
        }

        /// <summary>
        /// Port of the g_V_untilXout_outX_repeatXin_asXaXX_selectXaX_byXtailXlocalX_nameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.until(__.out.out).repeat(__.in.as('a')).select('a').by(tail(local).name)"
        /// </summary>
        [TestMethod]
        public void VerticesUntilOutOutRepeatInAsASelectAByTailLocalName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().Until(GraphTraversal2.__().Out().Out()).Repeat(GraphTraversal2.__().In().As("a")).Select("a").By(GraphTraversal2.__().Tail(GremlinKeyword.Scope.Local).Values("name"));

                List<string> results = traversal.Next();
                Assert.AreEqual(5, results.Count);
                foreach (string result in results)
                {
                    Assert.AreEqual("marko", result);
                }
            }
        }

        /// <summary>
        /// Port of the g_V_untilXout_outX_repeatXin_asXaX_in_asXbXX_selectXa_bX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.until(__.out.out).repeat(__.in.as('a').in.as('b')).select('a','b').by('name')"
        /// </summary>
        [TestMethod]
        public void VerticesUntilOutOutRepeatInAsAInAsBSelectABByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().Until(GraphTraversal2.__().Out().Out()).Repeat(GraphTraversal2.__().In().As("a").In().As("b")).Select("a", "b").By("name");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());
                
                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic temp in dynamicResult)
                {
                    Assert.AreEqual("josh", temp["a"].ToString());
                    Assert.AreEqual("marko", temp["b"].ToString());
                }
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_whereXoutXknowsXX_selectXaX_byXnameX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V().as('a').where(out('knows')).select('a')"
        /// </summary>
        [TestMethod]
        public void VerticesAsAWhereOutKnowsSelectA()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Where(GraphTraversal2.__().Out("knows")).Select("a");

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(1, dynamicResult.Count);
                dynamic vertex = dynamicResult[0];
                Assert.AreEqual("marko", vertex["properties"]["name"][0]["value"].ToString());
            }
        }

        /// <summary>
        /// Port of the g_V_outE_weight_groupCount_selectXkeysX_unfold UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.outE.weight.groupCount.select(keys).unfold"
        /// </summary>
        [TestMethod]
        public void VerticesOutEValuesWeightGroupCountSelectKeys()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().OutE().Values("weight").GroupCount().Select(GremlinKeyword.Column.Keys).Unfold();
                List<string> results = traversal.Next();

                CheckUnOrderedResults(new[] { "0.5", "1", "0.4", "0.2" }, results);
            }
        }

        /// <summary>
        /// Port of the g_V_outE_weight_groupCount_selectXvaluesX_unfold UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.outE.weight.groupCount.select(values).unfold"
        /// </summary>
        [TestMethod]
        public void VerticesOutEValuesWeightGroupCountSelectValues()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                GraphTraversal2 traversal = command.g().V().OutE().Values("weight").GroupCount().Select(GremlinKeyword.Column.Values).Unfold();
                List<string> results = traversal.Next();

                CheckUnOrderedResults(new[] { "1", "1", "2", "2" }, results);
            }
        }

        /// <summary>
        /// Port of the g_V_asXaX_outXknowsX_asXbX_localXselectXa_bX_byXnameXX UT from org/apache/tinkerpop/gremlin/process/traversal/step/map/SelectTest.java.
        /// Equivalent gremlin: "g.V.as('a').out('knows').as('b').local(select('a', 'b').by('name'))"
        /// </summary>
        [TestMethod]
        public void VerticesAsAOutKnowsAsBLocalSelectABByName()
        {
            using (GraphViewCommand command = new GraphViewCommand(graphConnection))
            {
                command.OutputFormat = OutputFormat.GraphSON;
                GraphTraversal2 traversal = command.g().V().As("a").Out("knows").As("b").Local(GraphTraversal2.__().Select("a", "b").By("name"));

                List<string> result = traversal.Next();
                dynamic dynamicResult = JsonConvert.DeserializeObject<dynamic>(result.FirstOrDefault());

                Assert.AreEqual(2, dynamicResult.Count);
                foreach (dynamic temp in dynamicResult)
                {
                    if (temp["a"].ToString() == "marko")
                    {
                        Assert.IsTrue("vadas".Equals(temp["b"].ToString()) || "josh".Equals(temp["b"].ToString()));
                    }
                    else
                    {
                        Assert.Fail("Unknown result");
                    }
                }
            }
        }

        private void AssertCommonA(dynamic traversalResult)
        {
            List<string> expected = new List<string>
            {
                "a,marko;b,lop",
                "a,marko;b,vadas",
                "a,marko;b,josh",
                "a,josh;b,ripple",
                "a,josh;b,lop",
                "a,peter;b,lop"
            };

            CheckUnOrderedResults(expected, traversalResult);
        }

        private void AssertCommonB(dynamic dynamicResult)
        {
            Assert.AreEqual(1, dynamicResult.Count);
            dynamic edge = dynamicResult[0];
            Assert.AreEqual("knows", edge["label"].ToString());
            Assert.AreEqual(1.0D, double.Parse(edge["properties"]["weight"].ToString()));
        }
    }
}
