using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using GraphView;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphViewUnitTest
{
    [TestClass]
    public class PageRankTest
    {
        [TestMethod]
        public void LoadClassicGraphData()
        {
            GraphViewConnection connection = new GraphViewConnection("https://graphview.documents.azure.com:443/",
              "MqQnw4xFu7zEiPSD+4lLKRBQEaQHZcKsjlHxXn2b96pE/XlJ8oePGhjnOofj1eLpUdsfYgEhzhejk2rjH/+EKA==",
              "GroupMatch", "MarvelTest");
            connection.ResetCollection();
            GraphViewCommand graphCommand = new GraphViewCommand(connection);

            graphCommand.g().AddV("person").Property("name", "marko").Property("age", 29).Next();
            graphCommand.g().AddV("person").Property("name", "vadas").Property("age", 27).Next();
            graphCommand.g().AddV("software").Property("name", "lop").Property("lang", "java").Next();
            graphCommand.g().AddV("person").Property("name", "josh").Property("age", 32).Next();
            graphCommand.g().AddV("software").Property("name", "ripple").Property("lang", "java").Next();
            graphCommand.g().AddV("person").Property("name", "peter").Property("age", 35).Next();
            graphCommand.g().V().Has("name", "marko").AddE("knows").Property("weight", 0.5d).To(graphCommand.g().V().Has("name", "vadas")).Next();
            graphCommand.g().V().Has("name", "marko").AddE("knows").Property("weight", 1.0d).To(graphCommand.g().V().Has("name", "josh")).Next();
            graphCommand.g().V().Has("name", "marko").AddE("created").Property("weight", 0.4d).To(graphCommand.g().V().Has("name", "lop")).Next();
            graphCommand.g().V().Has("name", "josh").AddE("created").Property("weight", 1.0d).To(graphCommand.g().V().Has("name", "ripple")).Next();
            graphCommand.g().V().Has("name", "josh").AddE("created").Property("weight", 0.4d).To(graphCommand.g().V().Has("name", "lop")).Next();
            graphCommand.g().V().Has("name", "peter").AddE("created").Property("weight", 0.2d).To(graphCommand.g().V().Has("name", "lop")).Next();

            graphCommand.Dispose();
            connection.Dispose();
        }
        [TestMethod]
        public void runPageRank()
        {
            GraphViewConnection connection = new GraphViewConnection("https://graphview.documents.azure.com:443/",
            "MqQnw4xFu7zEiPSD+4lLKRBQEaQHZcKsjlHxXn2b96pE/XlJ8oePGhjnOofj1eLpUdsfYgEhzhejk2rjH/+EKA==",
            "GroupMatch", "MarvelTest");
            GraphViewCommand graph = new GraphViewCommand(connection);
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<String, VertexMetrics> vertexCacheMetrics = new Dictionary<string, VertexMetrics>();
            HashSet<String> nonConvergenceVertex = new HashSet<string>();
            var vertexs = JsonConvert.DeserializeObject < JArray >(graph.g().V().Next().FirstOrDefault());
            foreach(var v in vertexs)
            {
                var id = v["id"];
                nonConvergenceVertex.Add(id.ToString());
            }

            var bound = 0.01;
            var iterNum = 100;
            while(iterNum > 0)
            {
                iterNum--;
                if(nonConvergenceVertex.Count == 0)
                {
                    break;
                }
                // (1) only process the nonConverge vertex
                var vs = JsonConvert.DeserializeObject<JArray>(graph.g().V(nonConvergenceVertex.ToArray()).Next().FirstOrDefault());
                foreach (var v in vs)
                {
                    var vertexId = v["id"].ToString();
                    var inEdges = v["inE"];

                    // (2) only process the nonConverge inEdge vertex
                    if (inEdges != null)
                    {
                        foreach (var e in inEdges)
                        {
                            if (!vertexCacheMetrics.ContainsKey(vertexId))
                            {
                                vertexCacheMetrics[vertexId] = new VertexMetrics();
                            }
                            var tmpE = e.First.First;
                            var inVId = tmpE["outV"].ToString();
                            if (nonConvergenceVertex.Contains(inVId))
                            {
                                if (!vertexCacheMetrics.ContainsKey(inVId))
                                {
                                    vertexCacheMetrics[inVId] = new VertexMetrics();
                                }
                                var w = vertexCacheMetrics[inVId].convergeVertexWeightSum + vertexCacheMetrics[inVId].nonConvergeVertexWeightSumPrev;
                                var size = inEdges.First.First.Count(); // need to refactor for diff edge type
                                var tmpW = w / size;

                                // find the last iter value
                                if (!vertexCacheMetrics[vertexId].inVWeight.ContainsKey(inVId))
                                {
                                    vertexCacheMetrics[vertexId].inVWeight[inVId] = 0.0;
                                }
                                vertexCacheMetrics[vertexId].inVWeight[inVId] = tmpW;

                                if (Math.Abs(tmpW - vertexCacheMetrics[vertexId].inVWeight[inVId]) < 0.01)
                                {
                                    // (3) inc the converge vertex weight
                                    nonConvergenceVertex.Remove(vertexId);
                                    vertexCacheMetrics[vertexId].convergeVertexWeightSum += tmpW;
                                }
                                else
                                {
                                    vertexCacheMetrics[vertexId].nonConvergeVertexWeightSum += tmpW;
                                }
                            }
                        }
                    }
                    if (vertexCacheMetrics.ContainsKey(vertexId)) { 
                    // clear and swap the non converge vertex weight
                    vertexCacheMetrics[vertexId].nonConvergeVertexWeightSumPrev = vertexCacheMetrics[vertexId].nonConvergeVertexWeightSum;
                    vertexCacheMetrics[vertexId].nonConvergeVertexWeightSum = 0.0;
                    }
                }
            }
            Console.WriteLine("The Program finished");
        }
    }

    public class VertexMetrics
    {
        public Double convergeVertexWeightSum = 1.0;
        public Double nonConvergeVertexWeightSumPrev = 1.0;
        public Double nonConvergeVertexWeightSum = 1.0;
        public Dictionary<String, Double> inVWeight = new Dictionary<string, double>();
    }
}
