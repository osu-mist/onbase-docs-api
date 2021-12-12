import _ from 'lodash';

import { errorBuilder, errorHandler } from 'errors/errors';
import {
  getAccessToken,
  initiateStagingArea,
  uploadFile,
  getKeywordsGuid,
} from '../../db/http/onbase-dao';
// import { serializePet, serializePets } from '../serializers/pets-serializer';

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
    const token = await getAccessToken(onbaseProfile, res);

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

    return res.status(201).send('hello');
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { post };
