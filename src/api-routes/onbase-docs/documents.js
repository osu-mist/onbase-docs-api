import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import {
  getAccessToken,
  getKeywordsGuid,
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
    const { size, buffer, mimetype } = uploadedDocument;

    // Get access token
    const token = await getAccessToken(onbaseProfile);

    // Get keywords GUID
    const keywordsGuid = await getKeywordsGuid(token, documentTypeId);
    if (keywordsGuid instanceof Error) {
      return errorBuilder(res, 400, [keywordsGuid.message]);
    }

    // Prepare staging area
    const fileExtension = /[^/]*$/.exec(mimetype)[0];
    const { id: uploadId, numberOfParts } = await initiateStagingArea(token, fileExtension, size);

    // Upload file
    // TODO: numberOfParts logic handlers
    await uploadFile(token, uploadId, numberOfParts, mimetype, buffer);

    // Archive document
    const documentId = await archiveDocument(
      token,
      documentTypeId,
      uploadId,
      keywordsGuid,
    );

    // Get document meta data
    const documentMetaData = await getDocumentById(token, documentId);
    documentMetaData.size = size;
    documentMetaData.extension = fileExtension;
    documentMetaData.documentTypeId = documentTypeId;

    // Serialize document
    const serializedDocument = serializeDocument(documentMetaData, req);

    return res.status(201).send(serializedDocument);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { post };
