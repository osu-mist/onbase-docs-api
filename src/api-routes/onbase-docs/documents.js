import { errorHandler } from 'errors/errors';
// import { getPets, postPet } from '../db/json/pets-dao-example';
// import { serializePet, serializePets } from '../serializers/pets-serializer';

/**
 * Post document
 *
 * @type {RequestHandler}
 */
const post = async (req, res) => {
  try {
    // const rawPet = await postPet(req.body);
    // const result = serializePet(rawPet, req);
    // res.set('Location', result.data.links.self);
    res.status(201).send('hello');
  } catch (err) {
    errorHandler(res, err);
  }
};

export { post };
