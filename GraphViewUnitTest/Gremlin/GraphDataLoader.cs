﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using GraphView;

namespace GraphViewUnitTest.Gremlin
{
    using System;
    using System.Configuration;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods to generate the various sample TinkerPop graphs.
    /// </summary>
    public static class GraphDataLoader
    {
        /// <summary>
        /// Generates and Loads the correct Graph Db, on the local document Db instance.
        /// </summary>
        /// <param name="graphData">The type of graph data to load from among the TinkerPop samples.</param>
        /// <param name="useReverseEdge"></param>
        public static void LoadGraphData(GraphData graphData, bool useReverseEdge)
        {
            switch (graphData)
            {
                case GraphData.CLASSIC:
                    LoadClassicGraphData(useReverseEdge);
                    break;
                case GraphData.MODERN:
                    LoadModernGraphData(useReverseEdge);
                    break;
                case GraphData.CREW:
                    throw new NotImplementedException("Crew requires supporting properties as documents themselves! This implementation currently does not support that functionality!!!");
                case GraphData.GRATEFUL:
                    throw new NotImplementedException("I'm not a fan of The Grateful Dead!");
                default:
                    throw new NotImplementedException("No idea how I ended up here!");
            }
        }

        /// <summary>
        /// Clears the Correct Graph on the local document Db instance, by clearing the appropriate collection.
        /// </summary>
        /// <param name="graphData">The type of graph data to clear from among the TinkerPop samples.</param>
        public static void ClearGraphData(GraphData graphData)
        {
            switch (graphData)
            {
                case GraphData.CLASSIC:
                    ClearGraphData(ConfigurationManager.AppSettings["DocDBCollectionClassic"]);
                    break;
                case GraphData.MODERN:
                    ClearGraphData(ConfigurationManager.AppSettings["DocDBCollectionModern"]);
                    break;
                case GraphData.CREW:
                    throw new NotImplementedException("Crew requires supporting properties as documents themselves! This implementation currently does not support that functionality!!!");
                case GraphData.GRATEFUL:
                    throw new NotImplementedException("I'm not a fan of The Grateful Dead!");
                default:
                    throw new NotImplementedException("No idea how I ended up here!");
            }
        }

        private static void LoadClassicGraphData(bool useReverseEdge)
        {
            GraphViewConnection connection = new GraphViewConnection(
                //ConfigurationManager.AppSettings["DocDBEndPoint"],
                ConfigurationManager.AppSettings["DocDBEndPointLocal"],
                //ConfigurationManager.AppSettings["DocDBKey"],
                ConfigurationManager.AppSettings["DocDBKeyLocal"],
                ConfigurationManager.AppSettings["DocDBDatabaseGremlin"],
                ConfigurationManager.AppSettings["DocDBCollectionClassic"],
                useReverseEdges: useReverseEdge);
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

        private static void LoadModernGraphData(bool useReverseEdge)
        {
            GraphViewConnection connection = new GraphViewConnection(
                //ConfigurationManager.AppSettings["DocDBEndPoint"],
                ConfigurationManager.AppSettings["DocDBEndPointLocal"],
                //ConfigurationManager.AppSettings["DocDBKey"],
                ConfigurationManager.AppSettings["DocDBKeyLocal"],
                ConfigurationManager.AppSettings["DocDBDatabaseGremlin"],
                ConfigurationManager.AppSettings["DocDBCollectionModern"],
                useReverseEdges: useReverseEdge);
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

        private static void ClearGraphData(string CollectionName)
        {
            GraphViewConnection connection = new GraphViewConnection(
                //ConfigurationManager.AppSettings["DocDBEndPoint"],
                ConfigurationManager.AppSettings["DocDBEndPointLocal"],
                //ConfigurationManager.AppSettings["DocDBKey"],
                ConfigurationManager.AppSettings["DocDBKeyLocal"],
                ConfigurationManager.AppSettings["DocDBDatabaseGremlin"],
                CollectionName);
            connection.ResetCollection();
            connection.Dispose();
        }
    }
}
