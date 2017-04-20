using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static GraphView.GraphViewKeywords;


namespace GraphView
{
    internal abstract class BulkOperation
    {
        public JObject OperationJObject { get; } = new JObject();

        public Dictionary<string, string> KnownEtags { get; } = new Dictionary<string, string>();

        public GraphViewConnection Connection { get; }


        protected BulkOperation(GraphViewConnection connection, string op)
        {
            this.Connection = connection;
            this.OperationJObject["op"] = op;
        }

        public abstract void Callback(BulkResponse response, JObject content);
    }


    internal class BulkOperationAddVertex : BulkOperation
    {
        /* Parameter schema: 
            {
                "op": "AddVertex",
                "vertexObject": { ... }
            }
           Response content:
            { }
        */

        private readonly JObject _vertexObject;

        public BulkOperationAddVertex(GraphViewConnection connection, JObject vertexObject)
            : base(connection, "AddVertex")
        {
            this._vertexObject = vertexObject;

            // OperationJObject
            this.OperationJObject["vertexObject"] = vertexObject;

            // KnownEtags:
            // Does need add anything
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            this._vertexObject[KW_DOC_ETAG] = response.Etags[(string)this._vertexObject[KW_DOC_ID]];
            Debug.Assert(this._vertexObject[KW_DOC_ETAG] != null);
        }
    }

    /// <summary>
    /// This bulk operation will only add one side - add either incoming edge or outgoing edge
    /// If use reverse edges, BulkOperationAddEdge must be called twice!
    /// </summary>
    internal class BulkOperationAddEdge : BulkOperation
    {
        public string FirstSpillEdgeDocId { get; private set; }
        public string NewEdgeDocId { get; private set; }


        private readonly string _vertexId;
        private readonly bool _isReverse;
        private readonly VertexField _thisVertexField;


