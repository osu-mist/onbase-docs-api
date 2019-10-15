import { errorBuilder, errorHandler } from 'errors/errors';
import { getPetById } from '../../db/json/pets-dao-example';
import { serializePet } from '../../serializers/pets-serializer';

/**
 * Get pet by unique ID
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  try {
    const { id } = req.params;
    const rawPet = await getPetById(id);
    const result = serializePet(rawPet, req);
    if (!result) {
      errorBuilder(res, 404, 'A pet with the specified ID was not found.');
    } else {
      res.send(result);
    }
  } catch (err) {
    errorHandler(res, err);
  }
};

export { get };
