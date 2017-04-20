﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using static GraphView.GraphViewKeywords;

namespace GraphView
{
    internal class ConstantSourceOperator : GraphViewExecutionOperator
    {
        private RawRecord _constantSource;
        ContainerEnumerator sourceEnumerator;

        public RawRecord ConstantSource
        {
            get { return _constantSource; }
            set { _constantSource = value; this.Open(); }
        }

        public ContainerEnumerator SourceEnumerator
        {
            get { return sourceEnumerator; }
            set
            {
                sourceEnumerator = value;
                Open();
            }
        }

        public ConstantSourceOperator()
        {
            Open();
        }

        public override RawRecord Next()
        {
            if (sourceEnumerator != null)
            {
                if (sourceEnumerator.MoveNext())
                {
                    return sourceEnumerator.Current;
                }
                else
                {
                    Close();
                    return null;
                }
            }
            else
            {
                if (!State())
                    return null;

                Close();
                return _constantSource;
            }
        }

        public override void ResetState()
        {
            if (sourceEnumerator != null)
            {
                sourceEnumerator.Reset();
                Open();
            }
            else
            {
                Open();
            }
        }
    }

    internal class FetchNodeOperator2 : GraphViewExecutionOperator
    {
        private Queue<RawRecord> outputBuffer;
        private JsonQuery vertexQuery;
        private GraphViewConnection connection;

        private IEnumerator<RawRecord> verticesEnumerator;

        public FetchNodeOperator2(GraphViewConnection connection, JsonQuery vertexQuery)
        {
            Open();
            this.connection = connection;
            this.vertexQuery = vertexQuery;
            verticesEnumerator = connection.CreateDatabasePortal().GetVertices(vertexQuery);
        }

        public override RawRecord Next()
        {
            if (verticesEnumerator.MoveNext())
            {
                return verticesEnumerator.Current;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            verticesEnumerator = connection.CreateDatabasePortal().GetVertices(vertexQuery);
            outputBuffer?.Clear();
            Open();
        }
    }


    /// <summary>
    /// The operator that takes a list of records as source vertexes and 
    /// traverses to their one-hop or multi-hop neighbors. One-hop neighbors
    /// are defined in the adjacency lists of the sources. Multi-hop
    /// vertices are defined by a recursive function that has a sub-query
    /// specifying a single hop from a vertex to another and a boolean fuction 
    /// controlling when the recursion terminates (in other words, # of hops).  
    /// 
    /// This operators emulates the nested-loop join algorithm.
    /// </summary>
    internal class TraversalOperator2 : GraphViewExecutionOperator
    {
        private int outputBufferSize;
        private int batchSize = 5000;
        private Queue<RawRecord> outputBuffer;
        private GraphViewConnection connection;
        private GraphViewExecutionOperator inputOp;
        
        // The index of the adjacency list in the record from which the traversal starts
        private int adjacencyListSinkIndex = -1;

        // The query that describes predicates on the sink vertices and its properties to return.
        // It is null if the sink vertex has no predicates and no properties other than sink vertex ID
        // are to be returned.  
        private JsonQuery sinkVertexQuery;

        // A list of index pairs, each specifying which field in the source record 
        // must match the field in the sink record. 
        // This list is not null when sink vertices have edges pointing back 
        // to the vertices other than the source vertices in the records by the input operator. 
        private List<Tuple<int, int>> matchingIndexes;

        public TraversalOperator2(
            GraphViewExecutionOperator inputOp,
            GraphViewConnection connection,
            int sinkIndex,
            JsonQuery sinkVertexQuery,
            List<Tuple<int, int>> matchingIndexes,
            int outputBufferSize = 10000)
        {
            Open();
            this.inputOp = inputOp;
            this.connection = connection;
            this.adjacencyListSinkIndex = sinkIndex;
            this.sinkVertexQuery = sinkVertexQuery;
            this.matchingIndexes = matchingIndexes;
            this.outputBufferSize = outputBufferSize;
        }

        public override RawRecord Next()
        {
            if (outputBuffer == null)
            {
                outputBuffer = new Queue<RawRecord>(outputBufferSize);
            }

            while (outputBuffer.Count < outputBufferSize && inputOp.State())
            {
                List<Tuple<RawRecord, string>> inputSequence = new List<Tuple<RawRecord, string>>(batchSize);

                // Loads a batch of source records
                for (int i = 0; i < batchSize && inputOp.State(); i++)
                {
                    RawRecord record = inputOp.Next();
                    if (record == null)
                    {
                        break;
                    }

                    inputSequence.Add(new Tuple<RawRecord, string>(record, record[adjacencyListSinkIndex].ToValue));
                }

                // When sinkVertexQuery is null, only sink vertices' IDs are to be returned. 
                // As a result, there is no need to send queries the underlying system to retrieve 
                // the sink vertices.  
                if (sinkVertexQuery == null)
                {
                    foreach (Tuple<RawRecord, string> pair in inputSequence)
                    {
                        RawRecord resultRecord = new RawRecord { fieldValues = new List<FieldObject>() };
                        resultRecord.Append(pair.Item1);
                        resultRecord.Append(new ValuePropertyField(GraphViewKeywords.KW_DOC_ID, pair.Item2,
                            JsonDataType.String, (VertexField) null));
                        outputBuffer.Enqueue(resultRecord);
                    }

                    continue;
                }

                // Groups records returned by sinkVertexQuery by sink vertices' references
                Dictionary<string, List<RawRecord>> sinkVertexCollection = new Dictionary<string, List<RawRecord>>(GraphViewConnection.InClauseLimit);

                HashSet<string> sinkReferenceSet = new HashSet<string>();
                StringBuilder sinkReferenceList = new StringBuilder();
                // Given a list of sink references, sends queries to the underlying system
                // to retrieve the sink vertices. To reduce the number of queries to send,
                // we pack multiple sink references in one query using the IN clause, i.e., 
                // IN (ref1, ref2, ...). Since the total number of references to locate may exceed
                // the limit that is allowed in the IN clause, we may need to send more than one 
                // query to retrieve all sink vertices. 
                int j = 0;
                while (j < inputSequence.Count)
                {
                    sinkReferenceSet.Clear();

                    //TODO: Verify whether DocumentDB still has inClauseLimit
                    while (sinkReferenceSet.Count < GraphViewConnection.InClauseLimit && j < inputSequence.Count)
                    {
                        sinkReferenceSet.Add(inputSequence[j].Item2);
                        j++;
                    }

                    sinkReferenceList.Clear();
                    foreach (string sinkRef in sinkReferenceSet)
                    {
                        if (sinkReferenceList.Length > 0)
                        {
                            sinkReferenceList.Append(", ");
                        }
                        sinkReferenceList.AppendFormat("'{0}'", sinkRef);
                    }

                    string inClause = string.Format("{0}.id IN ({1})", sinkVertexQuery.Alias, sinkReferenceList.ToString());

                    JsonQuery toSendQuery = new JsonQuery(sinkVertexQuery);

                    if (string.IsNullOrEmpty(toSendQuery.WhereSearchCondition))
                    {
                        toSendQuery.WhereSearchCondition = inClause;
                    }
                    else
                    {
                        toSendQuery.WhereSearchCondition = 
                            string.Format("({0}) AND {1}", sinkVertexQuery.WhereSearchCondition, inClause);
                    }

                    using (DbPortal databasePortal = connection.CreateDatabasePortal())
                    {
                        IEnumerator<RawRecord> verticesEnumerator = databasePortal.GetVertices(toSendQuery);

                        while (verticesEnumerator.MoveNext())
                        {
                            RawRecord rec = verticesEnumerator.Current;
                            if (!sinkVertexCollection.ContainsKey(rec[0].ToValue))
                            {
                                sinkVertexCollection.Add(rec[0].ToValue, new List<RawRecord>());
                            }
                            sinkVertexCollection[rec[0].ToValue].Add(rec);
                        }
                    }
                }

                foreach (Tuple<RawRecord, string> pair in inputSequence)
                {
                    if (!sinkVertexCollection.ContainsKey(pair.Item2))
                    {
                        continue;
                    }

                    RawRecord sourceRec = pair.Item1;
                    List<RawRecord> sinkRecList = sinkVertexCollection[pair.Item2];
                    
                    foreach (RawRecord sinkRec in sinkRecList)
                    {
                        if (matchingIndexes != null && matchingIndexes.Count > 0)
                        {
                            int k = 0;
                            for (; k < matchingIndexes.Count; k++)
                            {
                                int sourceMatchIndex = matchingIndexes[k].Item1;
                                int sinkMatchIndex = matchingIndexes[k].Item2;
                                if (!sourceRec[sourceMatchIndex].ToValue.Equals(sinkRec[sinkMatchIndex].ToValue, StringComparison.OrdinalIgnoreCase))
                                //if (sourceRec[sourceMatchIndex] != sinkRec[sinkMatchIndex])
                                {
                                    break;
                                }
                            }

                            // The source-sink record pair is the result only when it passes all matching tests. 
                            if (k < matchingIndexes.Count)
                            {
                                continue;
                            }
                        }

                        RawRecord resultRec = new RawRecord(sourceRec);
                        resultRec.Append(sinkRec);

                        outputBuffer.Enqueue(resultRec);
                    }
                }
            }

            if (outputBuffer.Count == 0)
            {
                if (!inputOp.State())
                    Close();
                return null;
            }
            else if (outputBuffer.Count == 1)
            {
                Close();
                return outputBuffer.Dequeue();
            }
            else
            {
                return outputBuffer.Dequeue();
            }
        }

        public override void ResetState()
        {
            inputOp.ResetState();
            outputBuffer?.Clear();
            Open();
        }
    }

    internal class BothVOperator : GraphViewExecutionOperator
    {
        private int outputBufferSize;
        private int batchSize = 100;
        private int inClauseLimit = 200;
        private Queue<RawRecord> outputBuffer;
        private GraphViewConnection connection;
        private GraphViewExecutionOperator inputOp;


        private List<int> adjacencyListSinkIndexes;

        // The query that describes predicates on the sink vertices and its properties to return.
        // It is null if the sink vertex has no predicates and no properties other than sink vertex ID
        // are to be returned.  
        private JsonQuery sinkVertexQuery;

        public BothVOperator(
            GraphViewExecutionOperator inputOp,
            GraphViewConnection connection,
            List<int> sinkIndexes,
            JsonQuery sinkVertexQuery,
            int outputBufferSize = 1000)
        {
            Open();
            this.inputOp = inputOp;
            this.connection = connection;
            this.adjacencyListSinkIndexes = sinkIndexes;
            this.sinkVertexQuery = sinkVertexQuery;
            this.outputBufferSize = outputBufferSize;
        }

        public override RawRecord Next()
        {
            if (outputBuffer == null)
            {
                outputBuffer = new Queue<RawRecord>(outputBufferSize);
            }

            while (outputBuffer.Count < outputBufferSize && inputOp.State())
            {
                List<Tuple<RawRecord, string>> inputSequence = new List<Tuple<RawRecord, string>>(batchSize);

                // Loads a batch of source records
                for (int i = 0; i < batchSize && inputOp.State(); i++)
                {
                    RawRecord record = inputOp.Next();
                    if (record == null)
                    {
                        break;
                    }

                    foreach (var adjacencyListSinkIndex in adjacencyListSinkIndexes)
                    {
                        inputSequence.Add(new Tuple<RawRecord, string>(record, record[adjacencyListSinkIndex].ToValue));
                    }
                }

                // When sinkVertexQuery is null, only sink vertices' IDs are to be returned. 
                // As a result, there is no need to send queries the underlying system to retrieve 
                // the sink vertices.  
                if (sinkVertexQuery == null)
                {
                    foreach (Tuple<RawRecord, string> pair in inputSequence)
                    {
                        RawRecord resultRecord = new RawRecord { fieldValues = new List<FieldObject>() };
                        resultRecord.Append(pair.Item1);
                        resultRecord.Append(new StringField(pair.Item2));
                        outputBuffer.Enqueue(resultRecord);
                    }

                    continue;
                }

                // Groups records returned by sinkVertexQuery by sink vertices' references
                Dictionary<string, List<RawRecord>> sinkVertexCollection = new Dictionary<string, List<RawRecord>>(inClauseLimit);

                HashSet<string> sinkReferenceSet = new HashSet<string>();
                StringBuilder sinkReferenceList = new StringBuilder();
                // Given a list of sink references, sends queries to the underlying system
                // to retrieve the sink vertices. To reduce the number of queries to send,
                // we pack multiple sink references in one query using the IN clause, i.e., 
                // IN (ref1, ref2, ...). Since the total number of references to locate may exceed
                // the limit that is allowed in the IN clause, we may need to send more than one 
                // query to retrieve all sink vertices. 
                int j = 0;
                while (j < inputSequence.Count)
                {
                    sinkReferenceSet.Clear();

                    //TODO: Verify whether DocumentDB still has inClauseLimit
                    while (sinkReferenceSet.Count < inClauseLimit && j < inputSequence.Count)
                    {
                        sinkReferenceSet.Add(inputSequence[j].Item2);
                        j++;
                    }

                    sinkReferenceList.Clear();
                    foreach (string sinkRef in sinkReferenceSet)
                    {
                        if (sinkReferenceList.Length > 0)
                        {
                            sinkReferenceList.Append(", ");
                        }
                        sinkReferenceList.AppendFormat("'{0}'", sinkRef);
                    }

                    string inClause = string.Format("{0}.id IN ({1})", sinkVertexQuery.Alias, sinkReferenceList.ToString());

                    JsonQuery toSendQuery = new JsonQuery(sinkVertexQuery);

                    if (string.IsNullOrEmpty(toSendQuery.WhereSearchCondition))
                    {
                        toSendQuery.WhereSearchCondition = inClause;
                    }
                    else
                    {
                        toSendQuery.WhereSearchCondition =
                            string.Format("({0}) AND {1}", sinkVertexQuery.WhereSearchCondition, inClause);
                    }

                    using (DbPortal databasePortal = connection.CreateDatabasePortal())
                    {
                        IEnumerator<RawRecord> verticesEnumerator = databasePortal.GetVertices(toSendQuery);

                        while (verticesEnumerator.MoveNext())
                        {
                            RawRecord rec = verticesEnumerator.Current;
                            if (!sinkVertexCollection.ContainsKey(rec[0].ToValue))
                            {
                                sinkVertexCollection.Add(rec[0].ToValue, new List<RawRecord>());
                            }
                            sinkVertexCollection[rec[0].ToValue].Add(rec);
                        }
                    }
                }

                foreach (Tuple<RawRecord, string> pair in inputSequence)
                {
                    if (!sinkVertexCollection.ContainsKey(pair.Item2))
                    {
                        continue;
                    }

                    RawRecord sourceRec = pair.Item1;
                    List<RawRecord> sinkRecList = sinkVertexCollection[pair.Item2];

                    foreach (RawRecord sinkRec in sinkRecList)
                    {
                        RawRecord resultRec = new RawRecord(sourceRec);
                        resultRec.Append(sinkRec);

                        outputBuffer.Enqueue(resultRec);
                    }
                }
            }

            if (outputBuffer.Count == 0)
            {
                if (!inputOp.State())
                    Close();
                return null;
            }
            else if (outputBuffer.Count == 1)
            {
                Close();
                return outputBuffer.Dequeue();
            }
            else
            {
                return outputBuffer.Dequeue();
            }
        }