        public BulkOperationAddEdge(
            GraphViewConnection connection,
            VertexField srcVertexField,
            VertexField sinkVertexField,
            bool isReverse, 
            int? spillThreshold,
            JObject edgeObject)
            : base(connection, "AddEdge")
        {
            /* Parameter schema: 
                {
                    "op": "AddEdge",
                    "srcV": ...,
                    "sinkV": ...,
                    "isReverse", true/false
                    "spillThreshold", null/0/>0     // Can be null
                    "edgeObject", {
                        "id": ...,
                        "label": ...,
                        "_srcV"/"_sinkV": "...",
                        "_srcVLabel"/"_sinkVLabel": "...",
                        ... (Other properties)
                    }
                }
               Response content:
                {
                    "firstSpillEdgeDocId": "..."    // Not null when spilling the _edge/_reverse_edge the first time
                    "newEdgeDocId": "..."           // Which document is this edge added to? (Can be null)
                }
            */
            this._thisVertexField = (isReverse ? sinkVertexField : srcVertexField);
            this._vertexId = this._thisVertexField.VertexId;
            this._isReverse = isReverse;

#if DEBUG
            if (isReverse)
            {
                Debug.Assert((string)edgeObject[KW_EDGE_SRCV] == srcVertexField.VertexId);
                Debug.Assert((string)edgeObject[KW_EDGE_SRCV_LABEL] == srcVertexField.VertexLabel);
            }
            else
            {
                Debug.Assert((string)edgeObject[KW_EDGE_SINKV] == sinkVertexField.VertexId);
                Debug.Assert((string)edgeObject[KW_EDGE_SINKV_LABEL] == sinkVertexField.VertexLabel);
            }
            Debug.Assert(!string.IsNullOrEmpty((string)edgeObject[KW_DOC_ID]));
            Debug.Assert(edgeObject[KW_EDGE_LABEL] != null);
#endif

            // Prepare etag dictionary
            string latestEdgeDocId = isReverse
                ? this._thisVertexField.LatestInEdgeDocumentId
                : this._thisVertexField.LatestOutEdgeDocumentId;
            this.KnownEtags[this._vertexId] = connection.VertexCache.GetCurrentEtag(this._vertexId);
            if (latestEdgeDocId != null)
            {
                // etag of latest edge document may not exist: since this document might have not been accessed before
                string etag = connection.VertexCache.TryGetCurrentEtag(latestEdgeDocId);
                this.KnownEtags[latestEdgeDocId] = etag;  // Can be null
            }

            this.OperationJObject["srcV"] = srcVertexField.VertexId;
            this.OperationJObject["sinkV"] = sinkVertexField.VertexId;
            this.OperationJObject["isReverse"] = isReverse;
            this.OperationJObject["spillThreshold"] = spillThreshold;
            this.OperationJObject["edgeObject"] = edgeObject;
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            this.FirstSpillEdgeDocId = (string)content["firstSpillEdgeDocId"];
            this.NewEdgeDocId = (string)content["newEdgeDocId"];


            //
            // Actually, there might be at most three documents whose etag are upserted
            //  - The vertex document: when nonspilled->nonspilled or nonspilled->spilled or spilled-but-create-new-edge-document
            //  - The edge document (firstSpillEdgeDocId): when nonspilled->spilled, the existing edges are spilled into this document
            //  - The edge document (newEdgeDocId): when spilled, the new edge is always stored here
            //

            //
            // Update vertex JObject's etag (if necessary)
            //
            if (response.Etags.ContainsKey(this._vertexId))
            {
                // The vertex is updated, either because it is changed from non-spilled to spilled, or
                // because its latest spilled edge document is updated
                Debug.Assert(response.Etags[this._vertexId] != null);
            }

            //
            // Update vertex edgeContainer's content (if necessary)
            //
            string latestEdgeDocId = this._isReverse
                ? this._thisVertexField.LatestInEdgeDocumentId
                : this._thisVertexField.LatestOutEdgeDocumentId;
            if (latestEdgeDocId != null)
            {
                // The edges are originally spilled (now it is still spilled)
                Debug.Assert(this.FirstSpillEdgeDocId == null);

                if (this.NewEdgeDocId == latestEdgeDocId)
                {
                    // Now the newly added edge is added to the latest edge document (not too large)
                    // Do nothing
                    // The vertex object should not be updated (etag not changed)
                    Debug.Assert(response.Etags[this._vertexId] == this.Connection.VertexCache.GetCurrentEtag(this._vertexId));
                }
                else
                {
                    // Now the newly added edge is stored in a new edge document
                    // The original latest edge document is too small to store the new edge
                    // Update the vertex object's latest edge document id
                    Debug.Assert(response.Etags.ContainsKey(this._vertexId));
                    Debug.Assert(response.Etags[this._vertexId] != this.Connection.VertexCache.GetCurrentEtag(this._vertexId));

                    if (this._isReverse) {
                        this._thisVertexField.LatestInEdgeDocumentId = this.NewEdgeDocId;
                    }
                    else {
                        this._thisVertexField.LatestOutEdgeDocumentId = this.NewEdgeDocId;
                    }
                }
            }
            else
            {
                // The vertex's edges are originally not spilled
                Debug.Assert(response.Etags.ContainsKey(this._vertexId));

                if (this.FirstSpillEdgeDocId != null)
                {
                    // Now the vertex is changed from not-spilled to spilled
                    Debug.Assert(this.NewEdgeDocId != null);
                    if (this._isReverse)
                    {
                        this._thisVertexField.LatestInEdgeDocumentId = this.NewEdgeDocId;
                    }
                    else
                    {
                        this._thisVertexField.LatestOutEdgeDocumentId = this.NewEdgeDocId;
                    }
                }
                else
                {
                    // Now the vertex is still not spilled
                    Debug.Assert(this.NewEdgeDocId == null);
                }
            }

        }
    }
    
    internal class BulkOperationDropVertexProperty : BulkOperation
    {
        public bool Found { get; private set; }

        public BulkOperationDropVertexProperty(GraphViewConnection connection, string vertexId, string propertyName)
            : base(connection, "DropVertexProperty")
        {
            /* Parameter schema: 
                {
                    "op": "DropVertexProperty",
                    "vertexId": ...,
                    "propertyName": ...,
                }
               Response content:
                {
                    "found": true/false
                }
            */

            this.OperationJObject["vertexId"] = vertexId;
            this.OperationJObject["propertyName"] = propertyName;

            this.KnownEtags[vertexId] = connection.VertexCache.GetCurrentEtag(vertexId);  // Not null
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            this.Found = (bool)content["found"];
        }
    }
    
