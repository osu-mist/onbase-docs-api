import { errorHandler } from 'errors/errors';
import { getAccessToken } from '../../db/http/connection';
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
    console.log(token);
    // const rawPet = await postPet(req.body);
    // const result = serializePet(rawPet, req);
    // res.set('Location', result.data.links.self);
    res.status(201).send('hello');
  } catch (err) {
    errorHandler(res, err);
  }
};

export { post };
