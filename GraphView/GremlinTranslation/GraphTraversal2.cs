using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
// ReSharper disable All

namespace GraphView
{
    public enum OutputFormat
    {
        Regular = 0,
        GraphSON
    }

    public class GraphSONProjector
    {
        internal static string ToGraphSON(List<RawRecord> results, GraphViewConnection connection)
        {
            StringBuilder finalGraphSonResult = new StringBuilder("[");
            HashSet<string> batchIdSet = new HashSet<string>();
            Dictionary<int, VertexField> batchGraphSonDict = new Dictionary<int, VertexField>();

            StringBuilder notBatchedGraphSonResult = new StringBuilder();
            bool firstEntry = true;
            foreach (RawRecord record in results)
            {
                if (firstEntry) {
                    firstEntry = false;
                }
                else {
                    notBatchedGraphSonResult.Append(", ");
                }
                FieldObject field = record[0];

                VertexField vertexField = field as VertexField;
                if (vertexField != null &&
                    (!vertexField.AdjacencyList.HasBeenFetched || !vertexField.RevAdjacencyList.HasBeenFetched))
                {
                    string vertexId = vertexField[GraphViewKeywords.KW_DOC_ID].ToValue;
                    batchIdSet.Add(vertexId);
                    batchGraphSonDict.Add(notBatchedGraphSonResult.Length, vertexField);
                    continue;
                }

                notBatchedGraphSonResult.Append(field.ToGraphSON());
            }

            if (batchIdSet.Any())
            {
                EdgeDocumentHelper.ConstructSpilledAdjListsOrVirtualRevAdjListsOfVertices(connection, batchIdSet);

                int startIndex = 0;
                foreach (KeyValuePair<int, VertexField> kvp in batchGraphSonDict)
                {
                    int insertedPosition = kvp.Key;
                    int length = insertedPosition - startIndex;
                    VertexField vertexField = kvp.Value;

                    finalGraphSonResult.Append(notBatchedGraphSonResult.ToString(startIndex, length));
                    finalGraphSonResult.Append(vertexField.ToGraphSON());
                    startIndex = insertedPosition;
                }

                finalGraphSonResult.Append(notBatchedGraphSonResult.ToString(startIndex,
                    notBatchedGraphSonResult.Length - startIndex));

            }
            else {
                finalGraphSonResult.Append(notBatchedGraphSonResult.ToString());
            }

            finalGraphSonResult.Append("]");
            return finalGraphSonResult.ToString();
        }
    }

    public class GraphTraversal2 : IEnumerable<string>
    {
        internal List<GremlinTranslationOperator> TranslationOpList { get; set; } = new List<GremlinTranslationOperator>();

        private OutputFormat outputFormat;
        private GraphTraversalIterator it;
        private GraphViewConnection connection;
        private string SqlScript;

        public class GraphTraversalIterator : IEnumerator<string>
        {
            object IEnumerator.Current => currentRecord;
            public string Current => currentRecord;

            private GraphViewConnection connection;
            private string currentRecord;
            private GraphViewExecutionOperator currentOperator;
            private OutputFormat outputFormat;
            private bool firstCall;

            internal GraphTraversalIterator(GraphViewExecutionOperator pCurrentOperator, 
                GraphViewConnection connection, OutputFormat outputFormat)
            {
                this.connection = connection;
                this.currentOperator = pCurrentOperator;
                this.outputFormat = outputFormat;
                this.firstCall = true;
            }

            public bool MoveNext()
            {
                if (currentOperator == null) return false;

                if (outputFormat == OutputFormat.GraphSON)
                {
                    List<RawRecord> rawRecordResults = new List<RawRecord>();

                    RawRecord outputRec = null;
                    bool firstEntry = true;
                    while ((outputRec = currentOperator.Next()) != null) {
                        rawRecordResults.Add(outputRec);
                        firstEntry = false;
                    }

                    if (firstEntry && !firstCall) {
                        return false;
                    }
                    else
                    {
                        firstCall = false;
                        currentRecord = GraphSONProjector.ToGraphSON(rawRecordResults, this.connection);
                        return true;
                    }
                }
                else
                {
                    RawRecord outputRec = null;
                    if ((outputRec = currentOperator.Next()) != null)
                    {
                        currentRecord = outputRec[0].ToString();
                        return currentRecord != null;
                    }
                    else return false;
                }
            }

            public void Reset() {}

            public void Dispose() {}
        }