    internal class BulkOperationDropVertexSingleProperty : BulkOperation
    {
        public bool Found { get; private set; }

        public BulkOperationDropVertexSingleProperty(GraphViewConnection connection, string vertexId, string propertyName, string singlePropertyId)
            : base(connection, "DropVertexSingleProperty")
        {
            /* Parameter schema: 
                {
                    "op": "DropVertexProperty",
                    "vertexId": ...,
                    "propertyName": ...,
                    "singlePropertyId": ...,
                }
               Response content:
                {
                    "found": true/false
                }
            */

            this.OperationJObject["vertexId"] = vertexId;
            this.OperationJObject["propertyName"] = propertyName;
            this.OperationJObject["singlePropertyId"] = singlePropertyId;

            this.KnownEtags[vertexId] = connection.VertexCache.GetCurrentEtag(vertexId);  // Not null
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            this.Found = (bool)content["found"];
        }
    }

    internal class BulkOperationDropVertexSinglePropertyMetaProperty : BulkOperation
    {
        public bool Found { get; private set; }

        public BulkOperationDropVertexSinglePropertyMetaProperty(GraphViewConnection connection, string vertexId, string propertyName, string singlePropertyId, string metaName)
            : base(connection, "DropVertexSinglePropertyMetaProperty")
        {
            /* Parameter schema: 
                {
                    "op": "DropVertexProperty",
                    "vertexId": ...,
                    "propertyName": ...,
                    "singlePropertyId": ...,
                    "metaName": ...,
                }
               Response content:
                {
                    "found": true/false
                }
            */

            this.OperationJObject["vertexId"] = vertexId;
            this.OperationJObject["propertyName"] = propertyName;
            this.OperationJObject["singlePropertyId"] = singlePropertyId;
            this.OperationJObject["metaName"] = metaName;

            this.KnownEtags[vertexId] = connection.VertexCache.GetCurrentEtag(vertexId);  // Not null
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            this.Found = (bool)content["found"];
        }
    }

    /// <summary>
    /// This bulk operation will only drop one side - drop the property of either incoming edge or outgoing edge
    /// If use reverse edges, BulkOperationDropEdgeProperty must be called twice!
    /// </summary>
    internal class BulkOperationDropEdgeProperty : BulkOperation
    {
        public BulkOperationDropEdgeProperty(
            GraphViewConnection connection,
            string srcOrSinkVertexId, string edgeId, string edgeDocId,
            bool isReverse, string[] dropProperties) 
            : base(connection, "DropEdgeProperty")
        {
            /* Parameter schema: 
                {
                    "op": "DropEdgeProperty",
                    "srcOrSinkVertexId": ...,
                    "edgeId": ...,
                    "edgeDocId": ...,
                    "isReverse": true/false,
                    "dropProperties": [
                        "prop1", "prop2", "prop3", ...
                    ]
                }
               Response content:
                {
                    "found": true/false
                    "oppoSideVId": ...
                }
            */

            this.OperationJObject["srcOrSinkVertexId"] = srcOrSinkVertexId;
            this.OperationJObject["edgeId"] = edgeId;
            this.OperationJObject["edgeDocId"] = edgeDocId;
            this.OperationJObject["isReverse"] = isReverse;
            this.OperationJObject["dropProperties"] = new JArray(new HashSet<string>(dropProperties).Cast<object>().ToArray());


            this.KnownEtags[srcOrSinkVertexId] = connection.VertexCache.GetCurrentEtag(srcOrSinkVertexId);
            if (edgeDocId != null)
            {
                this.KnownEtags[edgeDocId] = connection.VertexCache.TryGetCurrentEtag(edgeDocId);
            }
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            Debug.Assert((bool)content["found"] == true);
        }
    }

    /// <summary>
    /// This bulk operation will only drop one side - drop either incoming edge or outgoing edge
    /// If use reverse edges, BulkOperationDropEdge must be called twice!
    /// </summary>
    internal class BulkOperationDropEdge : BulkOperation
    {
        public string OppoSideVertexId { get; private set; }


