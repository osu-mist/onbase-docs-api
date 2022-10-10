import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import { parseQuery } from 'utils/parse-query';
import {
  getDocumentTypeIdByName,
  // getDocuments,
  getAccessToken,
  getDefaultKeywordsGuid,
  initiateStagingArea,
  uploadFile,
  archiveDocument,
  getDocumentById,
} from '../../db/http/onbase-dao';
import { serializeDocument } from '../../serializers/documents-serializer';

/**
 * Get documents
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  const { query, headers } = req;
  const onbaseProfile = headers['onbase-profile'];
  const parsedQuery = parseQuery(query);

  if (
    parsedQuery.keywordTypeNames.length !== parsedQuery.keywordValues.length
  ) {
    return errorBuilder(res, 400, [
      'Numbers of filter[keywordTypeNames] and filter[keywordValues] are not matched.',
    ]);
  }

  let result;
  let fbLb;

  // Get access token
  result = await getAccessToken(onbaseProfile);
  const token = result[0];
  [, fbLb] = result;

  // Convert document type name to document ID
  result = await getDocumentTypeIdByName(token, fbLb, parsedQuery.documentTypeName);
  if (result instanceof Error) {
    return errorBuilder(res, 400, [result.message]);
  }

  const documentTypeId = result[0];
  console.log(documentTypeId);
  [, fbLb] = result;

  return res.status(200).send('Nice');
};

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    const { files, headers, body: { documentTypeId } } = req;
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

    // Get keywords GUID
    result = await getDefaultKeywordsGuid(token, fbLb, documentTypeId);
    if (result instanceof Error) {
      return errorBuilder(res, 400, [result.message]);
    }

    const keywordsGuid = result[0];
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
      result = await uploadFile(token, fbLb, uploadId, numberOfPart + 1, mimetype, buffer);
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
      keywordsGuid,
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