        public IEnumerator<string> GetEnumerator()
        {
            var sqlScript = this.GetEndOp().ToSqlScript();
            SqlScript = sqlScript.ToString();
            it = new GraphTraversalIterator(sqlScript.Batches[0].Compile(null, this.connection), this.connection, outputFormat);
            return it;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public GraphTraversal2() {}

        public GraphTraversal2(GraphViewConnection pConnection)
        {
            this.connection = pConnection;
            this.outputFormat = OutputFormat.Regular;
        }

        public GraphTraversal2(GraphViewConnection connection, OutputFormat outputFormat)
        {
            this.connection = connection;
            this.outputFormat = outputFormat;
        }

        public List<string> Next()
        {
            WSqlScript sqlScript = this.GetEndOp().ToSqlScript();
            SqlScript = sqlScript.ToString();
            
            GraphViewExecutionOperator op = sqlScript.Batches[0].Compile(null, this.connection);
            List<RawRecord> rawRecordResults = new List<RawRecord>();
            RawRecord outputRec = null;

            while ((outputRec = op.Next()) != null) {
                rawRecordResults.Add(outputRec);
            }

            List<string> results = new List<string>();
            switch (outputFormat)
            {
                case OutputFormat.GraphSON:
                    results.Add(GraphSONProjector.ToGraphSON(rawRecordResults, this.connection));
                    break;
                default:
                    foreach (var record in rawRecordResults) {
                        FieldObject field = record[0];
                        results.Add(field.ToString());
                    }
                    break;
            }

            return results;
        }

        internal void InsertOperator(int index, GremlinTranslationOperator newOp)
        {
            if (index > this.TranslationOpList.Count) throw new QueryCompilationException();
            if (index == this.TranslationOpList.Count)
            {
                AddOperator(newOp);
            }
            if (index == 0)
            {
                this.TranslationOpList[index].InputOperator = newOp;
                this.TranslationOpList.Insert(index, newOp);
            }
            else
            {
                newOp.InputOperator = this.TranslationOpList[index-1];
                this.TranslationOpList.Insert(index, newOp);
                this.TranslationOpList[index + 1].InputOperator = newOp;
            }
        }

        internal void AddOperator(GremlinTranslationOperator newOp)
        {
            if (GetEndOp() is GremlinAndInfixOp)
            {
                ((GremlinAndInfixOp)GetEndOp()).SecondTraversal.AddOperator(newOp);
            }
            else if (GetEndOp() is GremlinOrInfixOp)
            {
                ((GremlinOrInfixOp)GetEndOp()).SecondTraversal.AddOperator(newOp);
            }
            else
            {
                newOp.InputOperator = GetEndOp();
                this.TranslationOpList.Add(newOp);
            }
        }

        internal GremlinTranslationOperator GetStartOp()
        {
            return this.TranslationOpList.Count == 0 ? null : this.TranslationOpList.First();
        }

        internal GremlinTranslationOperator GetEndOp()
        {
            return TranslationOpList.Count == 0 ? null: this.TranslationOpList.Last();
        }

        public GraphTraversal2 AddE(string edgeLabel)
        {
            this.AddOperator(new GremlinAddEOp(edgeLabel));
            return this;
        }

        public GraphTraversal2 AddV()
        {
            this.AddOperator(new GremlinAddVOp());
            return this;
        }

        public GraphTraversal2 AddV(params object[] propertyKeyValues)
        {
            this.AddOperator(new GremlinAddVOp(propertyKeyValues));
            return this;
        }

        public GraphTraversal2 AddV(string vertexLabel)
        {
            this.AddOperator(new GremlinAddVOp(vertexLabel));
            return this;
        }

        public GraphTraversal2 Aggregate(string sideEffectKey)
        {
            this.AddOperator(new GremlinAggregateOp(sideEffectKey));
            return this;
        }

        public GraphTraversal2 And(params GraphTraversal2[] andTraversals)
        {
            if (andTraversals.Length == 0)
            {
                //Infix And step
                GraphTraversal2 firstTraversal = GraphTraversal2.__();
                GraphTraversal2 sencondTraversal = GraphTraversal2.__();
                for (var i = 1; i < this.TranslationOpList.Count; i++) //reserve the first op as the input
                {
                    firstTraversal.AddOperator(this.TranslationOpList[i].Copy());
                }
                this.TranslationOpList.RemoveRange(1, this.TranslationOpList.Count - 1);
                this.AddOperator(new GremlinAndInfixOp(firstTraversal, sencondTraversal));
            }
            else
            {
                this.AddOperator(new GremlinAndOp(andTraversals));
            }
            return this;
        }

        public GraphTraversal2 As(params string[] labels) {
            this.AddOperator(new GremlinAsOp(labels));
            return this;    
        }

        public GraphTraversal2 Barrier()
        {
            this.AddOperator(new GremlinBarrierOp());
            return this;
        }

        public GraphTraversal2 Barrier(int maxBarrierSize)
        {
            this.AddOperator(new GremlinBarrierOp(maxBarrierSize));
            return this;
        }

        public GraphTraversal2 Both(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinBothOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 BothE(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinBothEOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 BothV()
        {
            this.AddOperator(new GremlinBothVOp());
            return this;
        }

        public GraphTraversal2 By()
        {
            GetEndOp().ModulateBy();
            return this;
        }

        public GraphTraversal2 By(GremlinKeyword.Order order)
        {
            GetEndOp().ModulateBy(order);
            return this;
        }

        public GraphTraversal2 By(IComparer comparer)
        {
            GetEndOp().ModulateBy(comparer);
            return this;
        }

        public GraphTraversal2 By(GremlinKeyword.Column column)
        {
            GetEndOp().ModulateBy(column);
            return this;
        }

        public GraphTraversal2 By(GremlinKeyword.Column column, GremlinKeyword.Order order)
        {
            GetEndOp().ModulateBy(column, order);
            return this;
        }

        public GraphTraversal2 By(GremlinKeyword.Column column, IComparer comparer)
        {
            GetEndOp().ModulateBy(column, comparer);
            return this;
        }

        public GraphTraversal2 By(string key)
        {
            GetEndOp().ModulateBy(key);
            return this;
        }

        public GraphTraversal2 By(string key, GremlinKeyword.Order order)
        {
            GetEndOp().ModulateBy(key, order);
            return this;
        }

        public GraphTraversal2 By(string key, IComparer order)
        {
            GetEndOp().ModulateBy(key, order);
            return this;
        }

        public GraphTraversal2 By(GraphTraversal2 traversal)
        {
            GetEndOp().ModulateBy(traversal);
            return this;
        }

        public GraphTraversal2 By(GraphTraversal2 traversal, GremlinKeyword.Order order)
        {
            GetEndOp().ModulateBy(traversal, order);
            return this;
        }

        public GraphTraversal2 By(GraphTraversal2 traversal, IComparer order)
        {
            GetEndOp().ModulateBy(traversal, order);
            return this;
        }

        public GraphTraversal2 Cap(params string[] sideEffectKeys)
        {
            this.AddOperator(new GremlinCapOp(sideEffectKeys));
            return this;
        }

        public GraphTraversal2 Choose(Predicate choosePredicate, GraphTraversal2 trueChoice, GraphTraversal2 falseChoice = null)
        {
            GraphTraversal2 traversalPredicate = GraphTraversal2.__().Is(choosePredicate);
            return Choose(traversalPredicate, trueChoice, falseChoice);
        }

        public GraphTraversal2 Choose(GraphTraversal2 traversalPredicate, GraphTraversal2 trueChoice, GraphTraversal2 falseChoice = null)
        {
            if (falseChoice == null) falseChoice = __();
            this.AddOperator(new GremlinChooseOp(traversalPredicate, trueChoice, falseChoice));
            return this;
        }

        public GraphTraversal2 Choose(GraphTraversal2 choiceTraversal)
        {
            this.AddOperator(new GremlinChooseOp(choiceTraversal));
            return this;
        }

        public GraphTraversal2 Coalesce(params GraphTraversal2[] coalesceTraversals)
        {
            this.AddOperator(new GremlinCoalesceOp(coalesceTraversals));
            return this;
        }

        public GraphTraversal2 Coin(double probability)
        {
            this.AddOperator(new GremlinCoinOp(probability));
            return this;
        }

        public GraphTraversal2 Constant()
        {
            this.AddOperator(new GremlinConstantOp(new List<object>()));
            return this;
        }

        public GraphTraversal2 Constant(object value)
        {
            if (GremlinUtil.IsList(value)
                || GremlinUtil.IsArray(value)
                || GremlinUtil.IsNumber(value)
                || value is string
                || value is bool) {
                this.AddOperator(new GremlinConstantOp(value));
            }
            else {
                throw new ArgumentException();
            }
            return this;
        }

        public GraphTraversal2 Count()
        {
            this.AddOperator(new GremlinCountGlobalOp());
            return this;
        }

        public GraphTraversal2 Count(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinCountGlobalOp());
            }
            else {
                this.AddOperator(new GremlinCountLocalOp());
            }
            return this;
        }

        public GraphTraversal2 CyclicPath()
        {
            this.AddOperator(new GremlinCyclicPathOp());
            return this;
        }

        public GraphTraversal2 Dedup(GremlinKeyword.Scope scope, params string[] dedupLabels)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinDedupGlobalOp(dedupLabels));
            }
            else {
                this.AddOperator(new GremlinDedupLocalOp(dedupLabels));
            }
            return this;
        }

        public GraphTraversal2 Dedup(params string[] dedupLabels)
        {
            this.AddOperator(new GremlinDedupGlobalOp(dedupLabels));
            return this;
        }

        public GraphTraversal2 Drop()
        {
            this.AddOperator(new GremlinDropOp());
            return this;
        }

        public GraphTraversal2 E()
        {
            this.AddOperator(new GremlinEOp());
            return this;
        }

        public GraphTraversal2 E(params object[] edgeIdsOrElements)
        {
            this.AddOperator(new GremlinEOp(edgeIdsOrElements));
            return this;
        }

        public GraphTraversal2 E(List<object> edgeIdsOrElements)
        {
            this.AddOperator(new GremlinEOp(edgeIdsOrElements));
            return this;
        }

        public GraphTraversal2 Emit()
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.IsEmit = true;
            }
            else {
                this.AddOperator(new GremlinRepeatOp()
                {
                    EmitContext = true,
                    IsEmit = true
                });
            }
            return this;
        }

        public GraphTraversal2 Emit(Predicate emitPredicate)
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.IsEmit = true;
                lastOp.EmitPredicate = emitPredicate;
            }
            else {
                this.AddOperator(new GremlinRepeatOp()
                {
                    EmitPredicate = emitPredicate,
                    IsEmit = true,
                    EmitContext = true
                });
            }
            return this;
        }

        public GraphTraversal2 Emit(GraphTraversal2 emitTraversal)
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.IsEmit = true;
                lastOp.EmitTraversal = emitTraversal;
            }
            else {
                this.AddOperator(new GremlinRepeatOp()
                {
                    EmitTraversal = emitTraversal,
                    IsEmit = true,
                    EmitContext = true
                });
            }
            return this;
        }

        public GraphTraversal2 FlatMap(GraphTraversal2 flatMapTraversal)
        {
            this.AddOperator(new GremlinFlatMapOp(flatMapTraversal));
            return this;
        }

        public GraphTraversal2 Fold()
        {
            this.AddOperator(new GremlinFoldOp());
            return this;
        }

        public GraphTraversal2 From(string fromLabel)
        {
            GremlinAddEOp addEOp = GetEndOp() as GremlinAddEOp;
            if (addEOp != null) {
                addEOp.FromVertexTraversal = GraphTraversal2.__().Select(fromLabel);
            }
            else {
                throw new SyntaxErrorException($"{GetEndOp()} cannot be cast to GremlinAddEOp");
            }
            return this;
        }

        public GraphTraversal2 From(GraphTraversal2 fromVertexTraversal)
        {
            GremlinAddEOp addEOp = GetEndOp() as GremlinAddEOp;
            if (addEOp != null) {
                addEOp.FromVertexTraversal = fromVertexTraversal;
            }
            else {
                throw new SyntaxErrorException($"{GetEndOp()} cannot be cast to GremlinAddEOp");
            }
            return this;
        }

        public GraphTraversal2 Group()
        {
            this.AddOperator(new GremlinGroupOp());
            return this;
        }

        public GraphTraversal2 Group(string sideEffectKey)
        {
            this.AddOperator(new GremlinGroupOp(sideEffectKey));
            return this;
        }

        public GraphTraversal2 GroupCount()
        {
            this.AddOperator(new GremlinGroupOp()
            {
                ProjectBy = __().Count(),
                IsProjectingACollection = false
            });
            return this;
        }

        public GraphTraversal2 GroupCount(string sideEffectKey)
        {
            this.AddOperator(new GremlinGroupOp(sideEffectKey)
            {
                ProjectBy = __().Count(),
                IsProjectingACollection =  false
            });
            return this;
        }

        public GraphTraversal2 Has(string propertyKey)
        {
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasProperty, propertyKey));
            return this;
        }

        public GraphTraversal2 HasNot(string propertyKey)
        {
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasNotProperty, propertyKey));
            return this;
        }

        public GraphTraversal2 Has(string propertyKey, object predicateOrValue)
        {
            GremlinUtil.CheckIsValueOrPredicate(predicateOrValue);
            this.AddOperator(new GremlinHasOp(propertyKey, predicateOrValue));
            return this;
        }

        public GraphTraversal2 Has(string propertyKey, GraphTraversal2 propertyTraversal)
        {
            this.AddOperator(new GremlinHasOp(propertyKey, propertyTraversal));
            return this;
        }

        public GraphTraversal2 Has(string label, string propertyKey, object predicateOrValue)
        {
            GremlinUtil.CheckIsValueOrPredicate(predicateOrValue);
            this.AddOperator(new GremlinHasOp(label, propertyKey, predicateOrValue));
            return this;
        }

        public GraphTraversal2 HasId(params object[] valuesOrPredicates)
        {
            GremlinUtil.CheckIsValueOrPredicate(valuesOrPredicates);
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasId, valuesOrPredicates));
            return this;
        }

        public GraphTraversal2 HasLabel(params object[] valuesOrPredicates)
        {
            GremlinUtil.CheckIsValueOrPredicate(valuesOrPredicates);
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasLabel, valuesOrPredicates));
            return this;
        }

        public GraphTraversal2 HasKey(params string[] valuesOrPredicates)
        {
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasKey, valuesOrPredicates));
            return this;
        }

        public GraphTraversal2 HasValue(params object[] valuesOrPredicates)
        {
            GremlinUtil.CheckIsValueOrPredicate(valuesOrPredicates);
            this.AddOperator(new GremlinHasOp(GremlinHasType.HasValue, valuesOrPredicates));
            return this;
        }

        public GraphTraversal2 Id()
        {
            this.AddOperator(new GremlinIdOp());
            return this;
        }

        public GraphTraversal2 Identity()
        {
            //Do nothing
            return this;
        }

        public GraphTraversal2 In(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinInOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 InE(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinInEOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 Inject()
        {
            //Do nothing
            return this;
        }

        public GraphTraversal2 Inject(params object[] injections)
        {
            foreach (var injection in injections)
            {
                if (GremlinUtil.IsInjectable(injection)) {
                    this.AddOperator(new GremlinInjectOp(injection));
                }
            }
            return this;
        }

        public GraphTraversal2 InV()
        {
            this.AddOperator(new GremlinInVOp());
            return this;
        }

        public GraphTraversal2 Is(object value)
        {
            this.AddOperator(new GremlinIsOp(value));
            return this;
        }

        public GraphTraversal2 Is(Predicate predicate)
        {
            this.AddOperator(new GremlinIsOp(predicate));
            return this;
        }

        public GraphTraversal2 Key()
        {
            this.AddOperator(new GremlinKeyOp());
            return this;
        }

        public GraphTraversal2 Label()
        {
            this.AddOperator(new GremlinLabelOp());
            return this;
        }

        public GraphTraversal2 Limit(int limit)
        {
            this.AddOperator(new GremlinRangeGlobalOp(0, limit));
            return this;
        }

        public GraphTraversal2 Limit(GremlinKeyword.Scope scope, int limit)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinRangeGlobalOp(0, limit));
            }
            else {
                this.AddOperator(new GremlinRangeLocalOp(0, limit));
            }
            return this;
        }

        public GraphTraversal2 Local(GraphTraversal2 localTraversal)
        {
            this.AddOperator(new GremlinLocalOp(localTraversal));
            return this;
        }

        public GraphTraversal2 Map(GraphTraversal2 mapTraversal)
        {
            this.AddOperator(new GremlinMapOp(mapTraversal));
            return this;   
        }

        public GraphTraversal2 Match(params GraphTraversal2[] matchTraversals)
        {
            this.AddOperator(new GremlinMatchOp(matchTraversals));
            return this;
        }

        public GraphTraversal2 Max()
        {
            this.AddOperator(new GremlinMaxGlobalOp());
            return this;
        }

        public GraphTraversal2 Max(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinMaxGlobalOp());
            }
            else {
                this.AddOperator(new GremlinMaxLocalOp());
            }
            return this;
        }

        public GraphTraversal2 Mean()
        {
            this.AddOperator(new GremlinMeanGlobalOp());
            return this;
        }

        public GraphTraversal2 Mean(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinMeanGlobalOp());
            }
            else {
                this.AddOperator(new GremlinMeanLocalOp());
            }
            return this;
        }

        public GraphTraversal2 Min()
        {
            this.AddOperator(new GremlinMinGlobalOp());
            return this;
        }

        public GraphTraversal2 Min(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinMinGlobalOp());
            }
            else {
                this.AddOperator(new GremlinMinLocalOp());
            }
            return this;
        }

        public GraphTraversal2 Not(GraphTraversal2 notTraversal)
        {
           this.AddOperator(new GremlinNotOp(notTraversal));
            return this;
        }

        public GraphTraversal2 Option(object pickToken, GraphTraversal2 traversalOption)
        {
            if (!(GremlinUtil.IsNumber(pickToken) || pickToken is string || pickToken is GremlinKeyword.Pick || pickToken is bool))
            {
                throw new ArgumentException();
            }
            var op = GetEndOp() as GremlinChooseOp;
            if (op != null) {
                if (op.Options.ContainsKey(pickToken)) {
                    throw new SyntaxErrorException(
                        $"Choose step can only have one traversal per pick token: {pickToken}");
                }
                op.Options[pickToken] = traversalOption;
                return this;
            }
            else {
                throw new Exception("Option step only can follow by choose step.");
            }
        }

        public GraphTraversal2 Optional(GraphTraversal2 traversalOption)
        {
            this.AddOperator(new GremlinOptionalOp(traversalOption));
            return this;
        }

        public GraphTraversal2 Or(params GraphTraversal2[] orTraversals)
        {
            if (orTraversals.Length == 0)
            {
                //Infix And step
                GraphTraversal2 firstTraversal = GraphTraversal2.__();
                GraphTraversal2 secondTraversal = GraphTraversal2.__();
                for (var i = 1; i < this.TranslationOpList.Count; i++)
                {
                    firstTraversal.AddOperator(this.TranslationOpList[i].Copy());
                }
                this.TranslationOpList.RemoveRange(1, this.TranslationOpList.Count - 1);
                this.AddOperator(new GremlinOrInfixOp(firstTraversal, secondTraversal));
            }
            else
            {
                this.AddOperator(new GremlinOrOp(orTraversals));
            }
            return this;
        }

        public GraphTraversal2 Order()
        {
            this.AddOperator(new GremlinOrderGlobalOp());
            return this;
        }

        public GraphTraversal2 Order(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinOrderGlobalOp());
            }
            else {
                this.AddOperator(new GremlinOrderLocalOp());
            }
            return this;
        }

        public GraphTraversal2 OtherV()
        {
            this.AddOperator(new GremlinOtherVOp());
            return this;
        }

        public GraphTraversal2 Out(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinOutOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 OutE(params string[] edgeLabels)
        {
            this.AddOperator(new GremlinOutEOp(edgeLabels));
            return this;
        }

        public GraphTraversal2 OutV()
        {
            this.AddOperator(new GremlinOutVOp());
            return this;
        }

        public GraphTraversal2 Path()
        {
            this.AddOperator(new GremlinPathOp());
            return this;
        }

        public GraphTraversal2 Project(params string[] projectKeys)
        {
            this.AddOperator(new GremlinProjectOp(projectKeys));
            return this;
        }

        public GraphTraversal2 Properties(params string[] propertyKeys)
        {
            this.AddOperator(new GremlinPropertiesOp(propertyKeys));
            return this;
        }

        public GraphTraversal2 Property(string key, object value, params object[] keyValues)
        {
            if (keyValues.Length % 2 != 0) throw new Exception("The parameter of property should be even");

            GremlinAddEOp addE = GetEndOp() as GremlinAddEOp;
            GremlinAddVOp addV = GetEndOp() as GremlinAddVOp;
            if (addE != null)
            {
                if (keyValues.Length > 0) throw new SyntaxErrorException("Edge can't have meta properties");
                addE.EdgeProperties.Add(new GremlinProperty(GremlinKeyword.PropertyCardinality.Single, key, value, null));
            }
            else if (addV != null && keyValues.Length == 0)
            {
                addV.VertexProperties.Add(new GremlinProperty(GremlinKeyword.PropertyCardinality.List, key, value, null));
            }
            else
            {
                return this.Property(GremlinKeyword.PropertyCardinality.Single, key, value, keyValues);
            }
            return this;
        }

        public GraphTraversal2 Property(GremlinKeyword.PropertyCardinality cardinality, string key, object value,
            params object[] keyValues)
        {
            if (keyValues.Length % 2 != 0) throw new Exception("The parameter of property should be even");

            Dictionary<string, object> metaProperties = new Dictionary<string, object>();
            for (var i = 0; i < keyValues.Length; i += 2)
            {
                metaProperties[keyValues[i] as string] = keyValues[i + 1];
            }
            GremlinProperty property = new GremlinProperty(cardinality, key, value, metaProperties);
            this.AddOperator(new GremlinPropertyOp(property));

            return this;
        }

        public GraphTraversal2 PropertyMap(params string[] propertyKeys)
        {
            this.AddOperator(new GremlinPropertyMapOp(propertyKeys));
            return this;
        }

        public GraphTraversal2 Range(int low, int high)
        {
            this.AddOperator(new GremlinRangeGlobalOp(low, high));
            return this;
        }

        public GraphTraversal2 Range(GremlinKeyword.Scope scope, int low, int high)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinRangeGlobalOp(low, high));
            }
            else {
                this.AddOperator(new GremlinRangeLocalOp(low, high));
            }
            return this;
        }

        public GraphTraversal2 Repeat(GraphTraversal2 repeatTraversal)
        {
            if (GetEndOp() is GremlinRepeatOp)
            {
                //until().repeat()
                //emit().repeat()
                (GetEndOp() as GremlinRepeatOp).RepeatTraversal = repeatTraversal;
            }
            else
            {
                //repeat().until()
                //repeat().emit()
                this.AddOperator(new GremlinRepeatOp(repeatTraversal));
            }
            return this;
        }

        public GraphTraversal2 Sample(int amountToSample)
        {
            this.AddOperator(new GremlinSampleGlobalOp(amountToSample));
            return this;
        }

        public GraphTraversal2 Sample(GremlinKeyword.Scope scope, int amountToSample)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinSampleGlobalOp(amountToSample));
            }
            else {
                this.AddOperator(new GremlinSampleLocalOp(amountToSample));
            }
            return this;
        }

        public GraphTraversal2 Select(GremlinKeyword.Column column)
        {
            this.AddOperator(new GremlinSelectColumnOp(column));
            return this;
        }

        public GraphTraversal2 Select(GremlinKeyword.Pop pop, params string[] selectKeys)
        {
            this.AddOperator(new GremlinSelectOp(pop, selectKeys));
            return this;
        }

        public GraphTraversal2 Select(params string[] selectKeys)
        {
            this.AddOperator(new GremlinSelectOp(GremlinKeyword.Pop.All, selectKeys));
            return this;
        }

        public GraphTraversal2 SideEffect(GraphTraversal2 sideEffectTraversal)
        {
            this.AddOperator(new GremlinSideEffectOp(sideEffectTraversal));
            return this;    
        }

        public GraphTraversal2 SimplePath()
        {
            this.AddOperator(new GremlinSimplePathOp());
            return this;
        }

        public GraphTraversal2 Store(string sideEffectKey)
        {
            this.AddOperator(new GremlinStoreOp(sideEffectKey));
            return this;
        }

        public GraphTraversal2 Sum()
        {
            this.AddOperator(new GremlinSumGlobalOp());
            return this;
        }

        public GraphTraversal2 Sum(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinSumGlobalOp());

            }
            else {
                this.AddOperator(new GremlinSumLocalOp());
            }
            return this;
        }

        public GraphTraversal2 Tail()
        {
            this.AddOperator(new GremlinRangeGlobalOp(0, 1, true));
            return this;
        }

        public GraphTraversal2 Tail(int limit)
        {
            this.AddOperator(new GremlinRangeGlobalOp(0, limit, true));
            return this;
        }

        public GraphTraversal2 Tail(GremlinKeyword.Scope scope)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinRangeGlobalOp(0, 1, true));
            }
            else {
                this.AddOperator(new GremlinRangeLocalOp(0, 1, true));
            }
            return this;
        }

        public GraphTraversal2 Tail(GremlinKeyword.Scope scope, int limit)
        {
            if (scope == GremlinKeyword.Scope.Global) {
                this.AddOperator(new GremlinRangeGlobalOp(0, limit, true));
            }
            else {
                this.AddOperator(new GremlinRangeLocalOp(0, limit, true));
            }
            return this;
        }

        public GraphTraversal2 TimeLimit(long timeLimit)
        {
            throw new NotImplementedException();
        }

        public GraphTraversal2 Times(int maxLoops)
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.RepeatTimes = maxLoops;
            }
            else {
                this.AddOperator(new GremlinRepeatOp() { RepeatTimes = maxLoops });
            }
            return this;
        }

        public GraphTraversal2 To(string toLabel)
        {
            GremlinAddEOp addEOp = GetEndOp() as GremlinAddEOp;
            if (addEOp != null) {
                addEOp.ToVertexTraversal = GraphTraversal2.__().Select(toLabel);
            }
            else {
                throw new SyntaxErrorException($"{GetEndOp()} cannot be cast to GremlinAddEOp");
            }
            return this;
        }

        public GraphTraversal2 To(GraphTraversal2 toVertex)
        {
            GremlinAddEOp addEOp = GetEndOp() as GremlinAddEOp;
            if (addEOp != null) {
                addEOp.ToVertexTraversal = toVertex;
            }
            else {
                throw new SyntaxErrorException($"{GetEndOp()} cannot be cast to GremlinAddEOp");
            }
            return this;
        }

        public GraphTraversal2 Tree()
        {
            this.AddOperator(new GremlinTreeOp());
            return this;
        }

        public GraphTraversal2 Tree(string sideEffectKey)
        {
            this.AddOperator(new GremlinTreeOp(sideEffectKey));
            return this;
        }

        public GraphTraversal2 Unfold()
        {
            this.AddOperator(new GremlinUnfoldOp());
            return this;
        }

        public GraphTraversal2 Union(params GraphTraversal2[] unionTraversals)
        {
            this.AddOperator(new GremlinUnionOp(unionTraversals));
            return this;
        }

        public GraphTraversal2 Until(Predicate untilPredicate)
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.TerminationPredicate = untilPredicate;
            }
            else {
                this.AddOperator(new GremlinRepeatOp()
                {
                    TerminationPredicate = untilPredicate,
                    StartFromContext = true
                });
            }
            return this;
        }

        public GraphTraversal2 Until(GraphTraversal2 untilTraversal)
        {
            GremlinRepeatOp lastOp = GetEndOp() as GremlinRepeatOp;
            if (lastOp != null) {
                lastOp.TerminationTraversal = untilTraversal;
            }
            else {
                this.AddOperator(new GremlinRepeatOp()
                {
                    TerminationTraversal = untilTraversal,
                    StartFromContext = true
                });
            }
            return this;
        }

        public GraphTraversal2 V(params object[] vertexIdsOrElements)
        {
            this.AddOperator(new GremlinVOp(vertexIdsOrElements));
            return this;
        }

        public GraphTraversal2 V(List<object> vertexIdsOrElements)
        {
            this.AddOperator(new GremlinVOp(vertexIdsOrElements));
            return this;
        }

        public GraphTraversal2 Value()
        {
            this.AddOperator(new GremlinValueOp());
            return this;
        }

        public GraphTraversal2 ValueMap(params string[] propertyKeys)
        {
            this.AddOperator(new GremlinValueMapOp(false, propertyKeys));
            return this;
        }

        public GraphTraversal2 ValueMap(bool isIncludeTokens, params string[] propertyKeys)
        {
            this.AddOperator(new GremlinValueMapOp(isIncludeTokens, propertyKeys));
            return this;
        }

        public GraphTraversal2 Values(params string[] propertyKeys)
        {
            this.AddOperator(new GremlinValuesOp(propertyKeys));
            return this;
        }

        public GraphTraversal2 Where(Predicate predicate)
        {
            this.AddOperator(new GremlinWherePredicateOp(predicate));
            return this;
        }

        public GraphTraversal2 Where(string startKey, Predicate predicate)
        {
            this.AddOperator(new GremlinWherePredicateOp(startKey, predicate));
            return this;
        }

        public GraphTraversal2 Where(GraphTraversal2 whereTraversal)
        {
            this.AddOperator(new GremlinWhereTraversalOp(whereTraversal));
            return this;
        }

        public static GraphTraversal2 __()
        {
            GraphTraversal2 traversal = new GraphTraversal2();
            traversal.AddOperator(new GremlinParentContextOp());
            return traversal;
        }

        public IEnumerable<string> EvalTraversal(string sCSCode)
        {
            return EvalGraphTraversal(ConvertGremlinToGraphTraversalCode(sCSCode));    
        }

        public string ConvertGremlinToGraphTraversalCode(string sCSCode)
        {
            sCSCode = sCSCode.Replace("\'", "\"");

            //replace gremlin steps with uppercase
            foreach (var item in GremlinKeyword.GremlinStepToGraphTraversalDict)
            {
                string originStr = "." + item.Key + "(";
                string targetStr = "." + item.Value + "(";
                sCSCode = sCSCode.Replace(originStr, targetStr);
            }
            //replace with GraphTraversal FunctionName
            foreach (var item in GremlinKeyword.GremlinMainStepToGraphTraversalDict)
            {
                sCSCode = sCSCode.Replace(item.Key, item.Value);
            }
            //replace gremlin predicate with GraphTraversal predicate
            foreach (var item in GremlinKeyword.GremlinPredicateToGraphTraversalDict)
            {
                Regex r1 = new Regex("\\((" + item.Key + ")\\(");
                if (r1.IsMatch(sCSCode))
                {
                    var match = r1.Match(sCSCode);
                    sCSCode = sCSCode.Replace(match.Groups[0].Value, match.Groups[0].Value[0] + item.Value + "(");
                }

                Regex r2 = new Regex("[^a-zA-Z],(" + item.Key + ")\\(");
                if (r2.IsMatch(sCSCode))
                {
                    var match = r2.Match(sCSCode);
                    sCSCode = sCSCode.Replace(match.Groups[0].Value, "\"," + item.Value + "(");
                }
            }

            //replace gremlin keyword
            foreach (var item in GremlinKeyword.GremlinKeywordToGraphTraversalDict)
            {
                RegexOptions ops = RegexOptions.Multiline;
                Regex r = new Regex("[^\"](" + item.Key + ")[^\"]", ops);
                if (r.IsMatch(sCSCode))
                {
                    var match = r.Match(sCSCode);
                    sCSCode = sCSCode.Replace(match.Groups[1].Value, item.Value);
                }
            }

            //replace gremlin array with C# array
            Regex arrayRegex = new Regex("[\\[]((\\s*?[\\\"|']\\S+?[\\\"|']\\s*?[,]*?\\s*?)*)[\\]]", RegexOptions.Multiline);
            var matchtest = arrayRegex.Match(sCSCode);
            if (arrayRegex.IsMatch(sCSCode))
            {
                var matchs = arrayRegex.Matches(sCSCode);
                for (var i = 0; i < matchs.Count; i++)
                {
                    List<string> values = new List<string>();
                    for (var j = 0; j < matchs[i].Groups.Count; j++)
                    {
                        values.Add(matchs[i].Groups[j].Value);
                    }
                    sCSCode = sCSCode.Replace(matchs[i].Groups[0].Value, "new List<string>() {"+ matchs[i].Groups[1].Value + "}");
                    values.Clear();
                }
            }
            return sCSCode;
        }

        public IEnumerable<string> EvalGraphTraversal(string sCSCode)
        {
            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("GraphView.dll");
            cp.ReferencedAssemblies.Add("System.dll");
            cp.GenerateInMemory = true;

            StringBuilder sb = new StringBuilder("");
            sb.Append("using GraphView;\n");
            sb.Append("using System;\n");
            sb.Append("using System.Collections.Generic;\n");

            sb.Append("namespace GraphView { \n");
            sb.Append("public class Program { \n");
            sb.Append("public object Main() {\n");
            sb.Append("GraphViewConnection connection = new GraphViewConnection("+ getConnectionInfo() +");");
            sb.Append("GraphViewCommand graph = new GraphViewCommand(connection);\n");
            switch(outputFormat)
            {
                case OutputFormat.GraphSON:
                    sb.Append("graph.OutputFormat = OutputFormat.GraphSON;\r\n");
                    break;
            }
            sb.Append("return " + sCSCode + ";\n");
            sb.Append("}\n");
            sb.Append("}\n");
            sb.Append("}\n");

            CodeDomProvider icc = CodeDomProvider.CreateProvider("CSharp");
            CompilerResults cr = icc.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                throw new Exception("ERROR: " + cr.Errors[0].ErrorText + "Error evaluating cs code");
            }

            System.Reflection.Assembly a = cr.CompiledAssembly;
            object o = a.CreateInstance("GraphView.Program");

            Type t = o.GetType();
            MethodInfo mi = t.GetMethod("Main");

            return (IEnumerable<string>)mi.Invoke(o, null);
        }

        private string addDoubleQuotes(string str)
        {
            return "\"" + str + "\"";
        }

        private string getConnectionInfo()
        {
            List<string> connectionList = new List<string>();
            connectionList.Add(addDoubleQuotes(this.connection.DocDBUrl));
            connectionList.Add(addDoubleQuotes(this.connection.DocDBPrimaryKey));
            connectionList.Add(addDoubleQuotes(this.connection.DocDBDatabaseId));
            connectionList.Add(addDoubleQuotes(this.connection.DocDBCollectionId));
            connectionList.Add(this.connection.UseReverseEdges.ToString().ToLowerInvariant());
            return string.Join(",", connectionList);
        }
    }
}


