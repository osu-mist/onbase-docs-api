import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import {
  getAccessToken,
  getKeywordsGuid,
  initiateStagingArea,
  uploadFile,
  archiveDocument,
} from '../../db/http/onbase-dao';
// import { serializePet, serializePets } from '../serializers/pets-serializer';

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    const { files, headers, body: { documentTypeId, fileTypeId } } = req;
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
    const { id: uploadId, numberOfParts } = await initiateStagingArea(token, mimetype, size);

    // Upload file
    // TODO: numberOfParts logic handlers
    await uploadFile(token, uploadId, numberOfParts, mimetype, buffer);

    // Archive document
    const documentId = await archiveDocument(
      token,
      documentTypeId,
      fileTypeId,
      uploadId,
      keywordsGuid,
    );

    return res.status(201).send(documentId);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { post };
