import _ from 'lodash';

import { errorHandler } from 'errors/errors';
import { getAccessToken, initiateStagingArea, uploadFile } from '../../db/http/onbase-dao';
// import { serializePet, serializePets } from '../serializers/pets-serializer';

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    const { files, headers } = req;
    const onbaseProfile = headers['onbase-profile'];

    // Upload document information from form data
    const uploadedDocument = _.find(files, { fieldname: 'uploadedDocument' });
    const { size, buffer, mimetype } = uploadedDocument;

    // Get access token
    const token = await getAccessToken(onbaseProfile, res);

    // Prepare staging area
    const { id: uploadId, numberOfParts } = await initiateStagingArea(token, mimetype, size);

    // Upload file
    // TODO: numberOfParts logic handlers
    await uploadFile(token, uploadId, numberOfParts, mimetype, buffer);

    // const rawPet = await postPet(req.body);
    // const result = serializePet(rawPet, req);
    // res.set('Location', result.data.links.self);
    res.status(201).send('hello');
  } catch (err) {
    errorHandler(res, err);
  }
};

export { post };