        public BulkOperationDropEdge(
            GraphViewConnection connection,
            string srcOrSinkVertexId,
            string edgeId, string edgeDocId /*Can be null*/, 
            bool isReverse)
            : base(connection, "DropEdge")
        {
            /* Parameter schema: 
                {
                    "op": "DropEdge",
                    "srcOrSinkVertexId": ...,
                    "edgeId": ...,
                    "edgeDocId": ...,
                    "isReverse": true/false
                }
               Response content:
                {
                    "found": true/false
                    "oppoSideVId": ...
                }
            */

            this.OperationJObject["srcOrSinkVertexId"] = srcOrSinkVertexId;
            this.OperationJObject["edgeId"] = edgeId;
            this.OperationJObject["edgeDocId"] = edgeDocId;
            this.OperationJObject["isReverse"] = isReverse;


            this.KnownEtags[srcOrSinkVertexId] = connection.VertexCache.GetCurrentEtag(srcOrSinkVertexId);
            if (edgeDocId != null) {
                this.KnownEtags[edgeDocId] = connection.VertexCache.TryGetCurrentEtag(edgeDocId);
            }
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            Debug.Assert((bool)content["found"] == true);

            this.OppoSideVertexId = (string)content["oppoSideVId"];
        }
    }


    /// <summary>
    /// This bulk operation will only udpate one side - drop the property of either incoming edge or outgoing edge
    /// If use reverse edges, BulkOperationUpdateEdgeProperty must be called twice!
    /// </summary>
    internal class BulkOperationUpdateEdgeProperty : BulkOperation
    {
        public string OriginallSpilled_NewEdgeDocId { get; private set; }  // Can be null

        public bool? OriginallNotSpilled_DidSpill { get; private set; }  // Can be null
        public string OriginallNotSpilled_VertexFirstEdgeDocId { get; private set; }  // Can be null
        public string OriginallNotSpilled_VertexLatestEdgeDocId { get; private set; }  // Can be null



        public BulkOperationUpdateEdgeProperty(
            GraphViewConnection connection,
            string srcOrSinkVertexId, string edgeId, string edgeDocId,
            bool isReverse, int? spillThreshold, JObject updateProperties)
            : base(connection, "UpdateEdgeProperty")
        {
            /* Parameter schema: 
                {
                    "op": "UpdateEdgeProperty",
                    "srcOrSinkVertexId": ...,
                    "edgeId": ...,
                    "edgeDocId": ...,
                    "isReverse": true/false,
                    "spillThreshold": null/0/>0,
                    "updateProperties": {
                        "prop1": "v1", 
                        "prop2": "v2", 
                        ...
                    }
                }
               Response content:
                {
                    "found": true/false

                    // If the edge is originally spilled
                    "newEdgeDocId": ...,    // To which edge-document is this edge stored (Can't be null if spilled)
                                            // NOTE: If this edge is spilled to a new new edge-document, it must be the vertex's latest edge-document

                    // Or... if the edge is originally not spilled
                    "didSpill": true/false
                    "vertexFirstEdgeDocId": ...,  // Can be null
                    "vertexLatestEdgeDocId": ...,  // Can be null                }
            */

            this.OperationJObject["srcOrSinkVertexId"] = srcOrSinkVertexId;
            this.OperationJObject["edgeId"] = edgeId;
            this.OperationJObject["edgeDocId"] = edgeDocId;
            this.OperationJObject["isReverse"] = isReverse;
            this.OperationJObject["spillThreshold"] = spillThreshold;
            this.OperationJObject["updateProperties"] = updateProperties;

            this.KnownEtags[srcOrSinkVertexId] = connection.VertexCache.GetCurrentEtag(srcOrSinkVertexId);
            if (edgeDocId != null)
            {
                this.KnownEtags[edgeDocId] = connection.VertexCache.TryGetCurrentEtag(edgeDocId);
            }
        }

