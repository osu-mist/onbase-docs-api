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
  getDefaultKeywords,
  initiateStagingArea,
  uploadFile,
  archiveDocument,
  getDocumentKeywords,
  reIndexDocumentById,
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
  const {
    documentTypeName,
    startDate,
    endDate,
    keywordTypeNames,
    keywordValues,
  } = parsedQuery;

  if (
    keywordTypeNames
    && keywordValues
    && keywordTypeNames.length !== keywordValues.length
  ) {
    return errorBuilder(res, 400, [
      'Numbers of filter[keywordTypeNames] and filter[keywordValues] are not matched.',
    ]);
  }

  if (!startDate && !endDate) {
    return errorBuilder(res, 400, [
      'Either the start date or the end date must be a specific value.',
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
  result = await getDocumentTypeByName(token, fbLb, documentTypeName);
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
  result = !_.isEmpty(documentIds)
    ? await getDocumentsByIds(token, fbLb, documentIds)
    : { items: [] };

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

    // File size limit: 25 MB
    if (size > 25000000) {
      return errorBuilder(res, 413);
    }

    let result;
    // the load balancer cookie (FB_LB) is updated after every request
    let fbLb;

    // Get access token
    result = await getAccessToken(onbaseProfile);
    const token = result[0];
    [, fbLb] = result;

    // Get default keywords
    result = await getDefaultKeywords(token, fbLb);
    if (result instanceof Error) {
      return errorBuilder(res, 400, [result.message]);
    }

    let keywordCollection = result[0];
    [, fbLb] = result;

    // Prepare staging area
    const fileExtension = /[^.]*$/.exec(originalname)[0];
    result = await initiateStagingArea(token, fbLb, fileExtension, size);

    const { id: uploadId, numberOfParts } = result[0];
    [, fbLb] = result;

    // Upload file in order
    // eslint-disable-next-line no-restricted-syntax
    for (const numberOfPart of _.range(numberOfParts)) {
      // eslint-disable-next-line no-await-in-loop
      result = await uploadFile(
        token,
        fbLb,
        uploadId,
        numberOfPart + 1,
        mimetype,
        buffer,
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
      uploadId,
      keywordCollection,
    );
    const documentId = result[0];
    [, fbLb] = result;

    [keywordCollection] = await getDocumentKeywords(
      token,
      fbLb,
      documentId,
    );

    // Re-index document
    await reIndexDocumentById(
      token,
      fbLb,
      documentId,
      documentTypeId,
      keywordCollection,
      indexKey,
    );

    // Serialize document
    const serializedDocument = serializeDocument(
      {
        id: documentId,
        typeId: documentTypeId,
      },
      req,
    );

    return res.status(201).send(serializedDocument);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get, post };