        public override void ResetState()
        {
            inputOp.ResetState();
            outputBuffer?.Clear();
            Open();
        }
    }

    internal class FilterOperator : GraphViewExecutionOperator
    {
        public GraphViewExecutionOperator Input { get; private set; }
        public BooleanFunction Func { get; private set; }

        public FilterOperator(GraphViewExecutionOperator input, BooleanFunction func)
        {
            Input = input;
            Func = func;
            Open();
        }

        public override RawRecord Next()
        {
            RawRecord rec;
            while (Input.State() && (rec = Input.Next()) != null)
            {
                if (Func.Evaluate(rec))
                {
                    return rec;
                }
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            Input.ResetState();
            Open();
        }
    }

    internal class CartesianProductOperator2 : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator leftInput;
        private ContainerEnumerator rightInputEnumerator;
        private RawRecord leftRecord;

        public CartesianProductOperator2(
            GraphViewExecutionOperator leftInput, 
            GraphViewExecutionOperator rightInput)
        {
            this.leftInput = leftInput;
            ContainerOperator rightInputContainer = new ContainerOperator(rightInput);
            rightInputEnumerator = rightInputContainer.GetEnumerator();
            leftRecord = null;
            Open();
        }

        public override RawRecord Next()
        {
            RawRecord cartesianRecord = null;

            while (cartesianRecord == null && State())
            {
                if (leftRecord == null && leftInput.State())
                {
                    leftRecord = leftInput.Next();
                }

                if (leftRecord == null)
                {
                    Close();
                    break;
                }
                else
                {
                    if (rightInputEnumerator.MoveNext())
                    {
                        RawRecord rightRecord = rightInputEnumerator.Current;
                        cartesianRecord = new RawRecord(leftRecord);
                        cartesianRecord.Append(rightRecord);
                    }
                    else
                    {
                        // For the current left record, the enumerator on the right input has reached the end.
                        // Moves to the next left record and resets the enumerator.
                        rightInputEnumerator.Reset();
                        leftRecord = null;
                    }
                }
            }

            return cartesianRecord;
        }

        public override void ResetState()
        {
            leftInput.ResetState();
            rightInputEnumerator.ResetState();
            Open();
        }
    }

    //internal class AdjacencyListDecoder : TableValuedFunction
    //{
    //    protected List<int> AdjacencyListIndexes;
    //    protected BooleanFunction EdgePredicate;
    //    protected List<string> ProjectedFields;

    //    public AdjacencyListDecoder(GraphViewExecutionOperator input, List<int> adjacencyListIndexes,
    //        BooleanFunction edgePredicate, List<string> projectedFields, int outputBufferSize = 1000)
    //        : base(input, outputBufferSize)
    //    {
    //        this.AdjacencyListIndexes = adjacencyListIndexes;
    //        this.EdgePredicate = edgePredicate;
    //        this.ProjectedFields = projectedFields;
    //    }

    //    internal override IEnumerable<RawRecord> CrossApply(RawRecord record)
    //    {
    //        List<RawRecord> results = new List<RawRecord>();

    //        foreach (var adjIndex in AdjacencyListIndexes)
    //        {
    //            string jsonArray = record[adjIndex].ToString();
    //            // Parse the adj list in JSON array
    //            var adj = JArray.Parse(jsonArray);
    //            foreach (var edge in adj.Children<JObject>())
    //            {
    //                // Construct new record
    //                var result = new RawRecord(ProjectedFields.Count);

    //                // Fill the field of selected edge's properties
    //                for (var i = 0; i < ProjectedFields.Count; i++)
    //                {
    //                    var projectedField = ProjectedFields[i];
    //                    var fieldValue = "*".Equals(projectedField, StringComparison.OrdinalIgnoreCase)
    //                        ? edge
    //                        : edge[projectedField];

    //                    result.fieldValues[i] = fieldValue != null ? new StringField(fieldValue.ToString()) : null;
    //                }

    //                results.Add(result);
    //            }
    //        }

    //        return results;
    //    }

    //    public override RawRecord Next()
    //    {
    //        if (outputBuffer == null)
    //            outputBuffer = new Queue<RawRecord>();

    //        while (outputBuffer.Count < outputBufferSize && inputOperator.State())
    //        {
    //            RawRecord srcRecord = inputOperator.Next();
    //            if (srcRecord == null)
    //                break;

    //            var results = CrossApply(srcRecord);
    //            foreach (var edgeRecord in results)
    //            {
    //                if (edgePredicate != null && !edgePredicate.Evaluate(edgeRecord))
    //                    continue;

    //                var resultRecord = new RawRecord(srcRecord);
    //                resultRecord.Append(edgeRecord);
    //                outputBuffer.Enqueue(resultRecord);
    //            }
    //        }

    //        if (outputBuffer.Count == 0)
    //        {
    //            if (!inputOperator.State())
    //                Close();
    //            return null;
    //        }
    //        else if (outputBuffer.Count == 1)
    //        {
    //            Close();
    //            return outputBuffer.Dequeue();
    //        }
    //        else
    //        {
    //            return outputBuffer.Dequeue();
    //        }
    //    }

    //    public override void ResetState()
    //    {
    //        inputOperator.ResetState();
    //        outputBuffer?.Clear();
    //        Open();
    //    }
    //}

    //internal abstract class TableValuedScalarFunction
    //{
    //    public abstract IEnumerable<string> Apply(RawRecord record);
    //}

    //internal class CrossApplyAdjacencyList : TableValuedScalarFunction
    //{
    //    private int adjacencyListIndex;

    //    public CrossApplyAdjacencyList(int adjacencyListIndex)
    //    {
    //        this.adjacencyListIndex = adjacencyListIndex;
    //    }

    //    public override IEnumerable<string> Apply(RawRecord record)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //internal class CrossApplyPath : TableValuedScalarFunction
    //{
    //    private GraphViewExecutionOperator referenceOp;
    //    private ConstantSourceOperator contextScan;
    //    private ExistsFunction terminateFunction;
    //    private int iterationUpperBound;

    //    public CrossApplyPath(
    //        ConstantSourceOperator contextScan, 
    //        GraphViewExecutionOperator referenceOp,
    //        int iterationUpperBound)
    //    {
    //        this.contextScan = contextScan;
    //        this.referenceOp = referenceOp;
    //        this.iterationUpperBound = iterationUpperBound;
    //    }

    //    public CrossApplyPath(
    //        ConstantSourceOperator contextScan,
    //        GraphViewExecutionOperator referenceOp,
    //        ExistsFunction terminateFunction)
    //    {
    //        this.contextScan = contextScan;
    //        this.referenceOp = referenceOp;
    //        this.terminateFunction = terminateFunction;
    //    }

    //    public override IEnumerable<string> Apply(RawRecord record)
    //    {
    //        contextScan.ConstantSource = record;

    //        if (terminateFunction != null)
    //        {
    //            throw new NotImplementedException();
    //        }
    //        else
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //}

    internal class OrderOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private List<RawRecord> inputBuffer;
        private int returnIndex;

        private List<Tuple<ScalarFunction, IComparer>> orderByElements;

        public OrderOperator(GraphViewExecutionOperator inputOp, List<Tuple<ScalarFunction, IComparer>> orderByElements)
        {
            this.Open();
            this.inputOp = inputOp;
            this.orderByElements = orderByElements;
            this.returnIndex = 0;
        }

        public override RawRecord Next()
        {
            if (this.inputBuffer == null)
            {
                this.inputBuffer = new List<RawRecord>();

                RawRecord inputRec = null;
                while (this.inputOp.State() && (inputRec = this.inputOp.Next()) != null) {
                    this.inputBuffer.Add(inputRec);
                }

                this.inputBuffer.Sort((x, y) =>
                {
                    int ret = 0;
                    foreach (Tuple<ScalarFunction, IComparer> orderByElement in this.orderByElements)
                    {
                        ScalarFunction byFunction = orderByElement.Item1;

                        FieldObject xKey = byFunction.Evaluate(x);
                        if (xKey == null) {
                            throw new GraphViewException("The provided traversal or property name of Order does not map to a value.");
                        }

                        FieldObject yKey = byFunction.Evaluate(y);
                        if (yKey == null) {
                            throw new GraphViewException("The provided traversal or property name of Order does not map to a value.");
                        }

                        IComparer comparer = orderByElement.Item2;
                        ret = comparer.Compare(xKey.ToObject(), yKey.ToObject());

                        if (ret != 0) break;
                    }
                    return ret;
                });
            }

            while (this.returnIndex < this.inputBuffer.Count) {
                return this.inputBuffer[this.returnIndex++];
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputBuffer = null;
            this.inputOp.ResetState();
            this.returnIndex = 0;

            this.Open();
        }
    }

    internal class OrderLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int inputObjectIndex;
        private List<Tuple<ScalarFunction, IComparer>> orderByElements;

        private List<string> populateColumns;

