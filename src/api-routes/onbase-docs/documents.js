import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import { parseQuery } from 'utils/parse-query';
import {
  getDocumentTypeByName,
  getKeywordTypesByNames,
  createQuery,
  getQueryResults,
  getDocumentsByIds,
  getAccessToken,
  getDefaultKeywordsGuid,
  initiateStagingArea,
  uploadFile,
  postIndexingModifiers,
  archiveDocument,
  getDocumentById,
} from '../../db/http/onbase-dao';
import {
  serializeDocument,
  serializeDocuments,
} from '../../serializers/documents-serializer';

/**
 * Get documents
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  const { query, headers } = req;
  const onbaseProfile = headers['onbase-profile'];
  const parsedQuery = parseQuery(query);
  const { startDate, endDate } = parsedQuery;

  if (
    parsedQuery.keywordTypeNames.length !== parsedQuery.keywordValues.length
  ) {
    return errorBuilder(res, 400, [
      'Numbers of filter[keywordTypeNames] and filter[keywordValues] are not matched.',
    ]);
  }

  if (new Date(startDate) > new Date(endDate)) {
    return errorBuilder(res, 400, [
      'The document end date is prior to the document start date.',
    ]);
  }

  let result;
  let fbLb;

  // Get access token
  result = await getAccessToken(onbaseProfile);
  const token = result[0];
  [, fbLb] = result;

  // Convert document type name to document ID
  result = await getDocumentTypeByName(token, fbLb, parsedQuery.documentTypeName);
  if (result instanceof Error) {
    return errorBuilder(res, 400, [result.message]);
  }

  const documentTypeId = result[0];
  [, fbLb] = result;

  // Convert keyword type names to keyword IDs
  result = await getKeywordTypesByNames(token, fbLb, parsedQuery);
  const keywordTypes = result[0];
  [, fbLb] = result;

  // Create a document query with provided search constraints
  result = await createQuery(
    token,
    fbLb,
    documentTypeId,
    keywordTypes,
    startDate,
    endDate,
  );

  if (result instanceof Error) {
    return errorBuilder(res, 400, [result.message]);
  }

  const queryId = result[0];
  [, fbLb] = result;

  // Get document IDs from the query
  result = await getQueryResults(token, fbLb, queryId);

  const documentIds = _.reduce(result[0].data.items, (ids, item) => {
    ids.push(item.id);
    return ids;
  }, []);
  [, fbLb] = result;

  // Get documents meta data by document IDs
  result = await getDocumentsByIds(token, fbLb, documentIds);

  // Serialize documents
  const serializedDocuments = serializeDocuments(result.items, query);

  return res.status(200).send(serializedDocuments);
};

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    const {
      files,
      headers,
      body: { documentTypeId, indexKey },
    } = req;
    const onbaseProfile = headers['onbase-profile'];

    // Upload document information from form data
    const uploadedDocument = _.find(files, { fieldname: 'uploadedDocument' });
    const {
      size,
      buffer,
      originalname,
      mimetype,
    } = uploadedDocument;

    let result;
    let fbLb; // the load balancer cookie (FB_LB) is updated after every request

    // Get access token
    result = await getAccessToken(onbaseProfile);
    const token = result[0];
    [, fbLb] = result;

    // Get default keywords GUID
    result = await getDefaultKeywordsGuid(token, fbLb, documentTypeId);
    if (result instanceof Error) {
      return errorBuilder(res, 400, [result.message]);
    }

    const defaultKeywordsGuid = result[0];
    [, fbLb] = result;

    // Perform autofill of keywords data by index key
    result = await postIndexingModifiers(
      token,
      fbLb,
      documentTypeId,
      defaultKeywordsGuid,
      indexKey,
    );
    const keywordCollection = result[0];
    [, fbLb] = result;

    // Prepare staging area
    const fileExtension = /[^.]*$/.exec(originalname)[0];
    result = await initiateStagingArea(token, fbLb, fileExtension, size);

    const { id: uploadId, numberOfParts, filePartSize } = result[0];
    [, fbLb] = result;

    // Upload file in order
    let chunkStart;
    let chunkEnd;

    // eslint-disable-next-line no-restricted-syntax
    for (const numberOfPart of _.range(numberOfParts)) {
      chunkStart = numberOfPart * filePartSize;
      chunkEnd = (numberOfPart + 1) * filePartSize;
      if (chunkEnd > size) {
        chunkEnd = size;
      }
      // eslint-disable-next-line no-await-in-loop
      result = await uploadFile(
        token,
        fbLb,
        uploadId,
        numberOfPart + 1,
        mimetype,
        buffer.slice(chunkStart, chunkEnd),
      );
      if (result instanceof Error) {
        return errorBuilder(res, 413, [result.message]);
      }
      [fbLb] = result;
    }

    // Archive document
    result = await archiveDocument(
      token,
      fbLb,
      documentTypeId,
      uploadId,
      keywordCollection,
    );
    const documentId = result[0];
    [, fbLb] = result;

    // Get document metadata
    const documentMetadata = await getDocumentById(token, fbLb, documentId);

    // Serialize document
    const serializedDocument = serializeDocument(documentMetadata, req);

    return res.status(201).send(serializedDocument);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get, post };