        public override void Callback(BulkResponse response, JObject content)
        {
            Debug.Assert((bool)content["found"] == true);

            if ((string)this.OperationJObject["edgeDocId"] != null) {
                this.OriginallSpilled_NewEdgeDocId = (string)content["newEdgeDocId"];
                Debug.Assert(!string.IsNullOrEmpty(this.OriginallSpilled_NewEdgeDocId));
            }
            else {
                this.OriginallNotSpilled_DidSpill = (bool?)content["didSpill"];
                this.OriginallNotSpilled_VertexFirstEdgeDocId = (string)content["vertexFirstEdgeDocId"];
                this.OriginallNotSpilled_VertexLatestEdgeDocId = (string)content["vertexLatestEdgeDocId"];

                Debug.Assert(this.OriginallNotSpilled_DidSpill != null);
                if (this.OriginallNotSpilled_DidSpill.Value) {
                    Debug.Assert(this.OriginallNotSpilled_VertexFirstEdgeDocId != null);
                    Debug.Assert(this.OriginallNotSpilled_VertexLatestEdgeDocId != null);
                }
            }
        }
    }




    internal enum BulkStatus : int
    {
        Success = 0,

        DBError = -1,
        NotAccepted = -2, // usually timeout
        AssertionFailed = -3,
        InternalError = -4,
    }


    internal class BulkResponse
    {
        [JsonProperty("Status", Required = Required.Always)]
        public BulkStatus Status { get; private set; }

        [JsonProperty("Message", Required = Required.Always)]
        public string Message { get; private set; }

        [JsonProperty("Debug", Required = Required.Always)]
        public string Debug { get; private set; }

        [JsonProperty("DocDBErrorCode", Required = Required.AllowNull)]
        public HttpStatusCode? DocDBErrorCode { get; private set; }

        [JsonProperty("DocDBErrorMessage", Required = Required.AllowNull)]
        public string DocDBErrorMessage { get; private set; }

        [JsonProperty("Content", Required = Required.Always)]
        public JArray Content { get; private set; }

        [JsonProperty("Etags", Required = Required.Always)]
        public Dictionary<string, string> Etags { get; private set; }

        [JsonConstructor]
        private BulkResponse() { }
    }
    

    internal class Bulk
    {
        public GraphViewConnection Connection { get; }

        public Bulk(GraphViewConnection connection)
        {
            this.Connection = connection;
        }



        public void BulkCall(params BulkOperation[] operations)
        {
            JArray opArray = new JArray();
            Dictionary<string, string> knownEtags = new Dictionary<string, string>();

            // Prepare opArray
            foreach (BulkOperation operation in operations) {
                opArray.Add(operation.OperationJObject);
                foreach (KeyValuePair<string, string> pair in operation.KnownEtags) {
                    string docId = pair.Key;
                    string etag = pair.Value;
#if DEBUG
                    if (knownEtags.ContainsKey(docId)) {
                        Debug.Assert(knownEtags[docId] == etag);
                    }
#endif
                    knownEtags[docId] = etag;
                }
            }

            // Do the call!
            string responseBody = this.Connection.ExecuteBulkOperation(opArray, knownEtags);
            BulkResponse response = JsonConvert.DeserializeObject<BulkResponse>(responseBody);
            if (response.Status != BulkStatus.Success)
            {
                throw new Exception($"BulkCall failed: {response.Message}");
            }

            // Invoke the callbacks
            for (int index = 0; index < operations.Length; ++index) {
                JToken content = response.Content[index];
                if ((content as JValue)?.Type == JTokenType.Null) {
                    operations[index].Callback(response, null);
                }
                else {
                    Debug.Assert(content is JObject);
                    operations[index].Callback(response, (JObject)content);
                }
            }


            //
            // Update etags in cache
            // NOTE: This MUST be done after all callbacks are invoked. 
            //       That is, to ensure the etags are not changed in the callbacks
            //
            foreach (KeyValuePair<string, string> pair in response.Etags)
            {
                string docId = pair.Key;
                string etag = pair.Value;
                if (etag != null)
                {
                    this.Connection.VertexCache.UpdateCurrentEtag(docId, etag);
                }
                else
                {
                    // If (etag == null), it means the etag is unknown
                    // Or this document is deleted by operation
                    this.Connection.VertexCache.TryRemoveEtag(docId);
                }
            }
        }

    }
}