        public OrderLocalOperator(
            GraphViewExecutionOperator inputOp, 
            int inputObjectIndex, 
            List<Tuple<ScalarFunction, IComparer>> orderByElements,
            List<string> populateColumns)
        {
            this.inputOp = inputOp;
            this.inputObjectIndex = inputObjectIndex;
            this.orderByElements = orderByElements;
            this.Open();

            this.populateColumns = populateColumns;
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                RawRecord newRecord = new RawRecord(srcRecord);

                FieldObject inputObject = srcRecord[this.inputObjectIndex];
                FieldObject orderedObject;
                if (inputObject is CollectionField)
                {
                    CollectionField inputCollection = (CollectionField)inputObject;
                    CollectionField orderedCollection = new CollectionField(inputCollection);
                    orderedCollection.Collection.Sort((x, y) =>
                    {
                        int ret = 0;
                        foreach (Tuple<ScalarFunction, IComparer> tuple in this.orderByElements)
                        {
                            ScalarFunction byFunction = tuple.Item1;

                            RawRecord initCompose1RecordOfX = new RawRecord();
                            initCompose1RecordOfX.Append(x);
                            FieldObject xKey = byFunction.Evaluate(initCompose1RecordOfX);
                            if (xKey == null) {
                                throw new GraphViewException("The provided traversal or property name of Order(local) does not map to a value.");
                            }

                            RawRecord initCompose1RecordOfY = new RawRecord();
                            initCompose1RecordOfY.Append(y);
                            FieldObject yKey = byFunction.Evaluate(initCompose1RecordOfY);
                            if (yKey == null) {
                                throw new GraphViewException("The provided traversal or property name of Order(local) does not map to a value.");
                            }

                            IComparer comparer = tuple.Item2;
                            ret = comparer.Compare(xKey.ToObject(), yKey.ToObject());

                            if (ret != 0) break;
                        }
                        return ret;
                    });
                    orderedObject = orderedCollection;
                }
                else if (inputObject is MapField)
                {
                    MapField inputMap = (MapField) inputObject;
                    List<EntryField> entries = inputMap.ToList();

                    entries.Sort((x, y) =>
                    {
                        int ret = 0;
                        foreach (Tuple<ScalarFunction, IComparer> tuple in this.orderByElements)
                        {
                            ScalarFunction byFunction = tuple.Item1;

                            RawRecord initKeyValuePairRecordOfX = new RawRecord();
                            initKeyValuePairRecordOfX.Append(x);
                            FieldObject xKey = byFunction.Evaluate(initKeyValuePairRecordOfX);
                            if (xKey == null) {
                                throw new GraphViewException("The provided traversal or property name of Order(local) does not map to a value.");
                            }
                            
                            RawRecord initKeyValuePairRecordOfY = new RawRecord();
                            initKeyValuePairRecordOfY.Append(y);
                            FieldObject yKey = byFunction.Evaluate(initKeyValuePairRecordOfY);
                            if (yKey == null) {
                                throw new GraphViewException("The provided traversal or property name of Order(local) does not map to a value.");
                            }
                            
                            IComparer comparer = tuple.Item2;
                            ret = comparer.Compare(xKey.ToObject(), yKey.ToObject());

                            if (ret != 0) break;
                        }
                        return ret;
                    });

                    MapField orderedMapField = new MapField();
                    foreach (EntryField entry in entries) {
                        orderedMapField.Add(entry.Key, entry.Value);
                    }
                    orderedObject = orderedMapField;
                }
                else {
                    orderedObject = inputObject;
                }

                RawRecord flatRawRecord = orderedObject.FlatToRawRecord(this.populateColumns);
                newRecord.Append(flatRawRecord);
                return newRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal interface IAggregateFunction
    {
        void Init();
        void Accumulate(params FieldObject[] values);
        FieldObject Terminate();
    }

    internal class ProjectOperator : GraphViewExecutionOperator
    {
        private List<ScalarFunction> selectScalarList;
        private GraphViewExecutionOperator inputOp;

        private RawRecord currentRecord;

        public ProjectOperator(GraphViewExecutionOperator inputOp)
        {
            this.Open();
            this.inputOp = inputOp;
            selectScalarList = new List<ScalarFunction>();
        }

        public void AddSelectScalarElement(ScalarFunction scalarFunction)
        {
            selectScalarList.Add(scalarFunction);
        }

        public override RawRecord Next()
        {
            currentRecord = inputOp.State() ? inputOp.Next() : null;
            if (currentRecord == null)
            {
                Close();
                return null;
            }

            RawRecord selectRecord = new RawRecord(selectScalarList.Count);
            int index = 0;
            foreach (var scalarFunction in selectScalarList)
            {
                // TODO: Skip * for now, need refactor
                // if (scalarFunction == null) continue;
                if (scalarFunction != null)
                {
                    FieldObject result = scalarFunction.Evaluate(currentRecord);
                    selectRecord.fieldValues[index++] = result;
                }
                else
                {
                    selectRecord.fieldValues[index++] = null;
                }
            }

            return selectRecord;
        }

        public override void ResetState()
        {
            currentRecord = null;
            inputOp.ResetState();
            Open();
        }
    }

    internal class ProjectAggregation : GraphViewExecutionOperator
    {
        List<Tuple<IAggregateFunction, List<ScalarFunction>>> aggregationSpecs;
        GraphViewExecutionOperator inputOp;

        public ProjectAggregation(GraphViewExecutionOperator inputOp)
        {
            this.inputOp = inputOp;
            aggregationSpecs = new List<Tuple<IAggregateFunction, List<ScalarFunction>>>();
            Open();
        }

        public void AddAggregateSpec(IAggregateFunction aggrFunc, List<ScalarFunction> aggrInput)
        {
            aggregationSpecs.Add(new Tuple<IAggregateFunction, List<ScalarFunction>>(aggrFunc, aggrInput));
        }

        public override void ResetState()
        {
            inputOp.ResetState();
            foreach (var aggr in aggregationSpecs)
            {
                if (aggr.Item1 != null)
                {
                    aggr.Item1.Init();
                }
            }
            Open();
        }

        public override RawRecord Next()
        {
            if (!State())
                return null;

            foreach (var aggr in aggregationSpecs)
            {
                if (aggr.Item1 != null)
                {
                    aggr.Item1.Init();
                }
            }

            RawRecord inputRec = null;
            while (inputOp.State() && (inputRec = inputOp.Next()) != null)
            {
                foreach (var aggr in aggregationSpecs)
                {
                    IAggregateFunction aggregate = aggr.Item1;
                    List<ScalarFunction> parameterFunctions = aggr.Item2;

                    if (aggregate == null)
                    {
                        continue;
                    }

                    FieldObject[] paraList = new FieldObject[aggr.Item2.Count];
                    for(int i = 0; i < parameterFunctions.Count; i++)
                    {
                        paraList[i] = parameterFunctions[i].Evaluate(inputRec); 
                    }

                    aggregate.Accumulate(paraList);
                }
            }

            RawRecord outputRec = new RawRecord();
            foreach (var aggr in aggregationSpecs)
            {
                if (aggr.Item1 != null)
                {
                    outputRec.Append(aggr.Item1.Terminate());
                }
                else
                {
                    outputRec.Append((StringField)null);
                }
            }

            Close();
            return outputRec;
        }
    }

    internal class MapOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;

        // The traversal inside the map function.
        private GraphViewExecutionOperator mapTraversal;
        private ConstantSourceOperator contextOp;

        public MapOperator(
            GraphViewExecutionOperator inputOp,
            GraphViewExecutionOperator mapTraversal,
            ConstantSourceOperator contextOp)
        {
            this.inputOp = inputOp;
            this.mapTraversal = mapTraversal;
            this.contextOp = contextOp;
            Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;
            while (inputOp.State() && (currentRecord = inputOp.Next()) != null)
            {
                contextOp.ConstantSource = currentRecord;
                mapTraversal.ResetState();
                RawRecord mapRec = mapTraversal.Next();
                mapTraversal.Close();

                if (mapRec == null) continue;
                RawRecord resultRecord = new RawRecord(currentRecord);
                resultRecord.Append(mapRec);

                return resultRecord;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            inputOp.ResetState();
            contextOp.ResetState();
            mapTraversal.ResetState();
            Open();
        }
    }

    internal class FlatMapOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;

        // The traversal inside the flatMap function.
        private GraphViewExecutionOperator flatMapTraversal;
        private ConstantSourceOperator contextOp;

        private RawRecord currentRecord = null;
        private Queue<RawRecord> outputBuffer;

        public FlatMapOperator(
            GraphViewExecutionOperator inputOp,
            GraphViewExecutionOperator flatMapTraversal,
            ConstantSourceOperator contextOp)
        {
            this.inputOp = inputOp;
            this.flatMapTraversal = flatMapTraversal;
            this.contextOp = contextOp;
            
            outputBuffer = new Queue<RawRecord>();
            Open();
        }

        public override RawRecord Next()
        {
            if (outputBuffer.Count > 0)
            {
                RawRecord r = new RawRecord(currentRecord);
                RawRecord toAppend = outputBuffer.Dequeue();
                r.Append(toAppend);

                return r;
            }

            while (inputOp.State())
            {
                currentRecord = inputOp.Next();
                if (currentRecord == null)
                {
                    Close();
                    return null;
                }

                contextOp.ConstantSource = currentRecord;
                flatMapTraversal.ResetState();
                RawRecord flatMapRec = null;
                while ((flatMapRec = flatMapTraversal.Next()) != null)
                {
                    outputBuffer.Enqueue(flatMapRec);
                }

                if (outputBuffer.Count > 0)
                {
                    RawRecord r = new RawRecord(currentRecord);
                    RawRecord toAppend = outputBuffer.Dequeue();
                    r.Append(toAppend);

                    return r;
                }
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            currentRecord = null;
            inputOp.ResetState();
            contextOp.ResetState();
            flatMapTraversal.ResetState();
            outputBuffer?.Clear();
            Open();
        }
    }

    internal class LocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;

        // The traversal inside the local function.
        private GraphViewExecutionOperator localTraversal;
        private ConstantSourceOperator contextOp;

        private RawRecord currentRecord = null;
        private Queue<RawRecord> outputBuffer;

        public LocalOperator(
            GraphViewExecutionOperator inputOp,
            GraphViewExecutionOperator localTraversal,
            ConstantSourceOperator contextOp)
        {
            this.inputOp = inputOp;
            this.localTraversal = localTraversal;
            this.contextOp = contextOp;

            outputBuffer = new Queue<RawRecord>();
            Open();
        }

        public override RawRecord Next()
        {
            if (outputBuffer.Count > 0)
            {
                RawRecord r = new RawRecord(currentRecord);
                RawRecord toAppend = outputBuffer.Dequeue();
                r.Append(toAppend);

                return r;
            }

            while (inputOp.State())
            {
                currentRecord = inputOp.Next();
                if (currentRecord == null)
                {
                    Close();
                    return null;
                }

                contextOp.ConstantSource = currentRecord;
                localTraversal.ResetState();
                RawRecord localRec = null;
                while ((localRec = localTraversal.Next()) != null)
                {
                    outputBuffer.Enqueue(localRec);
                }

                if (outputBuffer.Count > 0)
                {
                    RawRecord r = new RawRecord(currentRecord);
                    RawRecord toAppend = outputBuffer.Dequeue();
                    r.Append(toAppend);

                    return r;
                }
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            currentRecord = null;
            inputOp.ResetState();
            contextOp.ResetState();
            localTraversal.ResetState();
            outputBuffer?.Clear();
            Open();
        }
    }

    internal class OptionalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        // A list of record fields (identified by field indexes) from the input 
        // operator are to be returned when the optional traversal produces no results.
        // When a field index is less than 0, it means that this field value is always null. 
        private List<int> inputIndexes;

        // The traversal inside the optional function. 
        // The records returned by this operator should have the same number of fields
        // as the records drawn from the input operator, i.e., inputIndexes.Count 
        private GraphViewExecutionOperator optionalTraversal;
        private ConstantSourceOperator contextOp;
        private ContainerOperator rootContainerOp;

        private RawRecord currentRecord = null;
        private Queue<RawRecord> outputBuffer;

        private bool isCarryOnMode;
        private bool optionalTraversalHasResults;
        private bool hasReset;

        public OptionalOperator(
            GraphViewExecutionOperator inputOp,
            List<int> inputIndexes,
            GraphViewExecutionOperator optionalTraversal,
            ConstantSourceOperator contextOp,
            ContainerOperator containerOp,
            bool isCarryOnMode)
        {
            this.inputOp = inputOp;
            this.inputIndexes = inputIndexes;
            this.optionalTraversal = optionalTraversal;
            this.contextOp = contextOp;
            this.rootContainerOp = containerOp;

            this.isCarryOnMode = isCarryOnMode;
            this.optionalTraversalHasResults = false;
            this.hasReset = false;

            outputBuffer = new Queue<RawRecord>();
            Open();
        }

        public override RawRecord Next()
        {
            if (isCarryOnMode)
            {
                RawRecord traversalRecord;
                while (optionalTraversal.State() && (traversalRecord = optionalTraversal.Next()) != null)
                {
                    optionalTraversalHasResults = true;
                    return traversalRecord;
                }

                if (optionalTraversalHasResults)
                {
                    Close();
                    return null;
                }
                else
                {
                    if (!hasReset)
                    {
                        hasReset = true;
                        contextOp.ResetState();
                    }
                        
                    RawRecord inputRecord = null;
                    while (contextOp.State() && (inputRecord = contextOp.Next()) != null)
                    {
                        RawRecord r = new RawRecord(inputRecord);
                        foreach (int index in inputIndexes)
                        {
                            if (index < 0)
                            {
                                r.Append((FieldObject)null);
                            }
                            else
                            {
                                r.Append(inputRecord[index]);
                            }
                        }

                        return r;
                    }

                    Close();
                    return null;
                }
            }
            else
            {
                if (outputBuffer.Count > 0)
                {
                    RawRecord r = new RawRecord(currentRecord);
                    RawRecord toAppend = outputBuffer.Dequeue();
                    r.Append(toAppend);

                    return r;
                }

                while (inputOp.State())
                {
                    currentRecord = inputOp.Next();
                    if (currentRecord == null)
                    {
                        Close();
                        return null;
                    }

                    contextOp.ConstantSource = currentRecord;
                    optionalTraversal.ResetState();
                    RawRecord optionalRec = null;
                    while ((optionalRec = optionalTraversal.Next()) != null)
                    {
                        outputBuffer.Enqueue(optionalRec);
                    }

                    if (outputBuffer.Count > 0)
                    {
                        RawRecord r = new RawRecord(currentRecord);
                        RawRecord toAppend = outputBuffer.Dequeue();
                        r.Append(toAppend);

                        return r;
                    }
                    else
                    {
                        RawRecord r = new RawRecord(currentRecord);
                        foreach (int index in inputIndexes)
                        {
                            if (index < 0)
                            {
                                r.Append((FieldObject)null);
                            }
                            else
                            {
                                r.Append(currentRecord[index]);
                            }
                        }

                        return r;
                    }
                }

                Close();
                return null;
            }
        }

        public override void ResetState()
        {
            currentRecord = null;
            inputOp.ResetState();
            contextOp.ResetState();
            rootContainerOp?.ResetState();
            optionalTraversal.ResetState();
            outputBuffer?.Clear();
            Open();
        }
    }

    internal class UnionOperator : GraphViewExecutionOperator
    {
        private List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator>> traversalList;
        private int activeTraversalIndex;
        private ContainerOperator rootContainerOp;
        //
        // Only for union() without any branch
        //
        private GraphViewExecutionOperator inputOp;

        public UnionOperator(GraphViewExecutionOperator inputOp, ContainerOperator containerOp)
        {
            this.inputOp = inputOp;
            this.rootContainerOp = containerOp;
            traversalList = new List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator>>();
            Open();
            activeTraversalIndex = 0;
        }

        public void AddTraversal(ConstantSourceOperator contextOp, GraphViewExecutionOperator traversal)
        {
            traversalList.Add(new Tuple<ConstantSourceOperator, GraphViewExecutionOperator>(contextOp, traversal));
        }

        public override RawRecord Next()
        {
            //
            // Even the union() has no branch, the input still needs to be drained for cases like g.V().addV().union()
            //
            if (traversalList.Count == 0)
            {
                while (inputOp.State())
                {
                    inputOp.Next();
                }

                Close();
                return null;
            }

            RawRecord traversalRecord = null;
            while (traversalRecord == null && activeTraversalIndex < traversalList.Count)
            {
                GraphViewExecutionOperator activeOp = traversalList[activeTraversalIndex].Item2;
                if (activeOp.State() && (traversalRecord = activeOp.Next()) != null)
                {
                    break;
                }
                else
                {
                    activeTraversalIndex++;
                }
            }

            if (traversalRecord == null)
            {
                Close();
                return null;
            }
            else
            {
                return traversalRecord;
            }
        }

        public override void ResetState()
        {
            if (traversalList.Count == 0) {
                inputOp.ResetState();
            }

            foreach (Tuple<ConstantSourceOperator, GraphViewExecutionOperator> tuple in traversalList) {
                tuple.Item2.ResetState();
            }

            rootContainerOp.ResetState();
            activeTraversalIndex = 0;
            Open();
        }
    }

    internal class CoalesceOperator2 : GraphViewExecutionOperator
    {
        private List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator>> traversalList;
        private GraphViewExecutionOperator inputOp;

        private RawRecord currentRecord;
        private Queue<RawRecord> traversalOutputBuffer;

        public CoalesceOperator2(GraphViewExecutionOperator inputOp)
        {
            this.inputOp = inputOp;
            traversalList = new List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator>>();
            traversalOutputBuffer = new Queue<RawRecord>();
            Open();
        }

        public void AddTraversal(ConstantSourceOperator contextOp, GraphViewExecutionOperator traversal)
        {
            traversalList.Add(new Tuple<ConstantSourceOperator, GraphViewExecutionOperator>(contextOp, traversal));
        }

        public override RawRecord Next()
        {
            while (traversalOutputBuffer.Count == 0 && inputOp.State())
            {
                currentRecord = inputOp.Next();
                if (currentRecord == null)
                {
                    Close();
                    return null;
                }

                foreach (var traversalPair in traversalList)
                {
                    ConstantSourceOperator traversalContext = traversalPair.Item1;
                    GraphViewExecutionOperator traversal = traversalPair.Item2;
                    traversalContext.ConstantSource = currentRecord;
                    traversal.ResetState();

                    RawRecord traversalRec = null;
                    while ((traversalRec = traversal.Next()) != null)
                    {
                        traversalOutputBuffer.Enqueue(traversalRec);
                    }

                    if (traversalOutputBuffer.Count > 0)
                    {
                        break;
                    }
                }
            }

            if (traversalOutputBuffer.Count > 0)
            {
                RawRecord r = new RawRecord(currentRecord);
                RawRecord traversalRec = traversalOutputBuffer.Dequeue();
                r.Append(traversalRec);

                return r;
            }
            else
            {
                Close();
                return null;
            }
        }

        public override void ResetState()
        {
            currentRecord = null;
            inputOp.ResetState();
            traversalOutputBuffer?.Clear();
            Open();
        }
    }

