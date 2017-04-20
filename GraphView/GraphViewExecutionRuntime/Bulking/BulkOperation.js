
function BulkOperation(opArray, etagsObject) {

    "use strict";

    //================================================================================================
    //------------------------------------------------------------------------------------------------
    // [1] 
    // Some JavaScript-dependent functions (for convenience)
    //------------------------------------------------------------------------------------------------
    //================================================================================================

    //
    // NOTE: This is VERY TRICKY!! 
    //   According to the Javascript standard, an exception thrown in promise.catch() is NOT actually 
    // thrown to the global scope, thus DocDB doesn't see the error & abort the transaction.
    //
    // Test code:
    //
    //    new Promise((resolve, reject) => {
    //        reject(new Error());
    //        // Or: throw new Error();
    //    }).catch((error) => {
    //        getContext().getResponse().setBody("A-catch()");
    //        throw error;
    //        getContext().getResponse().setBody("B-catch()");
    //    });
    //    return;
    //
    //   Uncomment the code above. The client will successful execute the stored procedure, with response
    // body set to "A-catch()". This means catch() clause was hit, and `throw error;` did abort the 
    // execution flow. However, the error was not thrown to the outside scope, which would have made the 
    // transaction abort.
    //   I haven't found any way to make an exception spread to the outside scope, which means an exception
    // inside a promise can NEVER be thrown, and NEVER be seen to DocDB.
    //   I have to hack the behavior of `Promise.catch` by overwrite the function in its prototype. I don't
    // need the feature of catching an exception here, so when an exception happens, just call ERROR() to 
    // about the transaction. Note that ERROR() could be called many times when in nested promises.
    //
    /*
    Promise.prototype.chain = function(callback) {
        let thisPromise = this;

        let promise = new Promise(
            function(resolve, reject) {
                thisPromise.then(function(data) {
                    callback(resolve, reject, data);
                }).catch(function(error) {
                    reject(error);
                });
            }
        );
        return promise;
    };
    */

    //================================================================================================
    //------------------------------------------------------------------------------------------------
    // [2] 
    // Some global functions (operation-independent)
    //  - Constant
    //  - Prepare collection, request, response
    //  - Response status & error handling & assertion support
    // NOTE: All functions' names in this region are UPCASED
    //------------------------------------------------------------------------------------------------
    //================================================================================================


    //
    // Prepare collection, request, response
    //
    // Access all database operations - CRUD, query against documents in the current collection
    const Collection = getContext().getCollection();
    const CollectionLink = Collection.getSelfLink();
    // Access HTTP request body and headers for the procedure
    const Request = getContext().getRequest();
    // Access HTTP response body and headers from the procedure
    const Response = getContext().getResponse();

    //
    // Keywords
    //
    // ReSharper disable InconsistentNaming
    const KW_DOC_ID = "id";
    const KW_DOC_ETAG = "_etag";
    const KW_DOC_PARTITION = "_partition";

    const KW_VERTEX_LABEL = "label";
    const KW_VERTEX_EDGE = "_edge";
    const KW_VERTEX_REV_EDGE = "_reverse_edge";
    const KW_VERTEX_EDGE_SPILLED = "_edgeSpilled";
    const KW_VERTEX_REVEDGE_SPILLED = "_revEdgeSpilled";

    const KW_EDGE_LABEL = "label";
    const KW_EDGE_SRCV = "_srcV";
    const KW_EDGE_SRCV_LABEL = "_srcVLabel";
    const KW_EDGE_SINKV = "_sinkV";
    const KW_EDGE_SINKV_LABEL = "_sinkVLabel";

    const KW_EDGEDOC_VERTEXID = "_vertex_id";
    const KW_EDGEDOC_ISREVERSE = "_is_reverse";
    const KW_EDGEDOC_EDGE = KW_VERTEX_EDGE;
    // ReSharper restore InconsistentNaming

    //
    // Other constants
    //
    const Const = {
        DUMMY: null
    };
    Object.freeze(Const);


    //
    // Self-used status codes
    //
    const Status = { // REQUIRES: errCode < 0
        Success: 0,

        DBError: -1,
        NotAccepted: -2, // usually timeout
        AssertionFailed: -3,
        InternalError: -4
    };
    Object.freeze(Status);

    //
    // The response object writen to response body
    //
    const RespObject = {
        Status: Status.Success,
        Message: "",
        Debug: "",
        DocDBErrorCode: 0,
        DocDBErrorMessage: null,
        Content: new Array(opArray.length),
        Etags: new Object
    };
    Object.preventExtensions(RespObject);

    /**
     * Output a debug message to the "Debug" field of response object
     * 
     * @param {} message - Anything
     * @returns {} 
     */
    function DEBUG(message) {
        if (typeof message === "undefined" || message === null) {
            return;
        }
        else if (typeof message === "object") {
            RespObject.Debug += `${JSON.stringify(message)}\r\n`;
        }
        else {
            RespObject.Debug += `${message.toString()}\r\n`;
        }
    }

    /**
     * Abort the procedure (and throw Error) and report the error
     * 
     * @param {string|object} e - docDBErrorObject or `Status`
     * @param {string|object|undefined} message - anything, the message
     * @returns {} - will not return!
     * @example
     *      ERROR(docDBErrorObject);
     *      ERROR(docDBErrorObject, myMessage);
     *      ERROR(myErrorStatus, myMessage);
     */
    function ERROR(e, message) {

        ASSERT(!(e instanceof Error));

        // RespObject.Status, RespObject.DocDBErrorXxx
        if (typeof (e) === "number") {
            ASSERT(e !== Status.Success, "[ERROR] Should not pass Status.Success to ERROR() function.");
            RespObject.Status = e;
            RespObject.DocDBErrorCode = 0;
            RespObject.DocDBErrorMessage = null;
        }
        else {
            RespObject.Status = Status.DBError;
            RespObject.DocDBErrorCode = e["number"];
            RespObject.DocDBErrorMessage = e["body"];
        }

        // RespObject.Message
        if (message === undefined || message === null) {
            RespObject.Message = JSON.stringify(e);
        }
        else if (typeof (message) === "object") {
            RespObject.Message = JSON.stringify(message);
        }
        else {
            RespObject.Message = message.toString();
        }

        // RespObject.Content, RespObject.Etags
        RespObject.Content = new Array(opArray.length);
        RespObject.Etags = { };


        Response.setBody(JSON.stringify(RespObject));
        throw new Error(JSON.stringify(RespObject));
    }


    /**
     * Inform one operation is successful, and set the response content
     * 
     * @param {} index - The operation's index
     * @param {} content - An object (operation-dependent) indicating response's return value
     * @returns {} 
     */
    function SUCCESS(index, content) {
        ASSERT(RespObject.Content.length === opArray.length - 1);  // except the last dummy one
        ASSERT(index >= 0 && index < opArray.length);

        if (content === undefined || content === null) {
            RespObject.Content[index] = new Object();
        }
        else {
            ASSERT(typeof content === "object");
            RespObject.Content[index] = content;
        }
    }

    /**
     * Called when all operations are successfully finished.
     * This function sets the response body.
     * 
     * @returns {} 
     */
    function DONE() {
        RespObject.Status = Status.Success;
        RespObject.Message = "OK";
        RespObject.DocDBErrorCode = null;
        RespObject.DocDBErrorMessage = null;

        Response.setBody(JSON.stringify(RespObject));
    }

    /**
     * Make an assertion.
     * 
     * @param {} condition 
     * @param {} messageOnFail 
     * @returns {}
     */
    function ASSERT(condition, messageOnFail) {
        if (!condition) {
            if (typeof messageOnFail === "string" && messageOnFail) {
                ERROR(Status.AssertionFailed, new Error(messageOnFail).stack.toString());
            }
            else {
                ERROR(Status.AssertionFailed, new Error().stack.toString());
            }
        }
    }


    //================================================================================================
    //------------------------------------------------------------------------------------------------
    // [3]
    // Async document operations (callback form)
    //  - ETAG functions: GET/UPDATE
    //  - Document functions: create, delete, retrieve, replace
    //------------------------------------------------------------------------------------------------
    //================================================================================================

    //
    // ETAG support functions: GET & UPDATE
    //
    function GetEtag(documentId) {
        ASSERT(typeof documentId === "string" && documentId);

        let etag = RespObject.Etags[documentId];
        ASSERT(etag !== undefined); // This etag must exist! (although can be null)
        if (etag === null) {
            return null;
        }
        else {
            ASSERT(typeof etag === "string");
            return etag;
        }
    }

    function UpdateEtag(documentId, documentEtag) {
        ASSERT(typeof documentId === "string" && documentId, documentId);
        ASSERT(documentEtag == null || (typeof documentEtag === "string" && documentEtag));
        //DEBUG("Update Etag: '" + documentId + "' = '" + documentEtag + "'");
        RespObject.Etags[documentId] = documentEtag;
    }

    
    /**
     * Try to replace an existing document with a new one.
     * 
     * @param {Object} document - The new document object
     * @param {function(boolean, object)} replaceCallback - The callback indicating whether the document is too large! 
     *          The parameter is `true` if the replacement failed because the new document is too large.
     *          Callback(tooLarge, newDocument?)
     * @returns {} 
     */
    function TryReplaceDocument(document, replaceCallback) {
        ASSERT(document);
        ASSERT(document[KW_DOC_ID] && document[KW_DOC_PARTITION]);
        ASSERT(typeof document["_self"] === "string", "documentObject must contain `_self` as its link");

        let replaceOptions = {
            // indexAction: "default" | "include" | "exclude",
            // etag: GetEtag()
        };
        let etag = GetEtag(document[KW_DOC_ID]); // Can be null: means unknown
        if (etag !== null) {
            replaceOptions["etag"] = etag;
        }

        let isAccepted = Collection.replaceDocument(
            document["_self"], // documentLink: string
            document, // document: Object
            replaceOptions, // options: ReplaceOptions
            function(error, resource, options) { // callback: RequestCallback
                if (error) {
                    if (error.number === ErrorCodes.RequestEntityTooLarge) {
                        // This document is too large!
                        ASSERT(resource === null);
                        replaceCallback(true, null);
                    }
                    else {
                        // This operations failed due to other reasons
                        ERROR(error);
                    }
                }
                else {
                    // This document is successfully uploaded
                    UpdateEtag(resource[KW_DOC_ID], resource["_etag"]);

                    replaceCallback(false, resource);
                }
            }
        );
        if (!isAccepted) {
            ERROR(Status.NotAccepted, "[TryReplaceDocument] Not accepted");
        }
    }


    /**
     * Delete an existing document.
     * 
     * @param {Object} document - The existing document object (just use its _self field)
     * @param {function()} deleteCallback - The callback indicating the deletion is done! 
     * @returns {} 
     */
    function DeleteDocument(document, deleteCallback) {
        ASSERT(document);
        ASSERT(document[KW_DOC_ID] && document[KW_DOC_PARTITION]);
        ASSERT(typeof document["_self"] === "string", "documentObject must contain `_self` as its link");

        let deleteOptions = {
            // indexAction: "default" | "include" | "exclude",
            // etag: GET_ETAG()
        };
        let etag = GetEtag(document[KW_DOC_ID]); // Can be null: means unknown
        if (etag !== null) {
            deleteOptions["etag"] = etag;
        }

        let documentId = document[KW_DOC_ID];
        let isAccepted = Collection.deleteDocument(
            document["_self"], // documentLink: string
            deleteOptions, // options: DeleteOptions
            function(dbError, resource, options) { // callback: RequestCallback
                if (dbError) {
                    // This operations failed due some reason
                    ERROR(dbError);
                }
                else {
                    // This document is successfully deleted
                    UpdateEtag(documentId, null);

                    deleteCallback();
                }
            }
        );
        if (!isAccepted) {
            ERROR(Status.NotAccepted, "[DeleteDocument] Not accepted");
        }
    }


    /**
     * 
     * @param {Object<>} document 
     * @param {boolean} autoGenerateId 
     * @param {function(Object<>)} createCallback - createCallback(createdDocument)
     * @returns {} 
     */
    function CreateDocument(document, autoGenerateId, createCallback) {

        ASSERT(typeof document[KW_DOC_PARTITION] === "string");

        if (!autoGenerateId) {
            ASSERT(typeof document[KW_DOC_ID] === "string");
        }

        let createOptions = {
            // indexAction: "default" | "include" | "exclude",
            disableAutomaticIdGeneration: !autoGenerateId
        };

        let isAccepted = Collection.createDocument(
            CollectionLink, // collectionLink: string
            document, // body: Object
            createOptions, // options: CreateOptions
            function(dbError, resource, options) { // callback: RequestCallback
                if (dbError) {
                    ERROR(dbError);
                }
                else {
                    UpdateEtag(resource[KW_DOC_ID], resource["_etag"]);

                    createCallback(resource);
                }
            }
        );
        if (!isAccepted) {
            ERROR(Status.NotAccepted, "[CreateDocument] Not accepted");
        }
    }


    /**
     * This function would fail if not exactly one document is found.
     * 
     * @param {string} id 
     * @param {function(Object<>)} retrieveCallback - retrieveCallback(document)
     * @returns {} 
     */
    function RetrieveDocumentById(id, retrieveCallback) {
        ASSERT(id && typeof id === "string");
        ASSERT(retrieveCallback && typeof (retrieveCallback) === "function");

        let queryOptions = {
            enableScan: false,
            enableLowPrecisionOrderBy: true
        };

        let isAccepted = Collection.queryDocuments(
            CollectionLink, // collectionLink: string
            `SELECT * FROM doc WHERE doc['${KW_DOC_ID}'] = '${id}'`, // filterQuery: string|object
            queryOptions, // options: FeedOptions
            function(dbError, resources, options) { // callback: FeedCallback
                if (dbError) {
                    ERROR(dbError);
                }
                else {
                    ASSERT(resources instanceof Array, "Query result should be an array");
                    ASSERT(resources.length === 1, "The retrieve-by-id result should have exactly one document");

                    var existEtag = GetEtag(id);
                    if (existEtag && (existEtag !== resources[0][KW_DOC_ETAG])) {
                        ERROR(Status.DBError, "ETAG mismatch!");
                    }

                    retrieveCallback(resources[0]);
                }
            }
        );
        if (!isAccepted) {
            ERROR(Status.NotAccepted, "[RetrieveDocumentById] Not accepted");
        }
    }



    //================================================================================================
    //------------------------------------------------------------------------------------------------
    // [4]
    // The main logic
    //  - Loop the opArray and execute each operation
    //------------------------------------------------------------------------------------------------
    //================================================================================================


    //
    // Prepare known etags
    //
    ASSERT(typeof etagsObject === "object");
    for (let tmpDocId in etagsObject) {
        if (etagsObject.hasOwnProperty(tmpDocId)) {
            let tmpEtag = etagsObject[tmpDocId];
            ASSERT(tmpEtag === null || (typeof tmpEtag === "string" && tmpEtag));
            UpdateEtag(tmpDocId, tmpEtag);
        }
    }

    /**
     * Execute one operation and return its result in contentCallback.
     * 
     * @param {} operation 
     * @param {function(Object<>)} contentCallback - contentCallback([content])
     * @returns {} 
     */
    function DispatchOperation(operation, contentCallback) {
        if (operation["op"] === "AddVertex") {
            /* Parameter schema: 
                {
                    "op": "AddVertex",
                    "vertexObject": { ... }
                }
               Response content:
                {
                    "etag": ...
                }
            */
            AddVertex(
                operation["vertexObject"],
                function(vertexDocument) {
                    contentCallback(null);
                });
        }
        else if (operation["op"] === "AddEdge") {
            /* Parameter schema: 
                {
                    "op": "AddEdge",
                    "srcV": ...,
                    "sinkV": ...,
                    "isReverse", true/false
                    "spillThreshold", null/0/>0     // Can be null
                    "edgeObject", {
                        KW_DOC_ID: ...,
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
            AddEdge(
                operation["srcV"],
                operation["sinkV"],
                operation["isReverse"],
                operation["spillThreshold"],
                operation["edgeObject"],
                function(firstEdgeDocId, latestEdgeDocId) {
                    let content = {
                        "firstSpillEdgeDocId": firstEdgeDocId,
                        "newEdgeDocId": latestEdgeDocId
                    };
                    contentCallback(content);
                });
        }
        else if (operation["op"] === "DropVertexProperty") {
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
            DropVertexProperty(
                operation["vertexId"],
                operation["propertyName"],
                function(found) {
                    let content = {
                        "found": found
                    };
                    contentCallback(content);
                });
        }
        else if (operation["op"] === "DropVertexSingleProperty") {
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
            DropVertexSingleProperty(
                operation["vertexId"],
                operation["propertyName"],
                operation["singlePropertyId"],
                function(found) {
                    let content = {
                        "found": found
                    };
                    contentCallback(content);
                }
            );
        }
        else if (operation["op"] === "DropVertexSinglePropertyMetaProperty") {
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
            DropVertexSinglePropertyMetaProperty(
                operation["vertexId"],
                operation["propertyName"],
                operation["singlePropertyId"],
                operation["metaName"],
                function(found) {
                    let content = {
                        "found": found
                    };
                    contentCallback(content);
                }
            );
        }
        else if (operation["op"] === "DropEdge") {
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
            DropEdge(
                operation["srcOrSinkVertexId"],
                operation["edgeId"],
                operation["edgeDocId"], // Can be null
                operation["isReverse"],
                function(found, oppoSideVId) {
                    let content = {
                        "found": found,
                        "oppoSideVId": oppoSideVId
                    };
                    contentCallback(content);
                });
        }
        else if (operation["op"] === "DropEdgeProperty") {
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
            DropEdgeProperty(
                operation["srcOrSinkVertexId"],
                operation["edgeId"],
                operation["edgeDocId"], // Can be null
                operation["isReverse"],
                operation["dropProperties"],
                function(found, oppoSideVId) {
                    let content = {
                        "found": found,
                        "oppoSideVId": oppoSideVId
                    };
                    contentCallback(content);
                });
        }
        else if (operation["op"] === "UpdateEdgeProperty") {
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
                    "vertexLatestEdgeDocId": ...,  // Can be null
                }
            */
            UpdateEdgeProperty(
                operation["srcOrSinkVertexId"],
                operation["edgeId"],
                operation["edgeDocId"], // Can be null
                operation["isReverse"],
                operation["spillThreshold"],
                operation["updateProperties"],
                function() {
                    let content = {
                        "found": false
                    };
                    contentCallback(content);
                },
                function(didSpill, vertexFirstEdgeDocId, vertexLatestEdgeDocId) {
                    ASSERT(operation["edgeId"] === null);
                    let content = {
                        "found": true,
                        "didSpill": didSpill,
                        "vertexFirstEdgeDocId": vertexFirstEdgeDocId,
                        "vertexLatestEdgeDocId": vertexLatestEdgeDocId
                    };
                    contentCallback(content);
                },
                function(newEdgeDocId) {
                    let content = {
                        "found": true,
                        "newEdgeDocId": newEdgeDocId
                    };
                    contentCallback(content);
                }
            );
        }
        else if (operation["op"] === "UpdateVertexProperty") {
            /* Parameter schema: 
                {
                    "op": "UpdateVertexProperty",
                    "vertexId": ...,
                    "updateProperties": [
                        {
                            "single": true/false,
                            "key": "<prop name>",
                            "value": {
                                "id": "xxxx",
                                "_value": ...,
                                "_meta": { ... },
                            }
                        },
                        {
                            "single": true/false,
                            "key": "<prop name>",
                            "value": {
                                "id": "xxxx",
                                "_value": ...,
                                "_meta": { ... },
                            }
                        },
                        ...
                    ]
                }
               Response content:
                {
                    "found": true/false

                    "spillReverse": null/true/false

                    "vertexFirstEdgeDocId": ...,  // Not null if (spillReverse != null)
                    "vertexLatestEdgeDocId": ...,  // Not null if (spillReverse != null)
                    "latestEdgeId": ...,  // Not null if (spillReverse != null)
                }
            */
            UpdateVertexProperty(
                operation["vertexId"],
                operation["updateProperties"],
                function(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId, latestEdgeId) {
                    let content = {
                        "didSpill": didSpill,
                        "spillReverse": spillReverse,
                        "firstSpillEdgeDocId": firstSpillEdgeDocId,
                        "latestSpillEdgeDocId": latestSpillEdgeDocId,
                        "latestEdgeId": latestEdgeId
                    };
                    contentCallback(content);
                }
            );
        }
        else {
            ERROR(Status.InternalError, `Unknown operation string: ${operation["op"]}`);
        }
    }

    const OperationCount = opArray.length;
    opArray.push({  // The last dummy operation: all done!
        DoWork: function() {
            DONE();
        }
    });

    for (let i = 0; i < OperationCount; i++) {
        opArray[i]["Index"] = i;
    }
    for (let i = 0; i < OperationCount; i++) {
        const operation = opArray[i];
        operation.DoWork = function() {
            var index = operation["Index"];
            ASSERT(index >= 0 && index < OperationCount);
            DispatchOperation(
                operation,
                function(content) {
                    SUCCESS(index, content);
                    opArray[index + 1].DoWork(); // "Closure on a variable modified in loop of outer scope" is safe here
                }
            );
        }
    }


    //
    // Main: Do the work now!
    //
    function Main() {
        opArray[0].DoWork();
    }

    Main();
    return;



    //================================================================================================
    //------------------------------------------------------------------------------------------------
    // [5]
    // Operations
    //  - Vertex: AddVertex, DropVertex, 
    //  - Edge: AddEdge, DropEdge, 
    //------------------------------------------------------------------------------------------------
    //================================================================================================

    function AddVertex(vertexObject, addVertexCallback) {

        ASSERT(vertexObject[KW_DOC_ID]);
        ASSERT(vertexObject[KW_DOC_PARTITION]);
        ASSERT(vertexObject[KW_DOC_ID] === vertexObject[KW_DOC_PARTITION]);

        CreateDocument(vertexObject, false, addVertexCallback);
    }


    function AddEdge(srcVId, sinkVId, isReverse, spillThreshold, edgeObject, addEdgeCallback) {

        ASSERT(typeof srcVId === "string" && srcVId);
        ASSERT(typeof sinkVId === "string" && sinkVId);
        ASSERT(typeof isReverse === "boolean");
        if (!spillThreshold) {
            spillThreshold = 0;
        }
        else {
            ASSERT(typeof spillThreshold === "number");
        }
        ASSERT(typeof edgeObject === "object" && edgeObject);
        ASSERT(typeof addEdgeCallback === "function" && addEdgeCallback);

        //
        // Check edge object
        //
        ASSERT(typeof edgeObject[KW_DOC_ID] === "string");
        ASSERT(edgeObject[KW_EDGE_LABEL] === null || typeof edgeObject[KW_EDGE_LABEL] === "string");
        if (isReverse) {
            ASSERT(typeof edgeObject[KW_EDGE_SRCV] === "string");
            ASSERT(edgeObject[KW_EDGE_SRCV_LABEL] === null || typeof edgeObject[KW_EDGE_SRCV_LABEL] === "string");
        }
        else {
            ASSERT(typeof edgeObject[KW_EDGE_SINKV] === "string");
            ASSERT(edgeObject[KW_EDGE_SINKV_LABEL] === null || typeof edgeObject[KW_EDGE_SINKV_LABEL] === "string");
        }


        //
        // Do the insertion
        //
        let modifyVertexId = isReverse ? sinkVId : srcVId;
        let modifyArrayName = isReverse ? KW_VERTEX_REV_EDGE : KW_VERTEX_EDGE;
        let edgeSpillName = isReverse ? KW_VERTEX_REVEDGE_SPILLED : KW_VERTEX_EDGE_SPILLED;
        RetrieveDocumentById(
            modifyVertexId,
            function(vertexDocument) {
                let edgeContainer = vertexDocument[modifyArrayName];
                ASSERT(edgeContainer instanceof Array);

                ASSERT(typeof vertexDocument[edgeSpillName] === "boolean");
                if (vertexDocument[edgeSpillName]) {
                    // These edges are spilled
                    let tryEdgeDocId = edgeContainer[0][KW_DOC_ID];
                    ASSERT(typeof tryEdgeDocId === "string" && tryEdgeDocId);

                    TryAddEdgeToEdgeDocument(
                        edgeObject,
                        spillThreshold,
                        tryEdgeDocId,
                        function(addToEdgeDocId) {
                            if (addToEdgeDocId === tryEdgeDocId) { // Added to the latest edge document
                                // The vertex document should be left unchanged
                                addEdgeCallback(null, tryEdgeDocId);
                            }
                            else { // Add to a new edge document
                                // Update the latest edge document id in vertex document
                                edgeContainer[0][KW_DOC_ID] = addToEdgeDocId;
                                TryReplaceDocument(
                                    vertexDocument,
                                    function(dummyTooLarge, newVertexDocument) {
                                        ASSERT(dummyTooLarge === false);
                                        addEdgeCallback(null, addToEdgeDocId);
                                    });
                            }
                        });
                }
                else {
                    // This edge array is not spilled
                    edgeContainer.push(edgeObject);

                    if (spillThreshold !== 0 && edgeContainer.length > spillThreshold) {
                        // Spilling threshold is reached: too large!
                        ContinueWork(true, null);
                    }
                    else {
                        TryReplaceDocument(
                            vertexDocument,
                            function(tooLarge, newVertexDocument) {
                                ContinueWork(tooLarge, newVertexDocument);
                            });
                    }
                    
                    function ContinueWork(tooLarge, newVertexDocument) {
                        if (tooLarge) {
                            ASSERT(newVertexDocument === null);

                            // This vertex document is too large, either because the spilling threshold is reached,
                            // or the document excceeds the size limit. Now spill this vertex document!
                            // NOTE: now the vertex-document is not modified (etag not changed)
                            SpillVertex(
                                vertexDocument,
                                isReverse,
                                function(firstEdgeDocId, secondEdgeDocId) {
                                    addEdgeCallback(firstEdgeDocId, secondEdgeDocId);
                                });
                        }
                        else {
                            addEdgeCallback(null, null);
                        }
                    };
                }
            }
        );
    }


    function DropEdge(vertexId, edgeId, edgeDocId, isReverse, dropEdgeCallback) {
        ASSERT(typeof vertexId === "string" && vertexId);
        ASSERT(typeof edgeId === "string" && edgeId);
        ASSERT(edgeDocId === null || (typeof edgeDocId === "string" && edgeDocId)); // edgeDocId can be null
        ASSERT(typeof isReverse === "boolean");
        ASSERT(typeof dropEdgeCallback === "function" && dropEdgeCallback);

        FindEdge(
            vertexId,
            edgeId,
            edgeDocId,
            isReverse,
            function(found, isSpilled, document, edgeContainer, index) {
                if (!found) {
                    dropEdgeCallback(false, null);
                }
                else {
                    ASSERT(index >= 0 && index < edgeContainer.length);

                    // Get oppoSideVId
                    let oppoSideVId;
                    if (isReverse) {
                        oppoSideVId = edgeContainer[index][KW_EDGE_SRCV];
                    }
                    else {
                        oppoSideVId = edgeContainer[index][KW_EDGE_SINKV];
                    }


                    if (!isSpilled || edgeContainer.length > 1) {
                        // Not spilled || Edge document not empty after deletion
                        ContinueWork(false); // Don't delete the document
                    }
                    else {
                        RetrieveDocumentById(
                            vertexId,
                            function(vertexDocument) {
                                var doDelete;
                                if (isReverse) {
                                    ASSERT(vertexDocument[KW_VERTEX_REVEDGE_SPILLED] === true);
                                    doDelete = vertexDocument[KW_VERTEX_REV_EDGE][0][KW_DOC_ID] !== edgeDocId;
                                }
                                else {
                                    ASSERT(vertexDocument[KW_VERTEX_EDGE_SPILLED] === true);
                                    doDelete = vertexDocument[KW_VERTEX_EDGE][0][KW_DOC_ID] !== edgeDocId;
                                }
                                ContinueWork(doDelete);
                            }
                        );
                    }

                    function ContinueWork(doDelete) {
                        if (doDelete) {
                            DeleteDocument(
                                document,
                                function() {
                                    dropEdgeCallback(true, oppoSideVId);
                                }
                            );
                        }
                        else {
                            edgeContainer.splice(index, 1);
                            TryReplaceDocument(
                                document,
                                function(dummyTooLarge) {
                                    ASSERT(dummyTooLarge === false);
                                    dropEdgeCallback(true, oppoSideVId);
                                }
                            );
                        }
                    }
                }
            }
        );
    }


    function DropEdgeProperty(vertexId, edgeId, edgeDocId, isReverse, dropProperties, dropEdgePropertyCallback) {
        ASSERT(typeof vertexId === "string" && vertexId);
        ASSERT(typeof edgeId === "string" && edgeId);
        ASSERT(edgeDocId === null || (typeof edgeDocId === "string" && edgeDocId)); // edgeDocId can be null
        ASSERT(typeof isReverse === "boolean");
        ASSERT(dropProperties instanceof Array/* && dropProperties*/); // Can be empty
        ASSERT(typeof dropEdgePropertyCallback === "function" && dropEdgePropertyCallback);

        FindEdge(
            vertexId,
            edgeId,
            edgeDocId,
            isReverse,
            function(found, isSpilled, document, edgeContainer, index) {
                if (!found) {
                    dropEdgePropertyCallback(false, null);
                }
                else {
                    // Get oppoSideVId
                    let edgeObject = edgeContainer[index];
                    let oppoSideVId;
                    if (isReverse) {
                        oppoSideVId = edgeObject[KW_EDGE_SRCV];
                    }
                    else {
                        oppoSideVId = edgeObject[KW_EDGE_SINKV];
                    }

                    if (dropProperties.length === 0) {
                        dropEdgePropertyCallback(true, oppoSideVId);
                        return;
                    }

                    // We don't care whether this is a spilled edge document or a vertex document
                    // Just drop the edgeObject's property, and update the document
                    for (let i = 0; i < dropProperties.length; ++i) {
                        let propertyName = dropProperties[i];
                        if (edgeObject[propertyName] != undefined) {
                            delete edgeObject[propertyName];
                        }
                    }
                    TryReplaceDocument(
                        document,
                        function(dummyTooLarge) {
                            ASSERT(dummyTooLarge === false);
                            dropEdgePropertyCallback(true, oppoSideVId);
                        });

                }
            });
    }


    /**
     * 
     * @param {} vertexId 
     * @param {} updateProperties 
     * @param {} updateVertexPropertyCallback - 
     *          updateVertexPropertyCallback(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId, latestEdgeId)
     * @returns {} 
     */
    function UpdateVertexProperty(vertexId, updateProperties, updateVertexPropertyCallback) {
        RetrieveDocumentById(
            vertexId,
            function(vertexDocument) {
                for (let property in updateProperties) {
                    if (updateProperties.hasOwnProperty(property)) {
                        let isSingle = property["single"];
                        let key = property["key"];
                        let value = property["value"];  // JObject including id, value, meta
                        
                        // Get or create the vertex property
                        let propertyArray = vertexDocument[key];
                        if (propertyArray === undefined) {
                            propertyArray = new Array();
                            vertexDocument[key] = propertyArray;
                        }
                        else {
                            ASSERT(propertyArray);
                        }

                        // If single property (not list), clear the propertyArray
                        if (isSingle) {
                            propertyArray.splice(0);
                        }
                        propertyArray.push(value);

                        //
                        // Prepare the src & sink LAST edge id
                        // We need to pass this value back to the caller (in order to update the cache)
                        //
                        let outLastEdgeId = [null];
                        let outEdgeContainer = vertexDocument[KW_VERTEX_EDGE];
                        if (!vertexDocument[KW_VERTEX_EDGE_SPILLED] && outEdgeContainer.length > 0) {
                            outLastEdgeId[0] = outEdgeContainer[outEdgeContainer.length - 1][KW_DOC_ID];
                        }
                        let inLastEdgeId = [null];
                        let inEdgeContainer = vertexDocument[KW_VERTEX_REV_EDGE];
                        if (!vertexDocument[KW_VERTEX_REVEDGE_SPILLED] && inEdgeContainer.length > 0) {
                            inLastEdgeId[0] = inEdgeContainer[inEdgeContainer.length - 1][KW_DOC_ID];
                        }

                        // Upload or spill
                        UploadVertexDocumentOrSpillIfTooLarge(
                            vertexDocument,
                            null,
                            function(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId) {
                                let latestEdgeId = null;
                                if (didSpill) {
                                    if (spillReverse) {
                                        ASSERT(inLastEdgeId[0] != null);
                                        latestEdgeId = inLastEdgeId[0];
                                    }
                                    else {
                                        ASSERT(outLastEdgeId[0] != null);
                                        latestEdgeId = outLastEdgeId[0];
                                    }
                                }
                                updateVertexPropertyCallback(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId, latestEdgeId);
                            }
                        );
                    }
                }
            }
        );
    }


    /**
     * 
     * @param {} vertexId - source or sink vertex
     * @param {} edgeId 
     * @param {} edgeDocId 
     * @param {} isReverse 
     * @param {} spillThreshold 
     * @param {} updateProperties - JSON object
     * @returns {} 
     */
    function UpdateEdgeProperty(vertexId,
                                edgeId,
                                edgeDocId,
                                isReverse,
                                spillThreshold,
                                updateProperties,
                                callbackNotFound,
                                callbackOriginallyNotSpilled,
                                callbackOriginallySpilled) {
        //
        // The logic:
        // 1) Find the edge's vertex-document / edge-document  -> edgeContainer
        // 2) Remove the edgeObject from edgeContainer
        // 3) Modify edgeObject
        // 4) Append edgeObject to the LAST of edgeContainer
        // 5) Upload the vertex-document / edge-document
        //     - If success, done
        //     - If tooLarge, originally not spilled, spill the vertex
        //     - If tooLarge, originally spilled, try to add to the vertex's latest edge-document
        //       - If success, done
        //       - If tooLarge, create a new spilled edge-document to store it, and set as the vertex's latest edge-document
        //         - If success, done
        //         - If tooLarge, that means the modified edge can't be filled into one document. Fail!
        //
        if (!spillThreshold) {
            spillThreshold = 0;
        }
        else {
            ASSERT(typeof spillThreshold === "number" && spillThreshold > 0);
        }

        FindEdge(
            vertexId,
            edgeId,
            edgeDocId,
            isReverse,
            function(found, isSpilled, vertexOrEdgeDocument, edgeContainer, index) {
                if (!found) {
                    callbackNotFound();
                }
                else {
                    // Retrieve & update the edgeObject
                    let edgeObject = edgeContainer[index];
                    for (let propertyName in updateProperties) {
                        if (updateProperties.hasOwnProperty(propertyName)) {
                            edgeObject[propertyName] = updateProperties[propertyName];
                        }
                    }

                    // Move edgeObject to the LAST of edgeContainer
                    edgeContainer.splice(index, 1);
                    edgeContainer.push(edgeObject);

                    if (isSpilled) {

                        UploadEdgeDocumentOrSpillIfTooLarge(
                            vertexOrEdgeDocument, // actually is edge-document
                            spillThreshold,
                            function(spilledNew, addToEdgeDocId, originalVertexLatestEdgeDocId) {
                                callbackOriginallySpilled(addToEdgeDocId);
                            }
                        );
                    }
                    else {

                        UploadVertexDocumentOrSpillIfTooLarge(
                            vertexOrEdgeDocument, // actually is vertex-document
                            isReverse,
                            function(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId) {
                                if (didSpill) {
                                    callbackOriginallyNotSpilled(true, firstSpillEdgeDocId, latestSpillEdgeDocId);
                                }
                                else {
                                    callbackOriginallyNotSpilled(false, null, null);
                                }
                            }
                        );
                    }
                }
            }
        );
    }

    /**
     * Try to upload the edge-document
     * If it is too large, spill the LAST edgeObject to a new edge-document (and upload the rest of edge-document)
     *   - Firstly attempt to insert the LAST edgeObject into the latest edge-document of vertex
     *   - If fails, create a new edge-document to store it, and update the latest edge-document of vertex-document
     * 
     * @param {Object<>} edgeDocument 
     * @param {number} spillThreshold 
     * @param {} callback - 
     *           (spilledNew, newEdgeDocId, originalVertexLatestEdgeDocId)
     * @returns {} 
     */
    function UploadEdgeDocumentOrSpillIfTooLarge(edgeDocument, spillThreshold, callback) {
        TryReplaceDocument(
            edgeDocument,
            function(tooLarge, newDocument) {
                if (!tooLarge) {
                    callback(false, newDocument[KW_DOC_ID], null);
                }
                else {
                    let edgeContainer = edgeDocument[KW_EDGEDOC_EDGE];
                    if (edgeContainer.length === 1) {
                        ERROR(Status.DBError, "Too large!");
                    }

                    let lastEdgeObject = edgeContainer[edgeContainer.length - 1];
                    edgeContainer.splice(edgeContainer.length - 1, 1);

                    TryReplaceDocument(
                        edgeDocument,
                        function(dymmyTooLarge, newEdgeDocument) {
                            ASSERT(dymmyTooLarge === false);

                            let vertexId = newEdgeDocument[KW_EDGEDOC_VERTEXID];
                            let isReverse = newEdgeDocument[KW_EDGEDOC_ISREVERSE];
                            RetrieveDocumentById(
                                vertexId,
                                function(vertexDocument) {
                                    let tryLatestEdgeDocId;
                                    if (!isReverse) {
                                        ASSERT(vertexDocument[KW_VERTEX_EDGE_SPILLED] === true);
                                        tryLatestEdgeDocId = vertexDocument[KW_VERTEX_EDGE][0][KW_DOC_ID];
                                    }
                                    else {
                                        ASSERT(vertexDocument[KW_VERTEX_REVEDGE_SPILLED] === true);
                                        tryLatestEdgeDocId = vertexDocument[KW_VERTEX_REV_EDGE][0][KW_DOC_ID];
                                    }

                                    TryAddEdgeToEdgeDocument(
                                        lastEdgeObject,
                                        spillThreshold,
                                        tryLatestEdgeDocId,
                                        function(addToEdgeDocId) {
                                            if (addToEdgeDocId !== tryLatestEdgeDocId) {
                                                // Now the latest edge-document is too small to hold lastEdgeObject
                                                // Spilled to a new edge-document, and update the new edge-docuemnt as the lastest now!
                                                vertexDocument[isReverse ? KW_VERTEX_REV_EDGE : KW_VERTEX_EDGE][0][KW_DOC_ID] = addToEdgeDocId;
                                                TryReplaceDocument(
                                                    vertexDocument,
                                                    function(dummyTooLarge, newVertexDocument) {
                                                        ASSERT(dummyTooLarge === false);
                                                        callback(true, addToEdgeDocId, tryLatestEdgeDocId);
                                                    });
                                            }
                                            else {
                                                // Now (newEdgeDocId == tryLatestEdgeDocId)
                                                callback(true, addToEdgeDocId, tryLatestEdgeDocId);
                                            }
                                        }
                                    );
                                }
                            );
                        }
                    );
                }
            }
        );
    }


    /**
     * 
     * @param {} vertexDocument 
     * @param {} spillReverse - bool?
     * @param {} callback
     *           callback(didSpill, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId)
     * @returns {} 
     */
    function UploadVertexDocumentOrSpillIfTooLarge(vertexDocument, spillReverse, callback) {
        TryReplaceDocument(
            vertexDocument,
            function(tooLarge, newDocument) {
                if (!tooLarge) {
                    callback(false, null, null, null);
                }
                else {
                    // Now it is too large, either spill the incoming or outgoing edges
                    let outEdgeSpilled = vertexDocument[KW_VERTEX_EDGE_SPILLED];
                    let inEdgeSpilled = vertexDocument[KW_VERTEX_REVEDGE_SPILLED];
                    if (spillReverse === null) {
                        if (outEdgeSpilled || inEdgeSpilled) {
                            if (outEdgeSpilled && inEdgeSpilled) {
                                ERROR(Status.DBError, "Vertex is too large!");
                            }
                            // Now either outEdges or inEdges are spilled
                            spillReverse = outEdgeSpilled;
                        }
                        else {
                            var edgeStr = JSON.stringify(vertexDocument[KW_VERTEX_EDGE]);
                            var revEdgeStr = JSON.stringify(vertexDocument[KW_VERTEX_REV_EDGE]);
                            spillReverse = (revEdgeStr.length > edgeStr.length);
                        }
                    }

                    SpillVertex(
                        vertexDocument,
                        spillReverse,
                        function(firstSpillEdgeDocId, latestSpillEdgeDocId) {
                            callback(true, spillReverse, firstSpillEdgeDocId, latestSpillEdgeDocId);
                        });
                }
            });
    }


    /**
     * 
     * @param {} vertexId - If isReverse, this is sinkV, otherwise, this is srcV
     * @param {} edgeId 
     * @param {} edgeDocId 
     * @param {} isReverse 
     * @param {function(boolean,boolean,Object<>,Array<>,number)} findEdgeCallback - 
     *         (found?, isSpilled, vertexOrEdgeDocument, edgeContainer, index)
     * @returns {} 
     */
    function FindEdge(vertexId, edgeId, edgeDocId, isReverse, findEdgeCallback) {
        let docId;
        if (edgeDocId == null) {
            docId = vertexId;
        }
        else {
            docId = edgeDocId;
        }

        RetrieveDocumentById(
            docId,
            function(document) {
                let found = false;
                let edgeContainer, index;
                if (edgeDocId == null) {
                    // Not spilled
                    ASSERT(document[isReverse ? KW_VERTEX_REVEDGE_SPILLED : KW_VERTEX_EDGE_SPILLED] === false);
                    edgeContainer = document[isReverse ? KW_VERTEX_REV_EDGE : "_edge"];
                    for (index = 0; index < edgeContainer.length; ++index) {
                        if (edgeContainer[index][KW_DOC_ID] === edgeId) {
                            findEdgeCallback(true, false, document, edgeContainer, index);
                            found = true;
                            break;
                        }
                    }
                }
                else {
                    // Spilled
                    ASSERT(document[KW_EDGEDOC_VERTEXID] === vertexId);
                    ASSERT(document[KW_EDGEDOC_ISREVERSE] === isReverse);
                    edgeContainer = document[KW_EDGEDOC_EDGE];
                    for (index = 0; index < edgeContainer.length; ++index) {
                        if (edgeContainer[index][KW_DOC_ID] === edgeId) {
                            findEdgeCallback(true, true, document, edgeContainer, index);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) {
                    findEdgeCallback(false, undefined, undefined, undefined, undefined);
                }
            });
    }


    /**
     * 
     * @param {string} vertexId 
     * @param {string} propertyName 
     * @param {function(boolean)} callback - Returns whether this property is found?
     * @returns {} 
     */
    function DropVertexProperty(vertexId, propertyName, callback) {
        ASSERT(typeof vertexId === "string" && vertexId);
        ASSERT(typeof propertyName === "string" && propertyName);
        ASSERT(typeof callback === "function" && callback);

        RetrieveDocumentById(
            vertexId,
            function(vertexDocument) {
                let found = !(vertexDocument[propertyName] === undefined);
                if (found) {
                    delete vertexDocument[propertyName];
                    TryReplaceDocument(
                        vertexDocument,
                        function(dummyTooLarge, newVertexDocument) {
                            ASSERT(dummyTooLarge === false);
                            callback(true);
                        });
                }
                else {
                    callback(false);
                }
            }
        );
    }


    function DropVertexSingleProperty(vertexId, propertyName, singlePropertyId, callback) {
        ASSERT(typeof vertexId === "string" && vertexId);
        ASSERT(typeof propertyName === "string" && propertyName);
        ASSERT(typeof singlePropertyId === "string" && singlePropertyId);
        ASSERT(typeof callback === "function" && callback);

        RetrieveDocumentById(
            vertexId,
            function(vertexDocument) {
                let found = vertexDocument[propertyName] !== undefined;
                if (found) {
                    found = false;
                    let singlePropArray = vertexDocument[propertyName];
                    ASSERT(singlePropArray instanceof Array);
                    for (let index = singlePropArray.length - 1; index >= 0; --index) {
                        if (singlePropArray[index][KW_DOC_ID] === singlePropertyId) {
                            if (singlePropArray.length === 1) {
                                // If this single property is not duplicated, delete the whole vertex property!
                                delete vertexDocument[propertyName];
                            }
                            else {
                                // singlePropArray.length > 1, just delete this single-property
                                singlePropArray.splice(index, 1);
                            }
                            found = true;
                            break;
                        }
                    }

                    if (found) {
                        TryReplaceDocument(
                            vertexDocument,
                            function(dummyTooLarge, newVertexDocument) {
                                ASSERT(dummyTooLarge === false);
                                callback(true);
                            }
                        );
                    }
                    else {
                        callback(false);
                    }
                }
                else {
                    callback(false);
                }
            }
        );
    }


    function DropVertexSinglePropertyMetaProperty(vertexId, propertyName, singlePropertyId, metaName, callback) {

        ASSERT(typeof vertexId === "string" && vertexId);
        ASSERT(typeof propertyName === "string" && propertyName);
        ASSERT(typeof singlePropertyId === "string" && singlePropertyId);
        ASSERT(typeof metaName === "string" && metaName);
        ASSERT(typeof callback === "function" && callback);

        RetrieveDocumentById(
            vertexId,
            function(vertexDocument) {

                let found = !(vertexDocument[propertyName] === undefined);
                if (found) {
                    let singlePropArray = vertexDocument[propertyName];
                    let singleProp = null;
                    for (let index = 0; index < singlePropArray.length; ++index) {
                        if (singlePropArray[index][KW_DOC_ID] === singlePropertyId) {
                            singleProp = singlePropArray[index];
                            break;
                        }
                    }

                    if (singleProp !== null) {
                        ASSERT(singleProp["_meta"] !== undefined);
                        found = (singleProp["_meta"][metaName] !== undefined);

                        if (found) {
                            delete (singleProp["_meta"])[metaName];
                            TryReplaceDocument(
                                vertexDocument,
                                function(dummyTooLarge, newVertexDocument) {
                                    ASSERT(dummyTooLarge === false);
                                    callback(true);
                                }
                            );
                        }
                        else {
                            callback(false);
                        }
                    }
                    else {
                        callback(false);
                    }
                }
                else {
                    callback(false);
                }
            }
        );
    }


    /**
     * Try to add an edge to the edge document.
     * If too large, create a new edge document to store the edge.
     * 
     * @param {Object} edgeObject 
     * @param {number} spillThreshold 
     * @param {string} firstTryEdgeDocId 
     * @param {function(string)} callback - The edge is added to which edge document?
     * @returns {} 
     */
    function TryAddEdgeToEdgeDocument(edgeObject, spillThreshold, firstTryEdgeDocId, callback) {
        ASSERT(typeof spillThreshold === "number" && spillThreshold >= 0);
        ASSERT(typeof firstTryEdgeDocId === "string" && firstTryEdgeDocId);
        ASSERT(typeof callback === "function" && callback);

        RetrieveDocumentById(
            firstTryEdgeDocId,
            function(tryEdgeDoc) {
                let vertexId = tryEdgeDoc[KW_EDGEDOC_VERTEXID];
                ASSERT(typeof vertexId === "string" && vertexId);

                let isReverse = tryEdgeDoc[KW_EDGEDOC_ISREVERSE];
                ASSERT(typeof isReverse === "boolean");

                let edgeContainer = tryEdgeDoc[KW_EDGEDOC_EDGE];

                // If spill threshold is reached, tooLarge = true
                if (spillThreshold > 0) {
                    ASSERT(edgeContainer.length <= spillThreshold);
                    if (edgeContainer.length === spillThreshold) {
                        ContinueWork(true);
                        return;
                    }
                }

                // Try to update the edge document
                edgeContainer.push(edgeObject);
                TryReplaceDocument(
                    tryEdgeDoc,
                    function(tooLarge, newDocument) {
                        ContinueWork(tooLarge);
                    }
                );


                function ContinueWork(tooLarge) {
                    if (!tooLarge) {
                        callback(firstTryEdgeDocId);
                    }
                    else {
                        // Now the `firstTryEdgeDoc` is too small for the new edge
                        // Spill this edge to a new edge document
                        // NOTE: `firstTryEdgeDoc` is not modified! (etag remains unchanged)
                        let newEdgeDocObject = { };
                        newEdgeDocObject[KW_DOC_PARTITION] = vertexId;
                        newEdgeDocObject[KW_EDGEDOC_VERTEXID] = vertexId;
                        newEdgeDocObject[KW_EDGEDOC_ISREVERSE] = isReverse;
                        newEdgeDocObject[KW_EDGEDOC_EDGE] = new Array(1);
                        newEdgeDocObject[KW_EDGEDOC_EDGE][0] = edgeObject;

                        CreateDocument(
                            newEdgeDocObject,
                            true,
                            function(newEdgeDoc) {
                                ASSERT(typeof newEdgeDoc[KW_DOC_ID] === "string");
                                ASSERT(typeof newEdgeDoc[KW_DOC_ETAG] === "string");

                                callback(newEdgeDoc[KW_DOC_ID]);
                            }
                        );
                    }
                }
            }
        );
    }

    /**
     * Spill a not-spilled vertex
     * 
     * @param {} vertexDocument 
     * @param {} isReverse 
     * @param {function(string, string)} spillCallback 
     * @returns {} 
     */
    function SpillVertex(vertexDocument, isReverse, spillCallback) {

        ASSERT(typeof isReverse === "boolean");

        let edgeSpillName = isReverse ? KW_VERTEX_REVEDGE_SPILLED : KW_VERTEX_EDGE_SPILLED;
        ASSERT(vertexDocument[edgeSpillName] === false);

        let modifyArrayName = isReverse ? KW_VERTEX_REV_EDGE : "_edge";
        let edgeContainer = vertexDocument[modifyArrayName];
        ASSERT(edgeContainer instanceof Array);
        ASSERT(edgeContainer.length > 0);

        // Prepare the first edge document, which contains all but the last edges of the original vertex
        let firstEdgeDocObject = { };
        //firstEdgeDocObject[KW_DOC_ID] = undefined;
        firstEdgeDocObject[KW_DOC_PARTITION] = vertexDocument[KW_DOC_PARTITION];
        firstEdgeDocObject[KW_EDGEDOC_VERTEXID] = vertexDocument[KW_DOC_ID];
        firstEdgeDocObject[KW_EDGEDOC_ISREVERSE] = isReverse;
        firstEdgeDocObject[KW_EDGEDOC_EDGE] = new Array(edgeContainer.length - 1);
        for (let i = 0; i < edgeContainer.length - 1; i++) {
            firstEdgeDocObject[KW_EDGEDOC_EDGE][i] = edgeContainer[i];
        }

        // Prepare the second edge document, which contains the last edge of the original vertex
        let secondEdgeDocObject = { };
        //secondEdgeDocObject[KW_DOC_ID] = undefined;
        secondEdgeDocObject[KW_DOC_PARTITION] = vertexDocument[KW_DOC_PARTITION];
        secondEdgeDocObject[KW_EDGEDOC_VERTEXID] = vertexDocument[KW_DOC_ID];
        secondEdgeDocObject[KW_EDGEDOC_ISREVERSE] = isReverse;
        secondEdgeDocObject[KW_EDGEDOC_EDGE] = new Array(1);
        secondEdgeDocObject[KW_EDGEDOC_EDGE][0] = edgeContainer[edgeContainer.length - 1];

        //
        // Now create the two edge documents and update the vertex document
        //
        let edgeDocIds = new Array(2); // [0]: firstEdgeDocId, [1]: secondEdgeDocId

        // Create the first edge document
        CreateDocument(
            firstEdgeDocObject,
            true,
            function(firstEdgeDoc) {
                edgeDocIds[0] = firstEdgeDoc[KW_DOC_ID];

                // Create the second edge document
                CreateDocument(
                    secondEdgeDocObject,
                    true,
                    function(secondEdgeDoc) {
                        edgeDocIds[1] = secondEdgeDoc[KW_DOC_ID];

                        // Update the vertex document
                        vertexDocument[edgeSpillName] = true;
                        edgeContainer.length = 0;
                        edgeContainer.push(new Object());
                        edgeContainer[0][KW_DOC_ID] = edgeDocIds[1];
                        TryReplaceDocument(
                            vertexDocument,
                            function(dummyTooLarge) {
                                ASSERT(dummyTooLarge === false);
                                spillCallback(edgeDocIds[0], edgeDocIds[1]);
                            });
                    });
            });
    }
}
