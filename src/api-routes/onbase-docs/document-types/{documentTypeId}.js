import { errorBuilder } from 'errors/errors';
import { getAccessToken, getDocumentType } from '../../../db/http/onbase-dao';
import { serializeDocumentType } from '../../../serializers/document-types-serializer';

/**
 * Get document type by ID
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  const {
    headers,
    params: { documentTypeId },
  } = req;
  const onbaseProfile = headers['onbase-profile'];

  let result;

  // Get access token
  result = await getAccessToken(onbaseProfile);
  const token = result[0];
  const [, fbLb] = result;

  result = await getDocumentType(token, fbLb, null, documentTypeId);
  if (result instanceof Error) {
    return errorBuilder(res, 404, [result.message]);
  }

  // Serialize document type
  const serializedDocumentType = serializeDocumentType(result[0][0], req);

  return res.status(200).send(serializedDocumentType);
};

export { get };
