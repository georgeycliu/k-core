using System;
using GraphView;
using GraphViewUnitTest;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main
{
    class Program
    {
        static public void LoadClassicGraphData(GraphViewCommand graphCommand)
        {
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
        }
        static public void LoadClassicGraphData2(GraphViewCommand graphCommand)
        {
            graphCommand.g().AddV("node").Property("name", "1").Next();
            graphCommand.g().AddV("node").Property("name", "2").Next();
            graphCommand.g().AddV("node").Property("name", "3").Next();
            graphCommand.g().AddV("node").Property("name", "4").Next();
            graphCommand.g().AddV("node").Property("name", "5").Next();
            graphCommand.g().AddV("node").Property("name", "6").Next();
            graphCommand.g().AddV("node").Property("name", "7").Next();
            graphCommand.g().AddV("node").Property("name", "8").Next();
            graphCommand.g().AddV("node").Property("name", "9").Next();
            graphCommand.g().AddV("node").Property("name", "10").Next();
            graphCommand.g().AddV("node").Property("name", "11").Next();
            graphCommand.g().AddV("node").Property("name", "12").Next();
            graphCommand.g().AddV("node").Property("name", "13").Next();
            graphCommand.g().AddV("node").Property("name", "14").Next();
            graphCommand.g().AddV("node").Property("name", "15").Next();
            graphCommand.g().AddV("node").Property("name", "16").Next();
            graphCommand.g().AddV("node").Property("name", "17").Next();
            graphCommand.g().AddV("node").Property("name", "18").Next();
            graphCommand.g().AddV("node").Property("name", "19").Next();
            graphCommand.g().AddV("node").Property("name", "20").Next();
            graphCommand.g().AddV("node").Property("name", "21").Next();
            graphCommand.g().V().Has("name", "1").AddE("edge").To(graphCommand.g().V().Has("name", "2")).Next();
            graphCommand.g().V().Has("name", "1").AddE("edge").To(graphCommand.g().V().Has("name", "3")).Next();
            graphCommand.g().V().Has("name", "1").AddE("edge").To(graphCommand.g().V().Has("name", "4")).Next();
            graphCommand.g().V().Has("name", "2").AddE("edge").To(graphCommand.g().V().Has("name", "3")).Next();
            graphCommand.g().V().Has("name", "2").AddE("edge").To(graphCommand.g().V().Has("name", "4")).Next();
            graphCommand.g().V().Has("name", "3").AddE("edge").To(graphCommand.g().V().Has("name", "4")).Next();
            graphCommand.g().V().Has("name", "2").AddE("edge").To(graphCommand.g().V().Has("name", "21")).Next();
            graphCommand.g().V().Has("name", "4").AddE("edge").To(graphCommand.g().V().Has("name", "5")).Next();
            graphCommand.g().V().Has("name", "4").AddE("edge").To(graphCommand.g().V().Has("name", "9")).Next();
            graphCommand.g().V().Has("name", "9").AddE("edge").To(graphCommand.g().V().Has("name", "5")).Next();
            graphCommand.g().V().Has("name", "9").AddE("edge").To(graphCommand.g().V().Has("name", "10")).Next();
            graphCommand.g().V().Has("name", "9").AddE("edge").To(graphCommand.g().V().Has("name", "11")).Next();
            graphCommand.g().V().Has("name", "5").AddE("edge").To(graphCommand.g().V().Has("name", "10")).Next();
            graphCommand.g().V().Has("name", "5").AddE("edge").To(graphCommand.g().V().Has("name", "6")).Next();
            graphCommand.g().V().Has("name", "5").AddE("edge").To(graphCommand.g().V().Has("name", "7")).Next();
            graphCommand.g().V().Has("name", "5").AddE("edge").To(graphCommand.g().V().Has("name", "8")).Next();
            graphCommand.g().V().Has("name", "6").AddE("edge").To(graphCommand.g().V().Has("name", "7")).Next();
            graphCommand.g().V().Has("name", "6").AddE("edge").To(graphCommand.g().V().Has("name", "8")).Next();
            graphCommand.g().V().Has("name", "7").AddE("edge").To(graphCommand.g().V().Has("name", "8")).Next();
            graphCommand.g().V().Has("name", "7").AddE("edge").To(graphCommand.g().V().Has("name", "21")).Next();
            graphCommand.g().V().Has("name", "20").AddE("edge").To(graphCommand.g().V().Has("name", "21")).Next();
            graphCommand.g().V().Has("name", "20").AddE("edge").To(graphCommand.g().V().Has("name", "19")).Next();
            graphCommand.g().V().Has("name", "20").AddE("edge").To(graphCommand.g().V().Has("name", "18")).Next();
            graphCommand.g().V().Has("name", "20").AddE("edge").To(graphCommand.g().V().Has("name", "7")).Next();
            graphCommand.g().V().Has("name", "14").AddE("edge").To(graphCommand.g().V().Has("name", "13")).Next();
            graphCommand.g().V().Has("name", "14").AddE("edge").To(graphCommand.g().V().Has("name", "15")).Next();
            graphCommand.g().V().Has("name", "14").AddE("edge").To(graphCommand.g().V().Has("name", "16")).Next();
            graphCommand.g().V().Has("name", "17").AddE("edge").To(graphCommand.g().V().Has("name", "15")).Next();
            graphCommand.g().V().Has("name", "17").AddE("edge").To(graphCommand.g().V().Has("name", "16")).Next();
        }
        static public void naivekcore(GraphViewCommand graph)
        {
            var val = graph.g().V().Id().Next();
            Dictionary<string, int> degree = new Dictionary<string, int>();
            Dictionary<string, int> core = new Dictionary<string, int>();
            foreach (var x in val)
            {
                int deg = int.Parse(graph.g().V(x).Both().Count().Next()[0]);
                degree.Add(x, deg);
            }
            int n = degree.Count();
            while (n>0)
            {
                var minvertex = degree.OrderBy(kvp => kvp.Value).First().Key;
                core.Add(minvertex, degree[minvertex]);
                var neighbour = graph.g().V(minvertex).Both().Id().Next();
                foreach(var s in neighbour)
                {
                    if(degree.ContainsKey(s) && degree[s]>degree[minvertex])
                    {
                        degree[s] -= 1;
                    }
                }
                degree.Remove(minvertex);
                n--;
            }
            foreach (KeyValuePair<string, int> kvp in core)
            {
                Console.WriteLine("name = {0}, core_number = {1}", graph.g().V(kvp.Key).Values("name").Next()[0], kvp.Value);
            }
        }
        static void Main(string[] args)
        {
            string DOCDB_URL = "https://localhost:8081";
            string DOCDB_AUTHKEY = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            string DOCDB_DATABASE = "Network Science";
            string DOCDB_COLLECTION = "lyc";
            GraphViewConnection connection = new GraphViewConnection(DOCDB_URL, DOCDB_AUTHKEY, DOCDB_DATABASE, DOCDB_COLLECTION);
            connection.ResetCollection();
            GraphViewCommand graph = new GraphViewCommand(connection);
            Program.LoadClassicGraphData2(graph);

            Program.naivekcore(graph);

            
        }
    }
}
