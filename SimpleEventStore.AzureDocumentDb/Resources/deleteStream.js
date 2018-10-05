function deleteStream(streamId) {
    var context = getContext();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();
    var response = getContext().getResponse();

    // Based on https://raw.githubusercontent.com/Azure/azure-cosmosdb-js-server/master/samples/stored-procedures/bulkDelete.js
    var query = {
        query: "SELECT c._self FROM Commits c WHERE c.streamId = @streamId ORDER BY c.eventNumber ASC",
        parameters: [{ name: "@streamId", value: streamId }]
    };

    var responseBody = {
        deleted: 0,
        continuation: true
    };

    tryQueryAndDelete();

    // Recursively runs the query w/ support for continuation tokens.
    // Calls tryDelete(documents) as soon as the query returns documents.
    function tryQueryAndDelete(continuation) {
        var requestOptions = { continuation: continuation };

        var isAccepted = collection.queryDocuments(collectionLink, query, requestOptions, function (err, retrievedDocs, responseOptions) {
            if (err) throw err;

            if (retrievedDocs.length > 0) {
                // Begin deleting documents as soon as documents are returned form the query results.
                // tryDelete() resumes querying after deleting; no need to page through continuation tokens.
                //  - this is to prioritize writes over reads given timeout constraints.
                tryDelete(retrievedDocs);
            } else if (responseOptions.continuation) {
                // Else if the query came back empty, but with a continuation token; repeat the query w/ the token.
                tryQueryAndDelete(responseOptions.continuation);
            } else {
                // Else if there are no more documents and no continuation token - we are finished deleting documents.
                responseBody.continuation = false;
                response.setBody(responseBody);
            }
        });

        // If we hit execution bounds - return continuation: true.
        if (!isAccepted) {
            response.setBody(responseBody);
        }
    }

    // Recursively deletes documents passed in as an array argument.
    // Attempts to query for more on empty array.
    function tryDelete(documents) {
        if (documents.length > 0) {
            // Delete the first document in the array.
            var isAccepted = collection.deleteDocument(documents[0]._self, {}, function (err, responseOptions) {
                if (err) throw err;

                responseBody.deleted++;
                documents.shift();

                // Delete the next document in the array.
                tryDelete(documents);
            });

            // If we hit execution bounds - return continuation: true.
            if (!isAccepted) {
                response.setBody(responseBody);
            }
        } else {
            // If the document array is empty, query for more documents.
            tryQueryAndDelete();
        }
    }
}