    internal class RepeatOperator : GraphViewExecutionOperator
    {
        // Number of times the inner operator repeats itself.
        // If this number is less than 0, the termination condition 
        // is specified by a boolean function. 
        private readonly int repeatTimes;
        private int currentRepeatTimes;

        // The termination condition of iterations
        private readonly BooleanFunction terminationCondition;
        // If this variable is true, the iteration starts with the context record. 
        // This corresponds to the while-do loop semantics. 
        // Otherwise, the iteration starts with the the output of the first execution of the inner operator,
        // which corresponds to the do-while loop semantics.
        private readonly bool startFromContext;
        // The condition determining whether or not an intermediate state is emitted
        private readonly BooleanFunction emitCondition;
        // This variable specifies whether or not the context record is considered 
        // to be emitted when the iteration does not start with the context record,
        // i.e., startFromContext is false 
        private readonly bool emitContext;

        private readonly GraphViewExecutionOperator inputOp;
        // initialOp recieves records from the input operator
        // and extracts needed columns to generate records that are fed as the initial input into the inner operator.
        private readonly ConstantSourceOperator initialSourceOp;
        private readonly GraphViewExecutionOperator initialOp;

        private readonly ConstantSourceOperator tempSourceOp;
        private readonly ContainerOperator innerSourceOp;
        private readonly GraphViewExecutionOperator innerOp;

        private Queue<RawRecord> priorStates;
        private Queue<RawRecord> newStates;
        private readonly Queue<RawRecord> repeatResultBuffer;

        public RepeatOperator(
            GraphViewExecutionOperator inputOp,
            ConstantSourceOperator initialSourceOp,
            GraphViewExecutionOperator initialOp,
            ConstantSourceOperator tempSourceOp,
            ContainerOperator innerSourceOp,
            GraphViewExecutionOperator innerOp,
            int repeatTimes,
            BooleanFunction emitCondition,
            bool emitContext)
        {
            this.inputOp = inputOp;
            this.initialSourceOp = initialSourceOp;
            this.initialOp = initialOp;

            this.tempSourceOp = tempSourceOp;
            this.innerSourceOp = innerSourceOp;
            this.innerOp = innerOp;

            // By current implementation of Gremlin, when repeat time is set to 0,
            // it is reset to 1.
            this.repeatTimes = repeatTimes == 0 ? 1 : repeatTimes;
            this.currentRepeatTimes = 0;
            this.emitCondition = emitCondition;
            this.emitContext = emitContext;

            this.startFromContext = false;

            this.priorStates = new Queue<RawRecord>();
            this.newStates = new Queue<RawRecord>();
            this.repeatResultBuffer = new Queue<RawRecord>();
            this.Open();
        }

        public RepeatOperator(
            GraphViewExecutionOperator inputOp,
            ConstantSourceOperator initialSourceOp,
            GraphViewExecutionOperator initialOp,
            ConstantSourceOperator tempSourceOp,
            ContainerOperator innerSourceOp,
            GraphViewExecutionOperator innerOp,
            BooleanFunction terminationCondition,
            bool startFromContext,
            BooleanFunction emitCondition,
            bool emitContext)
        {
            this.inputOp = inputOp;
            this.initialSourceOp = initialSourceOp;
            this.initialOp = initialOp;

            this.tempSourceOp = tempSourceOp;
            this.innerSourceOp = innerSourceOp;
            this.innerOp = innerOp;

            this.terminationCondition = terminationCondition;
            this.startFromContext = startFromContext;
            this.emitCondition = emitCondition;
            this.emitContext = emitContext;
            this.repeatTimes = -1;

            this.priorStates = new Queue<RawRecord>();
            this.newStates = new Queue<RawRecord>();
            this.repeatResultBuffer = new Queue<RawRecord>();
            this.Open();
        }

        private void PrepareInnerOpSource(Queue<RawRecord> innerSourceRecords)
        {
            this.innerSourceOp.ResetState();
            this.tempSourceOp.ConstantSource = null;
            while (innerSourceRecords.Any()) {
                this.tempSourceOp.ConstantSource = innerSourceRecords.Dequeue();
                this.innerSourceOp.Next();
            }
        }

        public override RawRecord Next()
        {
            if (this.repeatResultBuffer.Count > 0) {
                return this.repeatResultBuffer.Dequeue();
            }

            if (this.repeatTimes > 0)
            {
                if (this.currentRepeatTimes > this.repeatTimes) {
                    this.Close();
                    return null;
                }

                if (this.currentRepeatTimes == 0)
                {
                    RawRecord outerRecord;
                    while (this.inputOp.State() && (outerRecord = this.inputOp.Next()) != null)
                    {
                        this.initialSourceOp.ConstantSource = outerRecord;
                        this.initialOp.ResetState();
                        RawRecord initialRec = this.initialOp.Next();

                        if (this.emitCondition != null && this.emitContext) {
                            if (this.emitCondition.Evaluate(initialRec)) {
                                this.repeatResultBuffer.Enqueue(initialRec);
                            }
                        }

                        this.priorStates.Enqueue(initialRec);
                    }
                    this.currentRepeatTimes++;
                    if (this.repeatResultBuffer.Count > 0) {
                        return this.repeatResultBuffer.Dequeue();
                    }
                }
                //
                // Evaluates the inner traversal for the [currentRepeatTimes] times iteration
                //
                while (this.currentRepeatTimes <= this.repeatTimes)
                {
                    this.PrepareInnerOpSource(this.priorStates);
                    this.innerOp.ResetState();

                    RawRecord newRec;
                    while ((newRec = innerOp.Next()) != null) {
                        this.newStates.Enqueue(newRec);
                        if (this.emitCondition != null && this.emitCondition.Evaluate(newRec)) {
                            this.repeatResultBuffer.Enqueue(newRec);
                        }
                    }

                    if (this.repeatResultBuffer.Count > 0) {
                        return this.repeatResultBuffer.Dequeue();
                    }

                    Queue<RawRecord> tmpQueue = this.priorStates;
                    this.priorStates = this.newStates;
                    this.newStates = tmpQueue;
                    this.currentRepeatTimes++;
                }

                //
                // if emitCondition != null, the results of the final round iteration have already been emitted
                //
                if (this.emitCondition == null) {
                    foreach (RawRecord resultRec in priorStates) {
                        this.repeatResultBuffer.Enqueue(resultRec);
                    }
                }
                if (this.repeatResultBuffer.Count > 0) {
                    return this.repeatResultBuffer.Dequeue();
                }
            }
            else
            {
                RawRecord outerRecord;
                while (this.inputOp.State() && (outerRecord = this.inputOp.Next()) != null)
                {
                    this.initialSourceOp.ConstantSource = outerRecord;
                    this.initialOp.ResetState();
                    RawRecord initialRec = this.initialOp.Next();

                    if (this.startFromContext) {
                        if (this.terminationCondition != null && this.terminationCondition.Evaluate(initialRec)) {
                            this.repeatResultBuffer.Enqueue(initialRec);
                        }
                        else if (this.emitContext) {
                            if (this.emitCondition == null || this.emitCondition.Evaluate(initialRec)) {
                                this.repeatResultBuffer.Enqueue(initialRec);
                            }
                        }
                    }
                    else {
                        if (this.emitContext && this.emitCondition != null) {
                            if (this.emitCondition.Evaluate(initialRec)) {
                                this.repeatResultBuffer.Enqueue(initialRec);
                            }
                        }
                    }

                    this.priorStates.Enqueue(initialRec);
                }

                if (this.repeatResultBuffer.Count > 0) {
                    return this.repeatResultBuffer.Dequeue();
                }

                while (this.priorStates.Count > 0)
                {
                    this.PrepareInnerOpSource(this.priorStates);
                    this.innerOp.ResetState();

                    RawRecord newRec;
                    while ((newRec = innerOp.Next()) != null) {
                        if (this.terminationCondition != null && this.terminationCondition.Evaluate(newRec)) {
                            this.repeatResultBuffer.Enqueue(newRec);
                        }
                        else {
                            if (this.emitCondition != null && this.emitCondition.Evaluate(newRec)) {
                                this.repeatResultBuffer.Enqueue(newRec);
                            }
                            this.priorStates.Enqueue(newRec);
                        }
                    }

                    if (this.repeatResultBuffer.Count > 0) {
                        return this.repeatResultBuffer.Dequeue();
                    }
                }
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.currentRepeatTimes = 0;
            this.inputOp.ResetState();
            this.innerOp.ResetState();
            this.priorStates.Clear();
            this.newStates.Clear();
            this.repeatResultBuffer.Clear();
            this.Open();
        }
    }

    internal class DeduplicateOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private HashSet<CollectionField> compositeDedupKeySet;
        private List<ScalarFunction> compositeDedupKeyFuncList;

        internal DeduplicateOperator(GraphViewExecutionOperator inputOperator, List<ScalarFunction> compositeDedupKeyFuncList)
        {
            this.inputOp = inputOperator;
            this.compositeDedupKeyFuncList = compositeDedupKeyFuncList;
            this.compositeDedupKeySet = new HashSet<CollectionField>();

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;
                
            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                List<FieldObject> keys = new List<FieldObject>();
                for (int dedupKeyIndex = 0; dedupKeyIndex < compositeDedupKeyFuncList.Count; dedupKeyIndex++)
                {
                    ScalarFunction getDedupKeyFunc = compositeDedupKeyFuncList[dedupKeyIndex];
                    FieldObject key = getDedupKeyFunc.Evaluate(srcRecord);
                    if (key == null) {
                        throw new GraphViewException("The provided traversal or property name of Dedup does not map to a value.");
                    }

                    keys.Add(key);
                }

                CollectionField compositeDedupKey = new CollectionField(keys);
                if (!this.compositeDedupKeySet.Contains(compositeDedupKey))
                {
                    this.compositeDedupKeySet.Add(compositeDedupKey);
                    return srcRecord;
                }
            }

            this.Close();
            this.compositeDedupKeySet.Clear();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.compositeDedupKeySet.Clear();

            this.Open();
        }
    }

    internal class DeduplicateLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private ScalarFunction getInputObjectionFunc;
        
        internal DeduplicateLocalOperator(
            GraphViewExecutionOperator inputOperator, 
            ScalarFunction getInputObjectionFunc)
        {
            this.inputOp = inputOperator;
            this.getInputObjectionFunc = getInputObjectionFunc;

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                RawRecord result = new RawRecord(currentRecord);
                FieldObject inputObject = this.getInputObjectionFunc.Evaluate(currentRecord);

                HashSet<Object> localObjectsSet = new HashSet<Object>();
                CollectionField uniqueCollection = new CollectionField();

                if (inputObject is CollectionField)
                {
                    CollectionField inputCollection = (CollectionField)inputObject;
                    
                    foreach (FieldObject localFieldObject in inputCollection.Collection)
                    {
                        Object localObj = localFieldObject.ToObject();
                        if (!localObjectsSet.Contains(localObj))
                        {
                            uniqueCollection.Collection.Add(localFieldObject);
                            localObjectsSet.Add(localObj);
                        }
                    }
                }
                else if (inputObject is PathField)
                {
                    PathField inputPath = (PathField)inputObject;

                    foreach (PathStepField pathStep in inputPath.Path.Cast<PathStepField>())
                    {
                        Object localObj = pathStep.ToObject();
                        if (!localObjectsSet.Contains(localObj))
                        {
                            uniqueCollection.Collection.Add(pathStep.StepFieldObject);
                            localObjectsSet.Add(localObj);
                        }
                    }
                }
                else {
                    throw new GraphViewException("Dedup(local) can only be applied to a list.");
                }

                result.Append(uniqueCollection);
                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class RangeOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int startIndex;
        //
        // if count is -1, return all the records starting from startIndex
        //
        private int highEnd;
        private int index;

        internal RangeOperator(GraphViewExecutionOperator inputOp, int startIndex, int count)
        {
            this.inputOp = inputOp;
            this.startIndex = startIndex;
            this.highEnd = count == -1 ? -1 : startIndex + count;
            this.index = 0;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            //
            // Return records in the [startIndex, highEnd)
            //
            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                if (this.index < this.startIndex || (this.highEnd != -1 && this.index >= this.highEnd))
                {
                    this.index++;
                    continue;
                }

                this.index++;
                return srcRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.index = 0;
            this.Open();
        }
    }

    internal class RangeLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int startIndex;
        //
        // if count is -1, return all the records starting from startIndex
        //
        private int count;
        private int inputCollectionIndex;

        private List<string> populateColumns;
        private bool wantSingleObject;

