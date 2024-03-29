import { errorBuilder } from 'errors/errors';
import { parseQuery } from 'utils/parse-query';
import {
  getDocumentType,
  getAccessToken,
} from '../../db/http/onbase-dao';
import { serializeDocumentTypes } from '../../serializers/document-types-serializer';

/**
 * Get document types
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  const { query, headers } = req;
  const onbaseProfile = headers['onbase-profile'];
  const parsedQuery = parseQuery(query);
  const { documentTypeName } = parsedQuery;

  let result;

  // Get access token
  result = await getAccessToken(onbaseProfile);
  const token = result[0];
  const [, fbLb] = result;

  result = await getDocumentType(token, fbLb, documentTypeName, null);
  if (result instanceof Error) {
    return errorBuilder(res, 400, [result.message]);
  }

  // Serialize document type
  const serializedDocumentTypes = serializeDocumentTypes(result[0], query);

  return res.status(200).send(serializedDocumentTypes);
};

export { get };
