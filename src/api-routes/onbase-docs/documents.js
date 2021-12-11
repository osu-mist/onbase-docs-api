import { errorHandler } from 'errors/errors';
import { getAccessToken, initiateStagingArea } from '../../db/http/onbase-dao';
// import { serializePet, serializePets } from '../serializers/pets-serializer';

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    const onbaseProfile = req.headers['onbase-profile'];
    const token = await getAccessToken(onbaseProfile, res);

    const uploadId = await initiateStagingArea(token, 'pdf', 9848);

    console.log(uploadId);
    // const rawPet = await postPet(req.body);
    // const result = serializePet(rawPet, req);
    // res.set('Location', result.data.links.self);
    res.status(201).send('hello');
  } catch (err) {
    errorHandler(res, err);
  }
};

export { post };
