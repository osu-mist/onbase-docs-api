import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import {
  getAccessToken,
  getDefaultKeywordsGuid,
  initiateStagingArea,
  uploadFile,
  archiveDocument,
  getDocumentById,
} from '../../db/http/onbase-dao';
import { serializeDocument } from '../../serializers/documents-serializer';

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

    // Get access token
    const token = await getAccessToken(onbaseProfile);

    // Get keywords GUID
    const keywordsGuid = await getDefaultKeywordsGuid(token, documentTypeId);
    if (keywordsGuid instanceof Error) {
      return errorBuilder(res, 400, [keywordsGuid.message]);
    }

    // Prepare staging area
    const fileExtension = /[^.]*$/.exec(originalname)[0];
    const { id: uploadId, numberOfParts } = await initiateStagingArea(token, fileExtension, size);

    // Upload file in order
    // eslint-disable-next-line no-restricted-syntax
    for (const numberOfPart of _.range(numberOfParts)) {
      // eslint-disable-next-line no-await-in-loop
      const uploadResult = await uploadFile(token, uploadId, numberOfPart + 1, mimetype, buffer);
      if (uploadResult instanceof Error) {
        return errorBuilder(res, 413, [uploadResult.message]);
      }
    }

    // Archive document
    const documentId = await archiveDocument(
      token,
      documentTypeId,
      uploadId,
      keywordsGuid,
    );

    // Get document metadata
    const documentMetadata = await getDocumentById(token, documentId);

    // Serialize document
    const serializedDocument = serializeDocument(documentMetadata, req);

    return res.status(201).send(serializedDocument);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { post };
