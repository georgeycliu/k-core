﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphViewUnitTest.Gremlin
{
    /// <summary>
    /// Abstract test class that contains helper methods, and common setup/cleanup.
    /// </summary>
    [TestClass]
    public class AbstractGremlinTest
    {
        protected static GraphViewConnection graphConnection = new GraphViewConnection(
                //ConfigurationManager.AppSettings["DocDBEndPoint"],
                ConfigurationManager.AppSettings["DocDBEndPointLocal"],
                //ConfigurationManager.AppSettings["DocDBKey"],
                ConfigurationManager.AppSettings["DocDBKeyLocal"],
                ConfigurationManager.AppSettings["DocDBDatabaseGremlin"],
                ConfigurationManager.AppSettings["DocDBCollectionModern"],
                useReverseEdges: true
            );

        /// <summary>
        /// Do any necessary setup.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            GraphDataLoader.LoadGraphData(GraphData.MODERN, graphConnection.UseReverseEdges);
        }

        /// <summary>
        /// Do any necessary cleanup.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            GraphDataLoader.ClearGraphData(GraphData.MODERN);
        }

        public string getVertexString(GraphViewCommand GraphViewCommand, string vertexName)
        {
            GraphViewCommand.OutputFormat = OutputFormat.GraphSON;
            return JsonConvert.DeserializeObject<dynamic>(GraphViewCommand.g().V().Has("name", vertexName).Next().FirstOrDefault()).First.ToString();
        }

        public string ConvertToVertexId(GraphViewCommand GraphViewCommand, string vertexName)
        {
            OutputFormat originalFormat = GraphViewCommand.OutputFormat;
            GraphViewCommand.OutputFormat = OutputFormat.Regular;

            string vertexId = GraphViewCommand.g().V().Has("name", vertexName).Id().Next().FirstOrDefault();

            GraphViewCommand.OutputFormat = originalFormat;

            return vertexId;
        }

        public string ConvertToPropertyId(GraphViewCommand GraphViewCommand, string vertexName, string property, string propertyValue)
        {
            OutputFormat originalFormat = GraphViewCommand.OutputFormat;
            GraphViewCommand.OutputFormat = OutputFormat.Regular;

            string propertyId = GraphViewCommand.g().V().Has("name", vertexName).Properties(property).HasValue(propertyValue).Id().Next().FirstOrDefault();

            GraphViewCommand.OutputFormat = originalFormat;

            return propertyId;
        }

        public string ConvertToEdgeId(GraphViewCommand GraphViewCommand, string outVertexName, string edgeLabel, string inVertexName)
        {
            OutputFormat originalFormat = GraphViewCommand.OutputFormat;
            GraphViewCommand.OutputFormat = OutputFormat.Regular;

            string edgeId = GraphViewCommand.g().V().Has("name", outVertexName).OutE(edgeLabel).As("e").InV().Has("name", inVertexName).Select("e").Values("id").Next().FirstOrDefault();

            GraphViewCommand.OutputFormat = originalFormat;

            return edgeId;
        }

        public static void CheckUnOrderedResults<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            CheckUnOrderedResults(expected, actual, EqualityComparer<T>.Default);
        }

        public static void CheckPathResults<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            List<T> expectedList = new List<T>();
            foreach (var item in expected)
            {
                expectedList.Add(item);
            }
            List<T> actualList = new List<T>();
            foreach (var item in actual)
            {
                actualList.Add(item);
            }
            for (var i = 0; i < expectedList.Count(); i++)
            {
                Assert.AreEqual(expectedList[i], actualList[i]);
            }
        }

        public static List<string> ConvertToList(dynamic result)
        {
            return ((JArray) result).Select(p => p.ToString()).ToList();
        }

        public static void CheckOrderedResults<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            List<T> expectedList = new List<T>();
            foreach (var item in expected)
            {
                expectedList.Add(item);
            }
            List<T> actualList = new List<T>();
            foreach (var item in actual)
            {
                actualList.Add(item);
            }
            for (var i = 0; i < expectedList.Count(); i++)
            {
                Assert.AreEqual(expectedList[i], actualList[i]);
            }
        }

        public static void CheckUnOrderedResults<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            Assert.AreEqual(expected.Count(), actual.Count());

            Dictionary<T, int> expectedHashMap = GetHashMap(expected, comparer);
            Dictionary<T, int> actualHashMap = GetHashMap(actual, comparer);

            foreach (T key in expectedHashMap.Keys)
            {
                Assert.AreEqual(expectedHashMap[key], actualHashMap[key]);
            }
        }

        private static Dictionary<T, int> GetHashMap<T>(IEnumerable<T> list, IEqualityComparer<T> comparer)
        {
            Dictionary<T, int> hashMap = new Dictionary<T, int>(comparer);
            foreach (T listVal in list)
            {
                if (!hashMap.ContainsKey(listVal))
                {
                    hashMap[listVal] = 0;
                }
                hashMap[listVal]++;
            }
            return hashMap;
        }

        public int GetVertexCount(GraphViewCommand graph)
        {
            graph.OutputFormat = OutputFormat.Regular;
            int count = JsonConvert.DeserializeObject<int>(graph.g().V().Count().Next().First());
            graph.OutputFormat = OutputFormat.GraphSON;
            return count;
        }

        public int GetEdgeCount(GraphViewCommand graph)
        {
            graph.OutputFormat = OutputFormat.Regular;
            int count = JsonConvert.DeserializeObject<int>(graph.g().E().Count().Next().First());
            graph.OutputFormat = OutputFormat.GraphSON;
            return count;
        }
    }
}