        internal RangeLocalOperator(
            GraphViewExecutionOperator inputOp, 
            int inputCollectionIndex, 
            int startIndex, int count,
            List<string> populateColumns)
        {
            this.inputOp = inputOp;
            this.startIndex = startIndex;
            this.count = count;
            this.inputCollectionIndex = inputCollectionIndex;
            this.populateColumns = populateColumns;
            this.wantSingleObject = this.count == 1;

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                RawRecord newRecord = new RawRecord(srcRecord);
                //
                // Return records in the [runtimeStartIndex, runtimeStartIndex + runtimeCount)
                //
                FieldObject inputObject = srcRecord[inputCollectionIndex];
                FieldObject filteredObject;
                if (inputObject is CollectionField)
                {
                    CollectionField inputCollection = (CollectionField)inputObject;
                    CollectionField newCollectionField = new CollectionField();

                    int runtimeStartIndex = startIndex > inputCollection.Collection.Count ? inputCollection.Collection.Count : startIndex;
                    int runtimeCount = this.count == -1 ? inputCollection.Collection.Count - runtimeStartIndex : this.count;
                    if (runtimeStartIndex + runtimeCount > inputCollection.Collection.Count) {
                        runtimeCount = inputCollection.Collection.Count - runtimeStartIndex;
                    }

                    newCollectionField.Collection = inputCollection.Collection.GetRange(runtimeStartIndex, runtimeCount);
                    if (wantSingleObject) {
                        filteredObject = newCollectionField.Collection.Any() ? newCollectionField.Collection[0] : null;
                    }
                    else {
                        filteredObject = newCollectionField;
                    }
                }
                else if (inputObject is PathField)
                {
                    PathField inputPath = (PathField)inputObject;
                    CollectionField newCollectionField = new CollectionField();

                    int runtimeStartIndex = startIndex > inputPath.Path.Count ? inputPath.Path.Count : startIndex;
                    int runtimeCount = this.count == -1 ? inputPath.Path.Count - runtimeStartIndex : this.count;
                    if (runtimeStartIndex + runtimeCount > inputPath.Path.Count) {
                        runtimeCount = inputPath.Path.Count - runtimeStartIndex;
                    }

                    newCollectionField.Collection =
                        inputPath.Path.GetRange(runtimeStartIndex, runtimeCount)
                            .Cast<PathStepField>()
                            .Select(p => p.StepFieldObject)
                            .ToList();
                    if (wantSingleObject) {
                        filteredObject = newCollectionField.Collection.Any() ? newCollectionField.Collection[0] : null;
                    }
                    else {
                        filteredObject = newCollectionField;
                    }
                }
                //
                // Return records in the [low, high)
                //
                else if (inputObject is MapField)
                {
                    MapField inputMap = (MapField)inputObject;
                    MapField newMap = new MapField();

                    int low = startIndex;
                    int high = this.count == -1 ? inputMap.Count : low + this.count;

                    int index = 0;
                    foreach (EntryField entry in inputMap) {
                        if (index >= low && index < high) {
                            newMap.Add(entry.Key, entry.Value);
                        }
                        if (++index >= high) {
                            break;
                        }
                    }
                    filteredObject = newMap;
                }
                else {
                    filteredObject = inputObject;
                }

                if (filteredObject == null) {
                    continue;
                }
                RawRecord flatRawRecord = filteredObject.FlatToRawRecord(this.populateColumns);
                newRecord.Append(flatRawRecord);
                return newRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class TailOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int lastN;
        private int count;
        private List<RawRecord> buffer; 

        internal TailOperator(GraphViewExecutionOperator inputOp, int lastN)
        {
            this.inputOp = inputOp;
            this.lastN = lastN;
            this.count = 0;
            this.buffer = new List<RawRecord>();

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null) {
                buffer.Add(srcRecord);
            }

            //
            // Reutn records from [buffer.Count - lastN, buffer.Count)
            //

            int startIndex = buffer.Count < lastN ? 0 : buffer.Count - lastN;
            int index = startIndex + this.count++;
            while (index < buffer.Count) {
                return buffer[index];
            } 

            this.Close();
            this.buffer.Clear();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.count = 0;
            this.buffer.Clear();
            this.Open();
        }
    }

    internal class TailLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int lastN;
        private int inputCollectionIndex;

        private List<string> populateColumns;
        private bool wantSingleObject;

        internal TailLocalOperator(
            GraphViewExecutionOperator inputOp, 
            int inputCollectionIndex, int lastN,
            List<string> populateColumns)
        {
            this.inputOp = inputOp;
            this.inputCollectionIndex = inputCollectionIndex;
            this.lastN = lastN;
            this.populateColumns = populateColumns;
            this.wantSingleObject = this.lastN == 1;

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                RawRecord newRecord = new RawRecord(srcRecord);
                //
                // Return records in the [localCollection.Count - lastN, localCollection.Count)
                //
                FieldObject inputObject = srcRecord[inputCollectionIndex];
                FieldObject filteredObject;
                if (inputObject is CollectionField)
                {
                    CollectionField inputCollection = (CollectionField)inputObject;
                    CollectionField newCollection = new CollectionField();

                    int startIndex = inputCollection.Collection.Count < lastN 
                                     ? 0 
                                     : inputCollection.Collection.Count - lastN;
                    int count = startIndex + lastN > inputCollection.Collection.Count
                                     ? inputCollection.Collection.Count - startIndex
                                     : lastN;

                    newCollection.Collection = inputCollection.Collection.GetRange(startIndex, count);
                    if (wantSingleObject) {
                        filteredObject = newCollection.Collection.Any() ? newCollection.Collection[0] : null;
                    }
                    else {
                        filteredObject = newCollection;
                    }
                }
                else if (inputObject is PathField)
                {
                    PathField inputPath = (PathField)inputObject;
                    CollectionField newCollection = new CollectionField();

                    int startIndex = inputPath.Path.Count < lastN
                                     ? 0
                                     : inputPath.Path.Count - lastN;
                    int count = startIndex + lastN > inputPath.Path.Count
                                     ? inputPath.Path.Count - startIndex
                                     : lastN;

                    newCollection.Collection =
                        inputPath.Path.GetRange(startIndex, count)
                            .Cast<PathStepField>()
                            .Select(p => p.StepFieldObject)
                            .ToList();
                    if (wantSingleObject) {
                        filteredObject = newCollection.Collection.Any() ? newCollection.Collection[0] : null;
                    }
                    else {
                        filteredObject = newCollection;
                    }
                }
                //
                // Return records in the [low, inputMap.Count)
                //
                else if (inputObject is MapField)
                {
                    MapField inputMap = inputObject as MapField;
                    MapField newMap = new MapField();
                    int low = inputMap.Count - lastN;

                    int index = 0;
                    foreach (EntryField entry in inputMap) {
                        if (index++ >= low)
                            newMap.Add(entry.Key, entry.Value);
                    }
                    filteredObject = newMap;
                }
                else {
                    filteredObject = inputObject;
                }

                if (filteredObject == null) {
                    continue;
                }
                RawRecord flatRawRecord = filteredObject.FlatToRawRecord(this.populateColumns);
                newRecord.Append(flatRawRecord);
                return newRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class SideEffectOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;

        private GraphViewExecutionOperator sideEffectTraversal;
        private ConstantSourceOperator contextOp;

        public SideEffectOperator(
            GraphViewExecutionOperator inputOp,
            GraphViewExecutionOperator sideEffectTraversal,
            ConstantSourceOperator contextOp)
        {
            this.inputOp = inputOp;
            this.sideEffectTraversal = sideEffectTraversal;
            this.contextOp = contextOp;

            Open();
        }

        public override RawRecord Next()
        {
            while (inputOp.State())
            {
                RawRecord currentRecord = inputOp.Next();
                if (currentRecord == null)
                {
                    Close();
                    return null;
                }

                //RawRecord resultRecord = new RawRecord(currentRecord);
                contextOp.ConstantSource = currentRecord;
                sideEffectTraversal.ResetState();

                while (sideEffectTraversal.State())
                {
                    sideEffectTraversal.Next();
                }

                return currentRecord;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            inputOp.ResetState();
            contextOp.ResetState();
            sideEffectTraversal.ResetState();
            Open();
        }
    }

    internal class InjectOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;

        private readonly int inputRecordColumnsCount;
        private readonly int injectColumnIndex;

        private readonly bool isList;
        private readonly string defaultProjectionKey;

        private readonly List<ScalarFunction> injectValues;

        private bool hasInjected;

        public InjectOperator(
            GraphViewExecutionOperator inputOp,
            int inputRecordColumnsCount,
            int injectColumnIndex,
            List<ScalarFunction> injectValues,
            bool isList,
            string defalutProjectionKey
            )
        {
            this.inputOp = inputOp;
            this.inputRecordColumnsCount = inputRecordColumnsCount;
            this.injectColumnIndex = injectColumnIndex;
            this.injectValues = injectValues;
            this.isList = isList;
            this.defaultProjectionKey = defalutProjectionKey;
            this.hasInjected = false;

            this.Open();
        }

        public override RawRecord Next()
        {
            if (!this.hasInjected)
            {
                this.hasInjected = true;
                RawRecord result = new RawRecord();

                if (isList)
                {
                    List<FieldObject> collection = new List<FieldObject>();
                    foreach (ScalarFunction injectValueFunc in this.injectValues)
                    {
                        Dictionary<string, FieldObject> compositeFieldObjects = new Dictionary<string, FieldObject>();
                        compositeFieldObjects.Add(defaultProjectionKey, injectValueFunc.Evaluate(null));
                        collection.Add(new Compose1Field(compositeFieldObjects, defaultProjectionKey));
                    }

                    //
                    // g.Inject()
                    //
                    if (this.inputRecordColumnsCount == 0) {
                        result.Append(new CollectionField(collection));
                    }
                    else
                    {
                        for (int columnIndex = 0; columnIndex < this.inputRecordColumnsCount; columnIndex++) {
                            if (columnIndex == this.injectColumnIndex)
                                result.Append(new CollectionField(collection));
                            else
                                result.Append((FieldObject)null);
                        }
                    }

                    return result;
                }
                else
                {
                    //
                    // g.Inject()
                    //
                    if (this.inputRecordColumnsCount == 0) {
                        result.Append(this.injectValues[0].Evaluate(null));
                    }
                    else
                    {
                        for (int columnIndex = 0; columnIndex < this.inputRecordColumnsCount; columnIndex++) {
                            if (columnIndex == this.injectColumnIndex)
                                result.Append(this.injectValues[0].Evaluate(null));
                            else
                                result.Append((FieldObject)null);
                        }
                    }

                    return result;
                }
            }


            RawRecord r = null;
            //
            // For the g.Inject() case, Inject operator itself is the first operator, and its inputOp is null
            //
            if (this.inputOp != null) {
                r = this.inputOp.State() ? this.inputOp.Next() : null;
            }

            if (r == null) {
                this.Close();
            }

            return r;
        }

        public override void ResetState()
        {
            this.hasInjected = false;
            this.Open();
        }
    }

    internal class AggregateOperator : GraphViewExecutionOperator
    {
        CollectionFunction aggregateState;
        GraphViewExecutionOperator inputOp;
        ScalarFunction getAggregateObjectFunction;
        Queue<RawRecord> outputBuffer;

        public AggregateOperator(GraphViewExecutionOperator inputOp, ScalarFunction getTargetFieldFunction, CollectionFunction aggregateState)
        {
            this.aggregateState = aggregateState;
            this.inputOp = inputOp;
            this.getAggregateObjectFunction = getTargetFieldFunction;
            this.outputBuffer = new Queue<RawRecord>();

            Open();
        }

        public override RawRecord Next()
        {
            RawRecord r = null;
            while (inputOp.State() && (r = inputOp.Next()) != null)
            {
                RawRecord result = new RawRecord(r);

                FieldObject aggregateObject = getAggregateObjectFunction.Evaluate(r);

                if (aggregateObject == null)
                    throw new GraphViewException("The provided traversal or property name in Aggregate does not map to a value.");

                aggregateState.Accumulate(aggregateObject);

                result.Append(aggregateState.CollectionField);

                outputBuffer.Enqueue(result);
            }

            if (outputBuffer.Count <= 1) Close();
            if (outputBuffer.Count != 0) return outputBuffer.Dequeue();
            return null;
        }

        public override void ResetState()
        {
            //aggregateState.Init();
            inputOp.ResetState();
            Open();
        }
    }

    internal class StoreOperator : GraphViewExecutionOperator
    {
        CollectionFunction storeState;
        GraphViewExecutionOperator inputOp;
        ScalarFunction getStoreObjectFunction;

        public StoreOperator(GraphViewExecutionOperator inputOp, ScalarFunction getTargetFieldFunction, CollectionFunction storeState)
        {
            this.storeState = storeState;
            this.inputOp = inputOp;
            this.getStoreObjectFunction = getTargetFieldFunction;
            Open();
        }

        public override RawRecord Next()
        {
            if (inputOp.State())
            {
                RawRecord r = inputOp.Next();
                if (r == null)
                {
                    Close();
                    return null;
                }

                RawRecord result = new RawRecord(r);

                FieldObject storeObject = getStoreObjectFunction.Evaluate(r);

                if (storeObject == null)
                    throw new GraphViewException("The provided traversal or property name in Store does not map to a value.");

                storeState.Accumulate(storeObject);

                result.Append(storeState.CollectionField);

                if (!inputOp.State())
                {
                    Close();
                }
                return result;
            }

            return null;
        }

        public override void ResetState()
        {
            //storeState.Init();
            inputOp.ResetState();
            Open();
        }
    }


    //
    // Note: our BarrierOperator's semantics is not the same the one's in Gremlin
    //
    internal class BarrierOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator _inputOp;
        private Queue<RawRecord> _outputBuffer;
        private int _outputBufferSize;

        public BarrierOperator(GraphViewExecutionOperator inputOp, int outputBufferSize = -1)
        {
            _inputOp = inputOp;
            _outputBuffer = new Queue<RawRecord>();
            _outputBufferSize = outputBufferSize;
            Open();
        }
          
        public override RawRecord Next()
        {
            while (_outputBuffer.Any()) {
                return _outputBuffer.Dequeue();
            }

            RawRecord record;
            while ((_outputBufferSize == -1 || _outputBuffer.Count <= _outputBufferSize) 
                    && _inputOp.State() 
                    && (record = _inputOp.Next()) != null)
            {
                _outputBuffer.Enqueue(record);
            }

            if (_outputBuffer.Count <= 1) Close();
            if (_outputBuffer.Count != 0) return _outputBuffer.Dequeue();
            return null;
        }

        public override void ResetState()
        {
            _inputOp.ResetState();
            _outputBuffer.Clear();
            Open();
        }
    }

    internal class ProjectByOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator _inputOp;
        private List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator, string>> _projectList;

        internal ProjectByOperator(GraphViewExecutionOperator pInputOperator)
        {
            _inputOp = pInputOperator;
            _projectList = new List<Tuple<ConstantSourceOperator, GraphViewExecutionOperator, string>>();
            Open();
        }

        public void AddProjectBy(ConstantSourceOperator contextOp, GraphViewExecutionOperator traversal, string key)
        {
            _projectList.Add(new Tuple<ConstantSourceOperator, GraphViewExecutionOperator, string>(contextOp, traversal, key));
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (_inputOp.State() && (currentRecord = _inputOp.Next()) != null)
            {
                MapField projectMap = new MapField();
                RawRecord extraRecord = new RawRecord();

                foreach (var tuple in _projectList)
                {
                    string projectKey = tuple.Item3;
                    ConstantSourceOperator projectContext = tuple.Item1;
                    GraphViewExecutionOperator projectTraversal = tuple.Item2;
                    projectContext.ConstantSource = currentRecord;
                    projectTraversal.ResetState();

                    RawRecord projectRec = projectTraversal.Next();
                    projectTraversal.Close();

                    if (projectRec == null)
                        throw new GraphViewException(
                            string.Format("The provided traverser of key \"{0}\" does not map to a value.", projectKey));

                    projectMap.Add(new StringField(projectKey), projectRec.RetriveData(0));
                    for (var i = 1; i < projectRec.Length; i++)
                        extraRecord.Append(projectRec[i]);
                }

                var result = new RawRecord(currentRecord);
                result.Append(projectMap);
                if (extraRecord.Length > 0)
                    result.Append(extraRecord);

                return result;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            _inputOp.ResetState();
            Open();
        }
    }

    internal class PropertyKeyOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int propertyFieldIndex;

        public PropertyKeyOperator(GraphViewExecutionOperator inputOp, int propertyFieldIndex)
        {
            this.inputOp = inputOp;
            this.propertyFieldIndex = propertyFieldIndex;
            this.Open();
        }


        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (inputOp.State() && (currentRecord = inputOp.Next()) != null)
            {
                PropertyField p = currentRecord[this.propertyFieldIndex] as PropertyField;
                if (p == null)
                    continue;

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField(p.PropertyName));

                return result;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class PropertyValueOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int propertyFieldIndex;

        public PropertyValueOperator(GraphViewExecutionOperator inputOp, int propertyFieldIndex)
        {
            this.inputOp = inputOp;
            this.propertyFieldIndex = propertyFieldIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (inputOp.State() && (currentRecord = inputOp.Next()) != null)
            {
                PropertyField p = currentRecord[this.propertyFieldIndex] as PropertyField;
                if (p == null)
                    continue;

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField(p.PropertyValue, p.JsonDataType));

                return result;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class QueryDerivedTableOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator _queryOp;
        private ContainerOperator _rootContainerOp;

        public QueryDerivedTableOperator(GraphViewExecutionOperator queryOp, ContainerOperator containerOp)
        {
            _queryOp = queryOp;
            _rootContainerOp = containerOp;

            Open();
        }

        public override RawRecord Next()
        {
            RawRecord derivedRecord;

            while (_queryOp.State() && (derivedRecord = _queryOp.Next()) != null)
            {
                return derivedRecord;
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            _queryOp.ResetState();
            _rootContainerOp?.ResetState();

            Open();
        }
    }

    internal class CountLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int objectIndex;

        public CountLocalOperator(GraphViewExecutionOperator inputOp, int objectIndex)
        {
            this.inputOp = inputOp;
            this.objectIndex = objectIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                RawRecord result = new RawRecord(currentRecord);
                FieldObject obj = currentRecord[this.objectIndex];
                Debug.Assert(obj != null, "The input of the CountLocalOperator should not be null.");

                if (obj is CollectionField)
                    result.Append(new StringField(((CollectionField)obj).Collection.Count.ToString(), JsonDataType.Long));
                else if (obj is PathField)
                    result.Append(new StringField(((PathField)obj).Path.Count.ToString(), JsonDataType.Long));
                else if (obj is MapField)
                    result.Append(new StringField(((MapField)obj).Count.ToString(), JsonDataType.Long));
                else if (obj is TreeField)
                    result.Append(new StringField(((TreeField)obj).Children.Count.ToString(), JsonDataType.Long));
                else
                    result.Append(new StringField("1", JsonDataType.Long));

                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class SumLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int objectIndex;

        public SumLocalOperator(GraphViewExecutionOperator inputOp, int objectIndex)
        {
            this.inputOp = inputOp;
            this.objectIndex = objectIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                FieldObject obj = currentRecord[this.objectIndex];
                Debug.Assert(obj != null, "The input of the SumLocalOperator should not be null.");

                double sum = 0.0;
                double current;

                if (obj is CollectionField)
                {
                    foreach (FieldObject fieldObject in ((CollectionField)obj).Collection)
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        sum += current;
                    }
                }
                else if (obj is PathField)
                {
                    foreach (FieldObject fieldObject in ((PathField)obj).Path)
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        sum += current;
                    }
                }
                else {
                    sum = double.TryParse(obj.ToValue, out current) ? current : double.NaN;
                }

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField(sum.ToString(CultureInfo.InvariantCulture), JsonDataType.Double));

                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class MaxLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int objectIndex;

        public MaxLocalOperator(GraphViewExecutionOperator inputOp, int objectIndex)
        {
            this.inputOp = inputOp;
            this.objectIndex = objectIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                FieldObject obj = currentRecord[this.objectIndex];
                Debug.Assert(obj != null, "The input of the MaxLocalOperator should not be null.");

                double max = double.MinValue;
                double current;

                if (obj is CollectionField)
                {
                    foreach (FieldObject fieldObject in ((CollectionField)obj).Collection)
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        if (max < current)
                            max = current;
                    }
                }
                else if (obj is PathField)
                {
                    foreach (PathStepField fieldObject in ((PathField)obj).Path.Cast<PathStepField>())
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        if (max < current)
                            max = current;
                    }
                }
                else {
                    max = double.TryParse(obj.ToValue, out current) ? current : double.NaN;
                }

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField(max.ToString(CultureInfo.InvariantCulture), JsonDataType.Double));

                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class MinLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int objectIndex;

        public MinLocalOperator(GraphViewExecutionOperator inputOp, int objectIndex)
        {
            this.inputOp = inputOp;
            this.objectIndex = objectIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                FieldObject obj = currentRecord[this.objectIndex];
                Debug.Assert(obj != null, "The input of the MinLocalOperator should not be null.");

                double min = double.MaxValue;
                double current;

                if (obj is CollectionField)
                {
                    foreach (FieldObject fieldObject in ((CollectionField)obj).Collection)
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        if (current < min)
                            min = current;
                    }
                }
                else if (obj is PathField)
                {
                    foreach (PathStepField fieldObject in ((PathField)obj).Path.Cast<PathStepField>())
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        if (current < min)
                            min = current;
                    }
                }
                else {
                    min = double.TryParse(obj.ToValue, out current) ? current : double.NaN;
                }

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField(min.ToString(CultureInfo.InvariantCulture), JsonDataType.Double));

                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class MeanLocalOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int objectIndex;

        public MeanLocalOperator(GraphViewExecutionOperator inputOp, int objectIndex)
        {
            this.inputOp = inputOp;
            this.objectIndex = objectIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                FieldObject obj = currentRecord[this.objectIndex];
                Debug.Assert(obj != null, "The input of the MeanLocalOperator should not be null.");

                double sum = 0.0;
                long count = 0;
                double current;

                if (obj is CollectionField)
                {
                    foreach (FieldObject fieldObject in ((CollectionField)obj).Collection)
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        sum += current;
                        count++;
                    }
                }
                else if (obj is PathField)
                {
                    foreach (PathStepField fieldObject in ((PathField)obj).Path.Cast<PathStepField>())
                    {
                        if (!double.TryParse(fieldObject.ToValue, out current))
                            throw new GraphViewException("The element of the local object cannot be cast to a number");

                        sum += current;
                        count++;
                    }
                }
                else
                {
                    count = 1;
                    sum = double.TryParse(obj.ToValue, out current) ? current : double.NaN;
                }

                RawRecord result = new RawRecord(currentRecord);
                result.Append(new StringField((sum / count).ToString(CultureInfo.InvariantCulture), JsonDataType.Double));

                return result;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class SimplePathOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int pathIndex;

        public SimplePathOperator(GraphViewExecutionOperator inputOp, int pathIndex)
        {
            this.inputOp = inputOp;
            this.pathIndex = pathIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                RawRecord result = new RawRecord(currentRecord);
                PathField path = currentRecord[pathIndex] as PathField;

                Debug.Assert(path != null, "The input of the simplePath filter should be a PathField generated by path().");

                HashSet<Object> intermediateStepSet = new HashSet<Object>();
                bool isSimplePath = true;

                foreach (PathStepField step in path.Path.Cast<PathStepField>())
                {
                    Object stepObj = step.ToObject();
                    if (intermediateStepSet.Contains(stepObj))
                    {
                        isSimplePath = false;
                        break;
                    }
                        
                    intermediateStepSet.Add(stepObj);
                }

                if (isSimplePath) {
                    return result;
                }
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class CyclicPathOperator : GraphViewExecutionOperator
    {
        private GraphViewExecutionOperator inputOp;
        private int pathIndex;

        public CyclicPathOperator(GraphViewExecutionOperator inputOp, int pathIndex)
        {
            this.inputOp = inputOp;
            this.pathIndex = pathIndex;
            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord;

            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                RawRecord result = new RawRecord(currentRecord);
                PathField path = currentRecord[pathIndex] as PathField;

                Debug.Assert(path != null, "The input of the cyclicPath filter should be a CollectionField generated by path().");

                HashSet<Object> intermediateStepSet = new HashSet<Object>();
                bool isCyclicPath = false;

                foreach (PathStepField step in path.Path.Cast<PathStepField>())
                {
                    Object stepObj = step.ToObject();
                    if (intermediateStepSet.Contains(stepObj))
                    {
                        isCyclicPath = true;
                        break;
                    }

                    intermediateStepSet.Add(stepObj);
                }

                if (isCyclicPath) {
                    return result;
                }
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class ChooseOperator : GraphViewExecutionOperator
    {
        GraphViewExecutionOperator inputOp;

        ScalarFunction targetSubQueryFunc;

        ConstantSourceOperator tempSourceOp;
        ContainerOperator trueBranchSourceOp;
        ContainerOperator falseBranchSourceOp;

        Queue<RawRecord> evaluatedTrueRecords;
        Queue<RawRecord> evaluatedFalseRecords;

        GraphViewExecutionOperator trueBranchTraversalOp;
        GraphViewExecutionOperator falseBranchTraversalOp;

        public ChooseOperator(
            GraphViewExecutionOperator inputOp,
            ScalarFunction targetSubQueryFunc,
            ConstantSourceOperator tempSourceOp,
            ContainerOperator trueBranchSourceOp,
            GraphViewExecutionOperator trueBranchTraversalOp,
            ContainerOperator falseBranchSourceOp,
            GraphViewExecutionOperator falseBranchTraversalOp
            )
        {
            this.inputOp = inputOp;
            this.targetSubQueryFunc = targetSubQueryFunc;
            this.tempSourceOp = tempSourceOp;
            this.trueBranchSourceOp = trueBranchSourceOp;
            this.trueBranchTraversalOp = trueBranchTraversalOp;
            this.falseBranchSourceOp = falseBranchSourceOp;
            this.falseBranchTraversalOp = falseBranchTraversalOp;

            this.evaluatedTrueRecords = new Queue<RawRecord>();
            this.evaluatedFalseRecords = new Queue<RawRecord>();

            Open();
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord = null;
            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                if (this.targetSubQueryFunc.Evaluate(currentRecord) != null)
                    this.evaluatedTrueRecords.Enqueue(currentRecord);
                else
                    this.evaluatedFalseRecords.Enqueue(currentRecord);
            }

            while (this.evaluatedTrueRecords.Any())
            {
                this.tempSourceOp.ConstantSource = this.evaluatedTrueRecords.Dequeue();
                this.trueBranchSourceOp.Next();
            }

            RawRecord trueBranchTraversalRecord;
            while (this.trueBranchTraversalOp.State() && (trueBranchTraversalRecord = this.trueBranchTraversalOp.Next()) != null) {
                return trueBranchTraversalRecord;
            }

            while (this.evaluatedFalseRecords.Any())
            {
                this.tempSourceOp.ConstantSource = this.evaluatedFalseRecords.Dequeue();
                this.falseBranchSourceOp.Next();
            }

            RawRecord falseBranchTraversalRecord;
            while (this.falseBranchTraversalOp.State() && (falseBranchTraversalRecord = this.falseBranchTraversalOp.Next()) != null) {
                return falseBranchTraversalRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.evaluatedTrueRecords.Clear();
            this.evaluatedFalseRecords.Clear();
            this.trueBranchSourceOp.ResetState();
            this.falseBranchSourceOp.ResetState();
            this.trueBranchTraversalOp.ResetState();
            this.falseBranchTraversalOp.ResetState();

            this.Open();
        }
    }

    internal class ChooseWithOptionsOperator : GraphViewExecutionOperator
    {
        GraphViewExecutionOperator inputOp;

        ScalarFunction targetSubQueryFunc;

        ConstantSourceOperator tempSourceOp;
        ContainerOperator optionSourceOp;

        int activeOptionTraversalIndex;
        bool needsOptionSourceInit;
        List<Tuple<ScalarFunction, Queue<RawRecord>, GraphViewExecutionOperator>> traversalList;

        Queue<RawRecord> noneRawRecords;
        GraphViewExecutionOperator optionNoneTraversalOp;
        const int noneBranchIndex = -1;

        public ChooseWithOptionsOperator(
            GraphViewExecutionOperator inputOp,
            ScalarFunction targetSubQueryFunc,
            ConstantSourceOperator tempSourceOp,
            ContainerOperator optionSourceOp
            )
        {
            this.inputOp = inputOp;
            this.targetSubQueryFunc = targetSubQueryFunc;
            this.tempSourceOp = tempSourceOp;
            this.optionSourceOp = optionSourceOp;
            this.activeOptionTraversalIndex = 0;
            this.noneRawRecords = new Queue<RawRecord>();
            this.optionNoneTraversalOp = null;
            this.needsOptionSourceInit = true;
            this.traversalList = new List<Tuple<ScalarFunction, Queue<RawRecord>, GraphViewExecutionOperator>>();

            this.Open();
        }

        public void AddOptionTraversal(ScalarFunction value, GraphViewExecutionOperator optionTraversalOp)
        {
            if (value == null) {
                this.optionNoneTraversalOp = optionTraversalOp;
                return;
            }
                
            this.traversalList.Add(new Tuple<ScalarFunction, Queue<RawRecord>, GraphViewExecutionOperator>(value,
                new Queue<RawRecord>(), optionTraversalOp));
        }

        private void PrepareOptionTraversalSource(int index)
        {
            this.optionSourceOp.ResetState();
            Queue<RawRecord> chosenRecords = index != ChooseWithOptionsOperator.noneBranchIndex 
                                             ? this.traversalList[index].Item2 
                                             : this.noneRawRecords;
            while (chosenRecords.Any())
            {
                this.tempSourceOp.ConstantSource = chosenRecords.Dequeue();
                this.optionSourceOp.Next();
            }
        }

        public override RawRecord Next()
        {
            RawRecord currentRecord = null;
            while (this.inputOp.State() && (currentRecord = this.inputOp.Next()) != null)
            {
                FieldObject evaluatedValue = this.targetSubQueryFunc.Evaluate(currentRecord);
                if (evaluatedValue == null) {
                    throw new GraphViewException("The provided traversal of choose() does not map to a value.");
                }

                bool hasBeenChosen = false;
                foreach (Tuple<ScalarFunction, Queue<RawRecord>, GraphViewExecutionOperator> tuple in this.traversalList)
                {
                    FieldObject rhs = tuple.Item1.Evaluate(null);
                    if (evaluatedValue.Equals(rhs))
                    {
                        tuple.Item2.Enqueue(currentRecord);
                        hasBeenChosen = true;
                        break;
                    }
                }

                if (!hasBeenChosen && this.optionNoneTraversalOp != null) {
                    this.noneRawRecords.Enqueue(currentRecord);
                }
            }

            RawRecord traversalRecord = null;
            while (this.activeOptionTraversalIndex < this.traversalList.Count)
            {
                if (this.needsOptionSourceInit)
                {
                    this.PrepareOptionTraversalSource(this.activeOptionTraversalIndex);
                    this.needsOptionSourceInit = false;
                }

                GraphViewExecutionOperator optionTraversalOp = this.traversalList[this.activeOptionTraversalIndex].Item3;
                
                while (optionTraversalOp.State() && (traversalRecord = optionTraversalOp.Next()) != null) {
                    return traversalRecord;
                }

                this.activeOptionTraversalIndex++;
                this.needsOptionSourceInit = true;
            }

            if (this.optionNoneTraversalOp != null)
            {
                if (this.needsOptionSourceInit)
                {
                    this.PrepareOptionTraversalSource(ChooseWithOptionsOperator.noneBranchIndex);
                    this.needsOptionSourceInit = false;
                }

                while (this.optionNoneTraversalOp.State() && (traversalRecord = this.optionNoneTraversalOp.Next()) != null) {
                    return traversalRecord;
                }
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.optionSourceOp.ResetState();
            this.needsOptionSourceInit = true;
            this.activeOptionTraversalIndex = 0;
            this.noneRawRecords.Clear();
            this.optionNoneTraversalOp?.ResetState();

            foreach (Tuple<ScalarFunction, Queue<RawRecord>, GraphViewExecutionOperator> tuple in this.traversalList)
            {
                tuple.Item2.Clear();
                tuple.Item3.ResetState();
            }

            this.Open();
        }
    }


    internal class CoinOperator : GraphViewExecutionOperator
    {
        private readonly double _probability;
        private readonly GraphViewExecutionOperator _inputOp;
        private readonly Random _random;

        public CoinOperator(
            GraphViewExecutionOperator inputOp,
            double probability)
        {
            this._inputOp = inputOp;
            this._probability = probability;
            this._random = new Random();

            Open();
        }

        public override RawRecord Next()
        {
            RawRecord current = null;
            while (this._inputOp.State() && (current = this._inputOp.Next()) != null) {
                if (this._random.NextDouble() <= this._probability) {
                    return current;
                }
            }

            Close();
            return null;
        }

        public override void ResetState()
        {
            this._inputOp.ResetState();
            Open();
        }
    }

    internal class SampleOperator : GraphViewExecutionOperator
    {
        private readonly GraphViewExecutionOperator inputOp;
        private readonly long amountToSample;
        private readonly ScalarFunction byFunction;  // Can be null if no "by" step
        private readonly Random random;

        private readonly List<RawRecord> inputRecords;
        private readonly List<double> inputProperties;
        private int nextIndex;

        public SampleOperator(
            GraphViewExecutionOperator inputOp,
            long amoutToSample,
            ScalarFunction byFunction)
        {
            this.inputOp = inputOp;
            this.amountToSample = amoutToSample;
            this.byFunction = byFunction;  // Can be null if no "by" step
            this.random = new Random();

            this.inputRecords = new List<RawRecord>();
            this.inputProperties = new List<double>();
            this.nextIndex = 0;
            this.Open();
        }

        public override RawRecord Next()
        {
            if (this.nextIndex == 0) {
                while (this.inputOp.State()) {
                    RawRecord current = this.inputOp.Next();
                    if (current == null) break;

                    this.inputRecords.Add(current);
                    if (this.byFunction != null) {
                        this.inputProperties.Add(double.Parse(this.byFunction.Evaluate(current).ToValue));
                    }
                }
            }

            // Return nothing if sample amount <= 0
            if (this.amountToSample <= 0) {
                this.Close();
                return null;
            }

            // Return all if sample amount > amount of inputs
            if (this.amountToSample >= this.inputRecords.Count) {
                if (this.nextIndex == this.inputRecords.Count) {
                    this.Close();
                    return null;
                }
                return this.inputRecords[this.nextIndex++];
            }

            // Sample!
            if (this.nextIndex < this.amountToSample) {
                
                // TODO: Implement the sampling algorithm!
                return this.inputRecords[this.nextIndex++];
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();

            this.inputRecords.Clear();
            this.inputProperties.Clear();
            this.nextIndex = 0;
            this.Open();
        }
    }

    internal class SampleLocalOperator : GraphViewExecutionOperator
    {
        private readonly GraphViewExecutionOperator inputOp;
        private readonly int inputCollectionIndex;
        private readonly int amountToSample;

        private readonly List<string> populateColumns;
        private readonly Random random;

        internal SampleLocalOperator(
            GraphViewExecutionOperator inputOp,
            int inputCollectionIndex,
            int amountToSample,
            List<string> populateColumns)
        {
            this.inputOp = inputOp;
            this.inputCollectionIndex = inputCollectionIndex;
            this.amountToSample = amountToSample;
            this.populateColumns = populateColumns;
            this.random = new Random();

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord srcRecord = null;

            while (this.inputOp.State() && (srcRecord = this.inputOp.Next()) != null)
            {
                RawRecord newRecord = new RawRecord(srcRecord);

                FieldObject inputObject = srcRecord[this.inputCollectionIndex];
                FieldObject sampledObject;
                if (inputObject is CollectionField)
                {
                    CollectionField inputCollection = (CollectionField)inputObject;
                    CollectionField newCollection;

                    if (inputCollection.Collection.Count <= this.amountToSample) {
                        newCollection = new CollectionField(inputCollection);
                    }
                    else
                    {
                        List<FieldObject> tempCollection = new List<FieldObject>(inputCollection.Collection);
                        newCollection = new CollectionField();

                        while (newCollection.Collection.Count < this.amountToSample) {
                            int pickedIndex = this.random.Next(0, tempCollection.Count);
                            FieldObject pickedObject = tempCollection[pickedIndex];
                            tempCollection.RemoveAt(pickedIndex);
                            newCollection.Collection.Add(pickedObject);
                        }
                    }

                    sampledObject = newCollection;
                }
                else if (inputObject is MapField)
                {
                    MapField inputMap = inputObject as MapField;
                    MapField newMap;

                    if (inputMap.Count <= this.amountToSample) {
                        newMap = new MapField(inputMap);
                    }
                    else
                    {
                        List<EntryField> tempEntrySet = inputMap.EntrySet;
                        newMap = new MapField();
                        while (newMap.Count < this.amountToSample)
                        {
                            int pickedIndex = this.random.Next(0, tempEntrySet.Count);
                            EntryField pickedEntry = tempEntrySet[pickedIndex];
                            tempEntrySet.RemoveAt(pickedIndex);
                            newMap.Add(pickedEntry.Key, pickedEntry.Value);
                        }
                    }

                    sampledObject = newMap;
                }
                else {
                    sampledObject = inputObject;
                }

                RawRecord flatRawRecord = sampledObject.FlatToRawRecord(this.populateColumns);
                newRecord.Append(flatRawRecord);
                return newRecord;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class Decompose1Operator : GraphViewExecutionOperator
    {
        private readonly GraphViewExecutionOperator inputOp;
        private readonly int decomposeTargetIndex;
        private readonly List<string> populateColumns;
        private readonly string tableDefaultColumnName;

        public Decompose1Operator(
            GraphViewExecutionOperator inputOp,
            int decomposeTargetIndex,
            List<string> populateColumns,
            string tableDefaultColumnName)
        {
            this.inputOp = inputOp;
            this.decomposeTargetIndex = decomposeTargetIndex;
            this.populateColumns = populateColumns;
            this.tableDefaultColumnName = tableDefaultColumnName;

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord inputRecord = null;
            while (this.inputOp.State() && (inputRecord = this.inputOp.Next()) != null)
            {
                FieldObject inputObj = inputRecord[this.decomposeTargetIndex];
                Compose1Field compose1Obj = inputRecord[this.decomposeTargetIndex] as Compose1Field;

                RawRecord r = new RawRecord(inputRecord);
                if (compose1Obj != null)
                {
                    foreach (string populateColumn in this.populateColumns) {
                        r.Append(compose1Obj[populateColumn]);
                    }
                }
                else {
                    foreach (string columnName in this.populateColumns) {
                        if (columnName.Equals(this.tableDefaultColumnName)) {
                            r.Append(inputObj);
                        }
                        else {
                            r.Append((FieldObject)null);
                        }
                    }
                }

                return r;
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal class SelectColumnOperator : GraphViewExecutionOperator
    {
        private readonly GraphViewExecutionOperator inputOp;
        private readonly int inputTargetIndex;

        //
        // true, select(keys)
        // false, select(values)
        //
        private readonly bool isSelectKeys;

        public SelectColumnOperator(
            GraphViewExecutionOperator inputOp,
            int inputTargetIndex,
            bool isSelectKeys)
        {
            this.inputOp = inputOp;
            this.inputTargetIndex = inputTargetIndex;
            this.isSelectKeys = isSelectKeys;

            this.Open();
        }

        public override RawRecord Next()
        {
            RawRecord inputRecord = null;
            while (this.inputOp.State() && (inputRecord = this.inputOp.Next()) != null)
            {
                FieldObject selectObj = inputRecord[this.inputTargetIndex];
                RawRecord r = new RawRecord(inputRecord);

                if (selectObj is MapField)
                {
                    MapField inputMap = (MapField)selectObj;
                    List<FieldObject> columns = new List<FieldObject>();

                    foreach (EntryField entry in inputMap) {
                        columns.Add(this.isSelectKeys ? entry.Key : entry.Value);
                    }

                    r.Append(new CollectionField(columns));
                    return r;
                }
                else if (selectObj is EntryField)
                {
                    EntryField inputEntry = (EntryField) selectObj;
                    r.Append(this.isSelectKeys ? inputEntry.Key : inputEntry.Value);
                    return r;
                }
                else if (selectObj is PathField)
                {
                    PathField inputPath = (PathField)selectObj;
                    List<FieldObject> columns = new List<FieldObject>();

                    foreach (PathStepField pathStep in inputPath.Path.Cast<PathStepField>())
                    {
                        if (this.isSelectKeys)
                        {
                            List<FieldObject> labels = new List<FieldObject>();
                            foreach (string label in pathStep.Labels) {
                                labels.Add(new StringField(label));
                            }
                            columns.Add(new CollectionField(labels));
                        } else {
                            columns.Add(pathStep.StepFieldObject);
                        }
                    }

                    r.Append(new CollectionField(columns));
                    return r;
                }
                throw new GraphViewException(string.Format("The provided object does not have acessible {0}.",
                    this.isSelectKeys ? "keys" : "values"));
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }

    internal abstract class SelectBaseOperator : GraphViewExecutionOperator
    {
        protected readonly GraphViewExecutionOperator inputOp;
        protected readonly Dictionary<string, IAggregateFunction> sideEffectStates;
        protected readonly int inputObjectIndex;
        protected readonly int pathIndex;

        protected readonly GraphViewKeywords.Pop pop;
        protected readonly string tableDefaultColumnName;

        protected SelectBaseOperator(
            GraphViewExecutionOperator inputOp,
            Dictionary<string, IAggregateFunction> sideEffectStates,
            int inputObjectIndex,
            int pathIndex,
            GraphViewKeywords.Pop pop,
            string tableDefaultColumnName)
        {
            this.inputOp = inputOp;
            this.sideEffectStates = sideEffectStates;
            this.inputObjectIndex = inputObjectIndex;
            this.pathIndex = pathIndex;

            this.pop = pop;
            this.tableDefaultColumnName = tableDefaultColumnName;
        }

        protected FieldObject GetSelectObject(RawRecord inputRec, string label)
        {
            MapField inputMap = inputRec[this.inputObjectIndex] as MapField;
            PathField path = inputRec[this.pathIndex] as PathField;

            StringField labelStringField = new StringField(label);

            IAggregateFunction globalSideEffectObject;
            FieldObject selectObject = null;

            if (this.sideEffectStates.TryGetValue(label, out globalSideEffectObject))
            {
                Dictionary<string, FieldObject> compositeFieldObject = new Dictionary<string, FieldObject>();
                compositeFieldObject.Add(this.tableDefaultColumnName, globalSideEffectObject.Terminate());
                selectObject = new Compose1Field(compositeFieldObject, this.tableDefaultColumnName);
            }
            else if (inputMap != null && inputMap.ContainsKey(labelStringField)) {
                selectObject = inputMap[labelStringField];
            }
            else
            {
                Debug.Assert(path != null);
                List<FieldObject> selectObjects = new List<FieldObject>();

                if (this.pop == Pop.First) {
                    foreach (PathStepField step in path.Path.Cast<PathStepField>()) {
                        if (step.Labels.Contains(label)) {
                            selectObjects.Add(step.StepFieldObject);
                            break;
                        }
                    }
                }
                else if (this.pop == Pop.Last) {
                    for (int reverseIndex = path.Path.Count - 1; reverseIndex >= 0; reverseIndex--) {
                        PathStepField step = (PathStepField)path.Path[reverseIndex];
                        if (step.Labels.Contains(label)) {
                            selectObjects.Add(step.StepFieldObject);
                            break;
                        }
                    }
                }
                //
                // this.pop == Pop.All
                //
                else {
                    foreach (PathStepField step in path.Path.Cast<PathStepField>()) {
                        if (step.Labels.Contains(label)) {
                            selectObjects.Add(step.StepFieldObject);
                        }
                    }
                }

                if (selectObjects.Count == 1) {
                    selectObject = selectObjects[0];
                }
                else if (selectObjects.Count > 1)
                {
                    Dictionary<string, FieldObject> compositeFieldObject = new Dictionary<string, FieldObject>();
                    compositeFieldObject.Add(this.tableDefaultColumnName, new CollectionField(selectObjects));
                    selectObject = new Compose1Field(compositeFieldObject, this.tableDefaultColumnName);
                }
            }

            return selectObject;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.Open();
        }
    }


    internal class SelectOperator : SelectBaseOperator
    {
        private readonly List<string> selectLabels;
        private readonly List<ScalarFunction> byFuncList;

        public SelectOperator(
            GraphViewExecutionOperator inputOp,
            Dictionary<string, IAggregateFunction> sideEffectStates,
            int inputObjectIndex,
            int pathIndex,
            GraphViewKeywords.Pop pop,
            List<string> selectLabels,
            List<ScalarFunction> byFuncList,
            string tableDefaultColumnName)
            : base(inputOp, sideEffectStates, inputObjectIndex, pathIndex, pop, tableDefaultColumnName)
        {
            this.selectLabels = selectLabels;
            this.byFuncList = byFuncList;

            this.Open();
        }

        private FieldObject GetProjectionResult(FieldObject selectObject, ref int activeByFuncIndex)
        {
            FieldObject projectionResult;

            if (this.byFuncList.Count == 0) {
                projectionResult = selectObject;
            }
            else
            {
                RawRecord initCompose1Record = new RawRecord();
                initCompose1Record.Append(selectObject);
                projectionResult = this.byFuncList[activeByFuncIndex++ % this.byFuncList.Count].Evaluate(initCompose1Record);

                if (projectionResult == null) {
                    throw new GraphViewException("The provided traversal or property name of path() does not map to a value.");
                }
            }

            return projectionResult;
        }

        public override RawRecord Next()
        {
            RawRecord inputRec;
            while (this.inputOp.State() && (inputRec = this.inputOp.Next()) != null)
            {
                int activeByFuncIndex = 0;

                MapField selectMap = new MapField();

                bool allLabelCanBeSelected = true;
                foreach (string label in this.selectLabels)
                {
                    FieldObject selectObject = this.GetSelectObject(inputRec, label);

                    if (selectObject == null)
                    {
                        allLabelCanBeSelected = false;
                        break;
                    }

                    selectMap.Add(new StringField(label), this.GetProjectionResult(selectObject, ref activeByFuncIndex));
                }

                if (!allLabelCanBeSelected) {
                    continue;
                }

                RawRecord r = new RawRecord(inputRec);
                r.Append(selectMap);
                return r;
            }

            this.Close();
            return null;
        }
    }

    internal class SelectOneOperator : SelectBaseOperator
    {
        private readonly string selectLabel;
        private readonly ScalarFunction byFunc;

        private readonly List<string> populateColumns;

        public SelectOneOperator(
            GraphViewExecutionOperator inputOp,
            Dictionary<string, IAggregateFunction> sideEffectStates,
            int inputObjectIndex,
            int pathIndex,
            GraphViewKeywords.Pop pop,
            string selectLabel,
            ScalarFunction byFunc,
            List<string> populateColumns,
            string tableDefaultColumnName)
            : base(inputOp, sideEffectStates, inputObjectIndex, pathIndex, pop, tableDefaultColumnName)
        {
            this.selectLabel = selectLabel;
            this.byFunc = byFunc;
            this.populateColumns = populateColumns;

            this.Open();
        }

        private FieldObject GetProjectionResult(FieldObject selectObject)
        {
            FieldObject projectionResult;

            RawRecord initCompose1Record = new RawRecord();
            initCompose1Record.Append(selectObject);
            projectionResult = this.byFunc.Evaluate(initCompose1Record);

            if (projectionResult == null) {
                throw new GraphViewException("The provided traversal or property name of path() does not map to a value.");
            }

            return projectionResult;
        }

        public override RawRecord Next()
        {
            RawRecord inputRec;
            while (this.inputOp.State() && (inputRec = this.inputOp.Next()) != null)
            {
                FieldObject selectObject = this.GetSelectObject(inputRec, this.selectLabel);

                if (selectObject == null) {
                    continue;
                }

                Compose1Field projectionResult = this.GetProjectionResult(selectObject) as Compose1Field;
                Debug.Assert(projectionResult != null, "projectionResult is Compose1Field.");

                RawRecord r = new RawRecord(inputRec);
                foreach (string columnName in this.populateColumns) {
                    r.Append(projectionResult[columnName]);
                }

                return r;
            }

            this.Close();
            return null;
        }
    }

    internal class AdjacencyListDecoder : GraphViewExecutionOperator
    {
        private readonly GraphViewExecutionOperator inputOp;
        private readonly int startVertexIndex;

        private readonly int adjacencyListIndex;
        private readonly int revAdjacencyListIndex;

        private readonly BooleanFunction edgePredicate;
        private readonly List<string> projectedFields;

        private readonly bool isStartVertexTheOriginVertex;

        private readonly Queue<RawRecord> outputBuffer;
        private readonly GraphViewConnection connection;

        private readonly int batchSize;
        private readonly Queue<Tuple<RawRecord, string>> batchInputSequence;

        private readonly int outputRecordLength;
        private bool hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch;

        public AdjacencyListDecoder(
            GraphViewExecutionOperator inputOp,
            int startVertexIndex, 
            int adjacencyListIndex, int revAdjacencyListIndex,
            bool isStartVertexTheOriginVertex,
            BooleanFunction edgePredicate, List<string> projectedFields,
            GraphViewConnection connection,
            int outputRecordLength,
            int batchSize = 1000)
        {
            this.inputOp = inputOp;
            this.outputBuffer = new Queue<RawRecord>();
            this.startVertexIndex = startVertexIndex;
            this.adjacencyListIndex = adjacencyListIndex;
            this.revAdjacencyListIndex = revAdjacencyListIndex;
            this.isStartVertexTheOriginVertex = isStartVertexTheOriginVertex;
            this.edgePredicate = edgePredicate;
            this.projectedFields = projectedFields;
            this.connection = connection;

            this.batchSize = batchSize;
            this.batchInputSequence = new Queue<Tuple<RawRecord, string>>();

            this.outputRecordLength = outputRecordLength;
            this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch = false;

            this.Open();
        }

        /// <summary>
        /// Fill edge's {_source, _sink, _other, id, *} meta fields
        /// </summary>
        /// <param name="record"></param>
        /// <param name="edge"></param>
        /// <param name="startVertexId"></param>
        /// <param name="isReversedAdjList"></param>
        /// <param name="isStartVertexTheOriginVertex"></param>
        internal static void FillMetaField(RawRecord record, EdgeField edge, 
            string startVertexId, bool isStartVertexTheOriginVertex, bool isReversedAdjList)
        {
            string otherValue;
            if (isStartVertexTheOriginVertex) {
                if (isReversedAdjList) {
                    otherValue = edge[KW_EDGE_SRCV].ToValue;
                }
                else {
                    otherValue = edge[KW_EDGE_SINKV].ToValue;
                }
            }
            else {
                otherValue = startVertexId;
            }

            record.fieldValues[0] = new StringField(edge.OutV);
            record.fieldValues[1] = new StringField(edge.InV);
            record.fieldValues[2] = new StringField(otherValue);
            record.fieldValues[3] = new StringField(edge.EdgeId);
            record.fieldValues[4] = new EdgeField(edge, otherValue);
        }

        /// <summary>
        /// Fill the field of selected edge's properties
        /// </summary>
        /// <param name="record"></param>
        /// <param name="edge"></param>
        /// <param name="projectedFields"></param>
        internal static void FillPropertyField(RawRecord record, EdgeField edge, List<string> projectedFields)
        {
            for (int i = GraphViewReservedProperties.ReservedEdgeProperties.Count; i < projectedFields.Count; i++) {
                record.fieldValues[i] = edge[projectedFields[i]];
            }
        }

        /// <summary>
        /// Decode an adjacency list and return all the edges satisfying the edge predicate
        /// </summary>
        /// <param name="adjacencyList"></param>
        /// <param name="startVertexId"></param>
        /// <param name="isReverse"></param>
        /// <returns></returns>
        private List<RawRecord> DecodeAdjacencyList(AdjacencyListField adjacencyList, string startVertexId, bool isReverse)
        {
            List<RawRecord> edgeRecordCollection = new List<RawRecord>();

            foreach (EdgeField edge in adjacencyList.AllEdges) {
                // Construct new record
                RawRecord edgeRecord = new RawRecord(this.projectedFields.Count);

                AdjacencyListDecoder.FillMetaField(edgeRecord, edge, startVertexId, this.isStartVertexTheOriginVertex, isReverse);
                AdjacencyListDecoder.FillPropertyField(edgeRecord, edge, this.projectedFields);

                if (this.edgePredicate != null && !this.edgePredicate.Evaluate(edgeRecord)) {
                    continue;
                }

                edgeRecordCollection.Add(edgeRecord);
            }

            return edgeRecordCollection;
        }

        /// <summary>
        /// Decode a record's adjacency list or/and reverse adjacency list
        /// and return all the edges satisfying the edge predicate
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private List<RawRecord> Decode(RawRecord record)
        {
            List<RawRecord> results = new List<RawRecord>();
            string startVertexId = record[this.startVertexIndex].ToValue;

            if (this.adjacencyListIndex >= 0)
            {
                AdjacencyListField adj = record[this.adjacencyListIndex] as AdjacencyListField;
                if (adj == null)
                    throw new GraphViewException(
                        $"The FieldObject at {this.adjacencyListIndex} is not a adjacency list but {(record[this.adjacencyListIndex] != null ? record[this.adjacencyListIndex].ToString() : "null")}");

                results.AddRange(this.DecodeAdjacencyList(adj, startVertexId, false));
            }

            if (this.revAdjacencyListIndex >= 0)
            {
                AdjacencyListField revAdj = record[this.revAdjacencyListIndex] as AdjacencyListField;
                if (revAdj == null)
                    throw new GraphViewException(
                        $"The FieldObject at {this.revAdjacencyListIndex} is not a reverse adjacency list but {(record[this.revAdjacencyListIndex] != null ? record[this.revAdjacencyListIndex].ToString() : "null")}");

                results.AddRange(this.DecodeAdjacencyList(revAdj, startVertexId, true));
            }

            return results;
        }

        /// <summary>
        /// Cross apply the adjacency list or/and reverse adjacency list of the record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private List<RawRecord> CrossApply(RawRecord record)
        {
            List<RawRecord> results = new List<RawRecord>();

            foreach (RawRecord edgeRecord in Decode(record)) {
                RawRecord r = new RawRecord(record);
                r.Append(edgeRecord);

                results.Add(r);
            }

            return results;
        }

        /// <summary>
        /// Send one query to construct all the spilled adjacency lists of vertice in the inputSequence 
        /// </summary>
        private void ConstructSpilledAdjListsOrVirtualRevAdjListsInBatch()
        {
            HashSet<string> vertexIdCollection = new HashSet<string>();
            foreach (Tuple<RawRecord, string> tuple in batchInputSequence)
            {
                string vertexId = tuple.Item2;
                VertexField vertexField;
                this.connection.VertexCache.TryGetVertexField(vertexId, out vertexField);
                if (vertexField != null)
                {
                    AdjacencyListField adj = vertexField[GraphViewKeywords.KW_VERTEX_EDGE] as AdjacencyListField;
                    AdjacencyListField revAdj = vertexField[GraphViewKeywords.KW_VERTEX_REV_EDGE] as AdjacencyListField;
                    Debug.Assert(adj != null, "adj != null");
                    Debug.Assert(revAdj != null, "revAdj != null");
                    if (adj.HasBeenFetched && revAdj.HasBeenFetched) {
                        continue;
                    }
                }
                vertexIdCollection.Add(tuple.Item2);
            }

            EdgeDocumentHelper.ConstructSpilledAdjListsOrVirtualRevAdjListsOfVertices(connection, vertexIdCollection);
        }

        public override RawRecord Next()
        {
            if (this.outputBuffer.Count > 0) {
                return outputBuffer.Dequeue();
            }

            while (this.batchInputSequence.Count >= batchSize
                || this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch
                || (this.batchInputSequence.Count != 0 && !this.inputOp.State()))
            {
                if (!this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch) {
                    this.ConstructSpilledAdjListsOrVirtualRevAdjListsInBatch();
                }

                Tuple<RawRecord, string> batchVertex = this.batchInputSequence.Dequeue();

                this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch = this.batchInputSequence.Count > 0;

                RawRecord currentRecord = batchVertex.Item1;

                foreach (RawRecord record in this.CrossApply(currentRecord)) {
                    this.outputBuffer.Enqueue(record);
                }

                if (this.outputBuffer.Count > 0) {
                    return this.outputBuffer.Dequeue();
                }
            }

            while (this.inputOp.State())
            {
                RawRecord currentRecord = this.inputOp.Next();

                if (currentRecord == null) {
                    break;
                }

                Debug.Assert(currentRecord.Length <= this.outputRecordLength, "currentRecord.Length <= this.outputRecordLength");
                bool hasBeenCrossAppliedOnServer = currentRecord.Length == this.outputRecordLength;

                if (hasBeenCrossAppliedOnServer) {
                    return currentRecord;
                }

                if (this.adjacencyListIndex >= 0)
                {
                    AdjacencyListField adj = currentRecord[this.adjacencyListIndex] as AdjacencyListField;
                    Debug.Assert(adj != null, "adj != null");
                    if (!adj.HasBeenFetched) {
                        this.batchInputSequence.Enqueue(new Tuple<RawRecord, string>(currentRecord, currentRecord[this.startVertexIndex].ToValue));
                        continue;
                    }
                }
                else if (this.revAdjacencyListIndex >= 0)
                {
                    AdjacencyListField revAdj = currentRecord[this.revAdjacencyListIndex] as AdjacencyListField;
                    Debug.Assert(revAdj != null, "revAdj != null");
                    if (!revAdj.HasBeenFetched) {
                        this.batchInputSequence.Enqueue(new Tuple<RawRecord, string>(currentRecord, currentRecord[this.startVertexIndex].ToValue));
                        continue;
                    }
                }

                foreach (RawRecord record in this.CrossApply(currentRecord)) {
                    this.outputBuffer.Enqueue(record);
                }

                if (this.outputBuffer.Count > 0) {
                    return this.outputBuffer.Dequeue();
                }
            }

            while (this.batchInputSequence.Count >= batchSize
                 || this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch
                 || (this.batchInputSequence.Count != 0 && !this.inputOp.State()))
            {
                if (!this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch) {
                    this.ConstructSpilledAdjListsOrVirtualRevAdjListsInBatch();
                }

                Tuple<RawRecord, string> batchVertex = this.batchInputSequence.Dequeue();

                this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch = this.batchInputSequence.Count > 0;

                RawRecord currentRecord = batchVertex.Item1;

                foreach (RawRecord record in this.CrossApply(currentRecord)) {
                    this.outputBuffer.Enqueue(record);
                }

                if (this.outputBuffer.Count > 0) {
                    return this.outputBuffer.Dequeue();
                }
            }

            this.Close();
            return null;
        }

        public override void ResetState()
        {
            this.inputOp.ResetState();
            this.outputBuffer.Clear();
            this.batchInputSequence.Clear();
            this.hasConstructedSpilledAdjListsOrVirtualRevAdjListsForCurrentBatch = false;
            this.Open();
        }
    }
}
