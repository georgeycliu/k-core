using System;
using GraphView;
using GraphViewUnitTest;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Main
{
    class Program
    {
        [DllImport("winmm")]
        static extern uint timeGetTime();
        [DllImport("winmm")]
        static extern void timeBeginPeriod(int t);
        [DllImport("winmm")]
        static extern void timeEndPeriod(int t);
        class vertex
        {
            public string id;
            public string name;
            public List<string> neighbours;
        }
        static public Dictionary<string, int> names = new Dictionary<string, int>();
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
            var val = graph.g().V().Values("name").Next();
            Dictionary<string, int> degree = new Dictionary<string, int>();
            Dictionary<string, int> core = new Dictionary<string, int>();
            foreach (var x in val)
            {
                int deg = int.Parse(graph.g().V().Has("name",x).Both().Count().Next()[0]);
                degree.Add(x, deg);
            }
            int n = degree.Count();
            while (n>0)
            {
                var minvertex = degree.OrderBy(kvp => kvp.Value).First().Key;
                core.Add(minvertex, degree[minvertex]);
                var neighbour = graph.g().V().Has("name", minvertex).Both().Values("name").Next();
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
            /*foreach (KeyValuePair<string, int> kvp in core)
            {
                Console.WriteLine("name = {0}, core_number = {1}", graph.g().V(kvp.Key).Values("name").Next()[0], kvp.Value);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= core.Count(); v++)
            {
                int c = core[v.ToString()];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }   
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }
        static public HashSet<string> connected_component(GraphViewCommand graph, int x)
        {
            HashSet<string> reached = new HashSet<string>();
            Queue<String> vertexQ1 = new Queue<String>();
            Queue<String> vertexQ2 = new Queue<String>();
            reached.Add(x.ToString());
            vertexQ1.Enqueue(x.ToString());
            while (vertexQ1.Count()!=0)
            {
                var tmp = vertexQ1.Dequeue();
                var neighbours = graph.g().V().Has("name", tmp).Both().Values("name").Next();
                foreach(var vertex in neighbours)
                {
                    if (reached.Contains(vertex)) continue;
                    else
                    {
                        reached.Add(vertex);
                        vertexQ2.Enqueue(vertex);
                    }
                }
                if(vertexQ1.Count()==0)
                {
                    var swap = vertexQ1;
                    vertexQ1 = vertexQ2;
                    vertexQ2 = swap;
                }
            }
            return reached;
        }
        static public int get_core_number(GraphViewCommand graph, int t)
        {
            HashSet<string> scc = Program.connected_component(graph, t);
            Dictionary<string, int> degree = new Dictionary<string, int>();
            Dictionary<string, int> core = new Dictionary<string, int>();
            foreach (var x in scc)
            {
                int deg = int.Parse(graph.g().V().Has("name", x).Both().Count().Next()[0]);
                degree.Add(x, deg);
            }
            int n = degree.Count();
            while (n > 0)
            {
                var minvertex = degree.OrderBy(kvp => kvp.Value).First().Key;
                core.Add(minvertex, degree[minvertex]);
                var neighbour = graph.g().V().Has("name", minvertex).Both().Values("name").Next();
                foreach (var s in neighbour)
                {
                    if (degree.ContainsKey(s) && degree[s] > degree[minvertex])
                    {
                        degree[s] -= 1;
                    }
                }
                degree.Remove(minvertex);
                n--;
            }
            /*foreach (KeyValuePair<string, int> kvp in core)
            {
                Console.WriteLine("name = {0}, core_number = {1}", graph.g().V(kvp.Key).Values("name").Next()[0], kvp.Value);
            }*/
            return core[t.ToString()];
        }
        static public void linearkcore(GraphViewCommand graph)
        {
            int n = int.Parse(graph.g().V().Count().Next()[0]);
            int[] core = new int[n + 1];
            int[] bin = new int[n + 1];
            int[] pos = new int[n + 1];
            int[] vert = new int[n + 1];
            int md = 0;
            int d = 0;
            for(int v=1;v<=n;v++)
            {
                d = int.Parse(graph.g().V().Has("name", v.ToString()).Both().Id().Count().Next()[0]);
                core[v] = d;
                if (d > md) md = d;
            }
            for (d = 0; d <= md; d++) bin[d] = 0;
            for (int v = 1; v <= n; v++) bin[core[v]]++;
            int start = 1;
            for(d=0;d<=md;d++)
            {
                int num = bin[d];
                bin[d] = start;
                start += num;
            }
            for(int v=1;v<=n;v++)
            {
                pos[v] = bin[core[v]];
                vert[pos[v]] = v;
                bin[core[v]]++;
            }
            for (d = md; d >= 1; d--) bin[d] = bin[d - 1];
            bin[0] = 1;
            for(int i=1;i<=n;i++)
            {
                int v = vert[i];
                foreach(var u in graph.g().V().Has("name",v.ToString()).Both().Values("name").Next())
                {
                    int t = int.Parse(u);
                    if(core[t]>core[v])
                    {
                        int du = core[t];
                        int pu = pos[t];
                        int pw = bin[du];
                        int w = vert[pw];
                        if(t!=w)
                        {
                            pos[t] = pw;
                            vert[pu] = w;
                            pos[w]= pu;
                            vert[pw] = t;
                        }
                        bin[du]++;
                        core[t]--;
                    }
                }
            }
            /*for(int v=1;v<=n;v++)
            {
                Console.WriteLine("name = {0}, core_number = {1}", v, core[v]);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = core[v];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }
        static public void va_main(GraphViewCommand graph)
        {
            int iter = 0;
            bool change = false;
            int n = int.Parse(graph.g().V().Count().Next()[0]);
            int[] core = new int[n];
            bool[] scheduled = new bool[n];
            for (int i = 0; i < n; i++)
            {
                scheduled[i] = true;
            }
            while(true)
            {
                int num_scheduled = 0;
                bool[] scheduledNow = new bool[n];
                Array.Copy(scheduled, scheduledNow, n);
                for (int i = 0; i < n; i++)
                {
                    scheduled[i] = false;
                }
                for (int v = 0; v < n; v++)
                {
                    if (scheduledNow[v] == true)
                    {
                        num_scheduled++;
                        if (iter == 0)
                        {
                            core[v] = int.Parse(graph.g().V().Has("name", (v + 1).ToString()).Both().Values("name").Count().Next()[0]);
                            scheduled[v] = true;
                            change = true;
                        }
                        else
                        {
                            int d_v = int.Parse(graph.g().V().Has("name", (v + 1).ToString()).Both().Values("name").Count().Next()[0]);
                            var neighbours = graph.g().V().Has("name", (v + 1).ToString()).Both().Values("name").Next();
                            int[] N_v = new int[d_v];
                            for (int j = 0; j < d_v; j++)
                            {
                                N_v[j] = int.Parse(neighbours[j]) - 1;
                            }
                            int localEstimate = -1;
                            int[] c = new int[core[v] + 1];
                            for (int i = 0; i < d_v; i++)
                            {
                                int u = N_v[i];
                                int j = Math.Min(core[v], core[u]);
                                c[j]++;
                            }

                            int cumul = 0;
                            for (int i = core[v]; i >= 2; i--)
                            {
                                cumul = cumul + c[i];
                                if (cumul >= i)
                                {
                                    localEstimate = i;
                                    break;
                                }
                            }
                            if (localEstimate == -1) localEstimate = d_v;
                            if (localEstimate < core[v])
                            {
                                core[v] = localEstimate;
                                change = true;
                                for (int i = 0; i < d_v; i++)
                                {
                                    int u = N_v[i];
                                    if (core[v] <= core[u])
                                        scheduled[u] = true;
                                }
                            }
                        }
                    }
                }
                iter++;                
                if (change==false)
                {
                    break;
                }
                else
                {
                    change = false;
                }
            }
            /*for(int v = 1; v <= n; v++)
            {
                Console.WriteLine("name = {0}, core_number = {1}", v, core[v]);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = core[v-1];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }
        static public void read_data(GraphViewCommand graph)
        {
            string line;
            int counter = 0;
            System.IO.StreamReader file_id = new System.IO.StreamReader("C:\\Users\\lyc11\\Documents\\yaoclass\\Network Science\\project\\dataset\\CA-GrQc-ID.txt");
            System.IO.StreamReader file = new System.IO.StreamReader("C:\\Users\\lyc11\\Documents\\yaoclass\\Network Science\\project\\dataset\\CA-GrQc-un-noselfloop.txt");
            while ((line = file_id.ReadLine()) != null)
            {
                counter++;
                graph.g().AddV("node").Property("name", counter.ToString()).Next();
                Program.names.Add(line, counter);
                Console.WriteLine(counter);
            }

            file_id.Close();
            counter = 0;
            while ((line = file.ReadLine()) != null)
            {
                string[] splitString = line.Split();
                graph.g().V().Has("name", Program.names[splitString[0]].ToString()).AddE("edge").To(graph.g().V().Has("name", Program.names[splitString[1]].ToString())).Next();
                counter++;
                Console.WriteLine(counter);
            }

            file.Close();
        }
        static public void assignpartitionkey(GraphViewCommand graph, Dictionary<string,int>partition)
        {
            foreach(var x in partition.Keys)
            {
                graph.g().V().Has("name", x).Property("partitionkey", partition[x]).Next();
            }
        }
        static public Dictionary<string,int> graph_partition(GraphViewCommand graph, int maxsize)
        {
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<string, vertex> vertices = new Dictionary<string, vertex>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            var data = JsonConvert.DeserializeObject<dynamic>(graph.g().V().Next()[0]);
            foreach (var node in data)
            {
                string id = (string)node["id"];
                if (!vertices.ContainsKey(id))
                {
                    vertex newnode = new vertex();
                    newnode.id = (string)node["id"];
                    newnode.name = (string)node["properties"]["name"][0]["value"];
                    newnode.neighbours = new List<string>();
                    if (node["outE"] != null)
                    {
                        var edges = node["outE"]["edge"];
                        foreach (var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["inV"]);
                        }
                    }
                    if (node["inE"] != null)
                    {
                        var edges = node["inE"]["edge"];
                        foreach (var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["outV"]);
                        }
                    }
                    vertices.Add(id, newnode);
                    names.Add(int.Parse(newnode.name), newnode.id);
                }
            }
            Dictionary<string, int> partition = new Dictionary<string, int>();
            int n = vertices.Count();
            graph.OutputFormat = OutputFormat.Regular;
            var allnames = graph.g().V().Values("name").Next();
            foreach(var x in allnames)
            {
                partition.Add(x, 0);
            }
            var rnd = new Random();
            var permutation = allnames.OrderBy(item => rnd.Next());
            foreach(var x in permutation)
            {
                Dictionary<int, HashSet<string>> temp = new Dictionary<int, HashSet<string>>();
                foreach(var y in allnames)
                {
                    if(partition[y] > 0)
                    {
                        if(temp.ContainsKey(partition[y]))
                        {
                            temp[partition[y]].Add(y);
                        }
                        else
                        {
                            HashSet<string> t = new HashSet<string>();
                            t.Add(y);
                            temp.Add(partition[y],t);
                        }
                    }
                }
                var neighbours = vertices[names[int.Parse(x)]].neighbours;
                Dictionary<int, int> temp2 = new Dictionary<int, int>();
                foreach(var p in temp.Keys)
                {
                    foreach(var ttmp in neighbours)
                    {
                        var v = vertices[ttmp].name;
                        if(temp[p].Contains(v) && temp[p].Count()<maxsize)
                        {
                            if (temp2.ContainsKey(p)) temp2[p]++;
                            else temp2.Add(p, 1);
                        }
                    }
                }
                int partitionkey;
                if(temp2.Count()==0)
                {
                    partitionkey = partition.Values.Max()+1;
                }
                else
                {
                    partitionkey = temp2.OrderBy(kvp => (-1) * kvp.Value).First().Key;
                }
                partition[x] = partitionkey;
            }
            return partition;
        }
        static public void parse_data(GraphViewCommand graph)
        {
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<string, vertex> vertices = new Dictionary<string, vertex>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            var data =  JsonConvert.DeserializeObject<dynamic>(graph.g().V().Next()[0]);
            foreach(var node in data)
            {
                string id = (string)node["id"];
                if (!vertices.ContainsKey(id))
                {
                    vertex newnode = new vertex();
                    newnode.id = (string)node["id"];
                    newnode.name = (string)node["properties"]["name"][0]["value"];
                    newnode.neighbours = new List<string>();
                    if(node["outE"]!=null)
                    {
                        var edges = node["outE"]["edge"];
                        foreach(var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["inV"]);
                        }
                    }
                    if (node["inE"] != null)
                    {
                        var edges = node["inE"]["edge"];
                        foreach (var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["outV"]);
                        }
                    }
                    vertices.Add(id, newnode);
                    names.Add(int.Parse(newnode.name), newnode.id);
                }
            }
        }
        static public void linearkcore_local(GraphViewCommand graph)
        {
            int n = int.Parse(graph.g().V().Count().Next()[0]);
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<string, vertex> vertices = new Dictionary<string, vertex>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            var data = JsonConvert.DeserializeObject<dynamic>(graph.g().V().Next()[0]);
            foreach (var node in data)
            {
                string id = (string)node["id"];
                if (!vertices.ContainsKey(id))
                {
                    vertex newnode = new vertex();
                    newnode.id = (string)node["id"];
                    newnode.name = (string)node["properties"]["name"][0]["value"];
                    newnode.neighbours = new List<string>();
                    names.Add(int.Parse(newnode.name), newnode.id);
                    if (node["outE"] != null)
                    {
                        var edges = node["outE"]["edge"];
                        foreach (var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["inV"]);
                        }
                    }
                    if (node["inE"] != null)
                    {
                        var edges = node["inE"]["edge"];
                        foreach (var tmp in edges)
                        {
                            newnode.neighbours.Add((string)tmp["outV"]);
                        }
                    }
                    vertices.Add(id, newnode);
                }
            }
            int[] core = new int[n + 1];
            int[] bin = new int[n + 1];
            int[] pos = new int[n + 1];
            int[] vert = new int[n + 1];
            int md = 0;
            int d = 0;
            for (int v = 1; v <= n; v++)
            {
                var id = names[v];
                d = vertices[id].neighbours.Count();
                core[v] = d;
                if (d > md) md = d;
            }
            for (d = 0; d <= md; d++) bin[d] = 0;
            for (int v = 1; v <= n; v++) bin[core[v]]++;
            int start = 1;
            for (d = 0; d <= md; d++)
            {
                int num = bin[d];
                bin[d] = start;
                start += num;
            }
            for (int v = 1; v <= n; v++)
            {
                pos[v] = bin[core[v]];
                vert[pos[v]] = v;
                bin[core[v]]++;
            }
            for (d = md; d >= 1; d--) bin[d] = bin[d - 1];
            bin[0] = 1;
            for (int i = 1; i <= n; i++)
            {
                int v = vert[i];
                var id = names[v];
                foreach (var u in vertices[id].neighbours)
                {
                    int t = int.Parse(vertices[u].name);
                    if (core[t] > core[v])
                    {
                        int du = core[t];
                        int pu = pos[t];
                        int pw = bin[du];
                        int w = vert[pw];
                        if (t != w)
                        {
                            pos[t] = pw;
                            vert[pu] = w;
                            pos[w] = pu;
                            vert[pw] = t;
                        }
                        bin[du]++;
                        core[t]--;
                    }
                }
            }
            /*for(int v=1;v<=n;v++)
            {
                Console.WriteLine("name = {0}, core_number = {1}", v, core[v]);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = core[v];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }
        static public void linearkcore_hop(GraphViewCommand graph, int[] degree)
        {
            int n = int.Parse(graph.g().V().Count().Next()[0]);
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<string, vertex> vertices = new Dictionary<string, vertex>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            
            int[] core = new int[n + 1];
            int[] bin = new int[n + 1];
            int[] pos = new int[n + 1];
            int[] vert = new int[n + 1];
            int md = 0;
            int d = 0;
            for (int v = 1; v <= n; v++)
            {
                d = degree[v];
                core[v] = d;
                if (d > md) md = d;
            }
            for (d = 0; d <= md; d++) bin[d] = 0;
            for (int v = 1; v <= n; v++) bin[core[v]]++;
            int start = 1;
            for (d = 0; d <= md; d++)
            {
                int num = bin[d];
                bin[d] = start;
                start += num;
            }
            for (int v = 1; v <= n; v++)
            {
                pos[v] = bin[core[v]];
                vert[pos[v]] = v;
                bin[core[v]]++;
            }
            for (d = md; d >= 1; d--) bin[d] = bin[d - 1];
            bin[0] = 1;
            for (int i = 1; i <= n; i++)
            {
                int v = vert[i];
                if(!names.ContainsKey(v))
                {
                    var t = v.ToString();
                    var tempdata = JsonConvert.DeserializeObject<dynamic>(graph.g().V().Has("name", t).Union(graph.g().V().Has("name", t).Both(), graph.g().V().Has("name", t).Both().Both(),  graph.g().V().Has("name", t)).Dedup().Next()[0]);
                    foreach (var node in tempdata)
                    {
                        string tempid = (string)node["id"];
                        if (!vertices.ContainsKey(tempid))
                        {
                            vertex newnode = new vertex();
                            newnode.id = (string)node["id"];
                            newnode.name = (string)node["properties"]["name"][0]["value"];
                            newnode.neighbours = new List<string>();
                            names.Add(int.Parse(newnode.name), newnode.id);
                            if (node["outE"] != null)
                            {
                                var edges = node["outE"]["edge"];
                                foreach (var tmp in edges)
                                {
                                    newnode.neighbours.Add((string)tmp["inV"]);
                                }
                            }
                            if (node["inE"] != null)
                            {
                                var edges = node["inE"]["edge"];
                                foreach (var tmp in edges)
                                {
                                    newnode.neighbours.Add((string)tmp["outV"]);
                                }
                            }
                            vertices.Add(tempid, newnode);
                        }
                    }
                }
                var id = names[v];
                foreach (var u in vertices[id].neighbours)
                {
                    if (!vertices.ContainsKey(u))
                    {
                        var tempdata = JsonConvert.DeserializeObject<dynamic>(graph.g().V(u).Union(graph.g().V(u).Both(), graph.g().V(u).Both().Both(),  graph.g().V(u)).Dedup().Next()[0]);
                        foreach (var node in tempdata)
                        {
                            string tempid = (string)node["id"];
                            if (!vertices.ContainsKey(tempid))
                            {
                                vertex newnode = new vertex();
                                newnode.id = (string)node["id"];
                                newnode.name = (string)node["properties"]["name"][0]["value"];
                                newnode.neighbours = new List<string>();
                                names.Add(int.Parse(newnode.name), newnode.id);
                                if (node["outE"] != null)
                                {
                                    var edges = node["outE"]["edge"];
                                    foreach (var tmp in edges)
                                    {
                                        newnode.neighbours.Add((string)tmp["inV"]);
                                    }
                                }
                                if (node["inE"] != null)
                                {
                                    var edges = node["inE"]["edge"];
                                    foreach (var tmp in edges)
                                    {
                                        newnode.neighbours.Add((string)tmp["outV"]);
                                    }
                                }
                                vertices.Add(tempid, newnode);
                            }
                        }
                    }
                    int t = int.Parse(vertices[u].name);
                    if (core[t] > core[v])
                    {
                        int du = core[t];
                        int pu = pos[t];
                        int pw = bin[du];
                        int w = vert[pw];
                        if (t != w)
                        {
                            pos[t] = pw;
                            vert[pu] = w;
                            pos[w] = pu;
                            vert[pw] = t;
                        }
                        bin[du]++;
                        core[t]--;
                    }
                }
            }
            /*for(int v=1;v<=n;v++)
            {
                Console.WriteLine("name = {0}, core_number = {1}", v, core[v]);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = core[v];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }
        static public void linearkcore_key(GraphViewCommand graph, int[] degree, Dictionary<string,int>partitionkey)
        {
            int n = int.Parse(graph.g().V().Count().Next()[0]);
            graph.OutputFormat = OutputFormat.GraphSON;
            Dictionary<string, vertex> vertices = new Dictionary<string, vertex>();
            Dictionary<int, string> names = new Dictionary<int, string>();

            int[] core = new int[n + 1];
            int[] bin = new int[n + 1];
            int[] pos = new int[n + 1];
            int[] vert = new int[n + 1];
            int md = 0;
            int d = 0;
            for (int v = 1; v <= n; v++)
            {
                d = degree[v];
                core[v] = d;
                if (d > md) md = d;
            }
            for (d = 0; d <= md; d++) bin[d] = 0;
            for (int v = 1; v <= n; v++) bin[core[v]]++;
            int start = 1;
            for (d = 0; d <= md; d++)
            {
                int num = bin[d];
                bin[d] = start;
                start += num;
            }
            for (int v = 1; v <= n; v++)
            {
                pos[v] = bin[core[v]];
                vert[pos[v]] = v;
                bin[core[v]]++;
            }
            for (d = md; d >= 1; d--) bin[d] = bin[d - 1];
            bin[0] = 1;
            for (int i = 1; i <= n; i++)
            {
                int v = vert[i];
                if (!names.ContainsKey(v))
                {
                    var t = v.ToString();
                    Console.WriteLine("here1");
                    var tempdata = JsonConvert.DeserializeObject<dynamic>(graph.g().V().Has("partitionkey", partitionkey[t]).Next()[0]);
                    foreach (var node in tempdata)
                    {
                        string tempid = (string)node["id"];
                        if (!vertices.ContainsKey(tempid))
                        {
                            vertex newnode = new vertex();
                            newnode.id = (string)node["id"];
                            newnode.name = (string)node["properties"]["name"][0]["value"];
                            newnode.neighbours = new List<string>();
                            names.Add(int.Parse(newnode.name), newnode.id);
                            if (node["outE"] != null)
                            {
                                var edges = node["outE"]["edge"];
                                foreach (var tmp in edges)
                                {
                                    newnode.neighbours.Add((string)tmp["inV"]);
                                }
                            }
                            if (node["inE"] != null)
                            {
                                var edges = node["inE"]["edge"];
                                foreach (var tmp in edges)
                                {
                                    newnode.neighbours.Add((string)tmp["outV"]);
                                }
                            }
                            vertices.Add(tempid, newnode);
                        }
                    }
                }
                var id = names[v];
                foreach (var u in vertices[id].neighbours)
                {
                    if (!vertices.ContainsKey(u))
                    {
                        Console.WriteLine("here2");
                        graph.OutputFormat = OutputFormat.Regular;
                        var key = int.Parse(graph.g().V(u).Values("partitionkey").Next()[0]);
                        graph.OutputFormat = OutputFormat.GraphSON;
                        var tempdata = JsonConvert.DeserializeObject<dynamic>(graph.g().V().Has("partitionkey", key).Next()[0]);
                        foreach (var node in tempdata)
                        {
                            string tempid = (string)node["id"];
                            if (!vertices.ContainsKey(tempid))
                            {
                                vertex newnode = new vertex();
                                newnode.id = (string)node["id"];
                                newnode.name = (string)node["properties"]["name"][0]["value"];
                                newnode.neighbours = new List<string>();
                                names.Add(int.Parse(newnode.name), newnode.id);
                                if (node["outE"] != null)
                                {
                                    var edges = node["outE"]["edge"];
                                    foreach (var tmp in edges)
                                    {
                                        newnode.neighbours.Add((string)tmp["inV"]);
                                    }
                                }
                                if (node["inE"] != null)
                                {
                                    var edges = node["inE"]["edge"];
                                    foreach (var tmp in edges)
                                    {
                                        newnode.neighbours.Add((string)tmp["outV"]);
                                    }
                                }
                                vertices.Add(tempid, newnode);
                            }
                        }
                    }
                    int t = int.Parse(vertices[u].name);
                    if (core[t] > core[v])
                    {
                        int du = core[t];
                        int pu = pos[t];
                        int pw = bin[du];
                        int w = vert[pw];
                        if (t != w)
                        {
                            pos[t] = pw;
                            vert[pu] = w;
                            pos[w] = pu;
                            vert[pw] = t;
                        }
                        bin[du]++;
                        core[t]--;
                    }
                }
            }
            /*for(int v=1;v<=n;v++)
            {
                Console.WriteLine("name = {0}, core_number = {1}", v, core[v]);
            }*/
            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = core[v];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("core_number = {0}, number of nodes = {1}", kvp.Key, kvp.Value);
            }
        }

        static void Main(string[] args)
        {
            string DOCDB_URL = "https://localhost:8081";
            string DOCDB_AUTHKEY = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            string DOCDB_DATABASE = "Network Science";
            string DOCDB_COLLECTION = "lyc2";
            GraphViewConnection connection = new GraphViewConnection(DOCDB_URL, DOCDB_AUTHKEY, DOCDB_DATABASE, DOCDB_COLLECTION);
            connection.ResetCollection();
            /*GraphViewConnection connection = new GraphViewConnection("https://graphview.documents.azure.com:443/",

                "MqQnw4xFu7zEiPSD+4lLKRBQEaQHZcKsjlHxXn2b96pE/XlJ8oePGhjnOofj1eLpUdsfYgEhzhejk2rjH/+EKA==",

                "GroupMatch", "Wiki_Temp");*/
            GraphViewCommand graph = new GraphViewCommand(connection);
            //Program.LoadClassicGraphData2(graph);
            Program.read_data(graph);
            //timeBeginPeriod(1);
            //uint start = timeGetTime();

            //Program.linearkcore(graph);
            //Program.naivekcore(graph);
            //Program.va_main(graph); 

            //Console.WriteLine((timeGetTime() - start)/1000.0);  //单位毫秒
            //timeEndPeriod(1);

            //Console.WriteLine(n);
            
            /*
            Dictionary<string, int> partition = Program.graph_partition(graph, 5);
            foreach(KeyValuePair<string,int> kvp in partition.OrderBy(kvp=>int.Parse(kvp.Key)))
            {
                Console.WriteLine("vertex = {0}, partition key = {1}", kvp.Key, kvp.Value);
            }
            Program.assignpartitionkey(graph,partition);
            /*

            //Console.WriteLine(Program.get_core_number(graph,13));
            //Program.linearkcore_local(graph);
            //Program.parse_data(graph);     
            /*
            var t = "7"; 
            var res2 = graph.g().V().Has("name", t).Union(graph.g().V().Has("name", t).Both(), graph.g().V().Has("name", t).Both().Both(), graph.g().V().Has("name", t)).Dedup().Values("name").Next();
            foreach (var temp in res2)
            {
                Console.WriteLine(temp);
            }*/





            graph.OutputFormat = OutputFormat.Regular;
            int n = int.Parse(graph.g().V().Values("name").Count().Next()[0]);
            int[] degree = new int[n + 1];
            for(int i=1;i<=n;i++)
            {
                degree[i] = int.Parse(graph.g().V().Has("name", i.ToString()).Both().Count().Next()[0]);
            }
            /*
            timeBeginPeriod(1);
            uint start = timeGetTime();
            Program.linearkcore_hop(graph, degree);
            Console.WriteLine((timeGetTime() - start)/1000.0);
            timeEndPeriod(1);
            */

            Dictionary<int, int> coredist = new Dictionary<int, int>();
            for (int v = 1; v <= n; v++)
            {
                int c = degree[v];
                if (coredist.ContainsKey(c))
                {
                    coredist[c]++;
                }
                else
                {
                    coredist.Add(c, 1);
                }
            }
            foreach (KeyValuePair<int, int> kvp in coredist)
            {
                Console.WriteLine("{0} {1}", kvp.Key, kvp.Value);
            }
        }
    }
}
