import { errorBuilder, errorHandler } from 'errors/errors';

import {
  getAccessToken,
  getDocumentKeywords,
  patchDocumentKeywords,
} from '../../../../db/http/onbase-dao';
import { serializeKeywords } from '../../../../serializers/keywords-serializer';

/**
 * Get document keywords
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  try {
    const { headers, params: { documentId } } = req;
    const onbaseProfile = headers['onbase-profile'];

    // Get access token
    const [token, fbLb] = await getAccessToken(onbaseProfile);

    // Get current keyword collection
    const [currentKeywordCollection] = await getDocumentKeywords(token, fbLb, documentId);
    if (currentKeywordCollection instanceof Error) {
      return errorBuilder(res, 404, currentKeywordCollection.message);
    }

    // Serialize keywords
    currentKeywordCollection.id = documentId;
    currentKeywordCollection.keywords = currentKeywordCollection.items[0].keywords;

    const serializedKeywords = serializeKeywords(currentKeywordCollection, req);

    return res.status(200).send(serializedKeywords);
  } catch (err) {
    return errorHandler(res, err);
  }
};

/**
 * Patch document keywords
 *
 * @type {RequestHandler}
 */
const patch = async (req, res) => {
  try {
    const { headers, params: { documentId }, body: { keywords: newKeywords } } = req;
    const onbaseProfile = headers['onbase-profile'];

    let result;
    // the load balancer cookie (FB_LB) is updated after every request
    let fbLb;
    // Get access token
    result = await getAccessToken(onbaseProfile);
    const token = result[0];
    [, fbLb] = result;

    // Get current keyword collection
    result = await getDocumentKeywords(token, fbLb, documentId);
    const currentKeywordCollection = result[0];
    [, fbLb] = result;

    if (currentKeywordCollection instanceof Error) {
      return errorBuilder(res, 404, currentKeywordCollection.message);
    }

    // Update document keywords
    result = await patchDocumentKeywords(
      token,
      fbLb,
      documentId,
      currentKeywordCollection,
      newKeywords,
    );
    [, fbLb] = result;

    // Get updated keyword collection
    result = await getDocumentKeywords(token, fbLb, documentId);
    const updatedKeywordCollection = result[0];
    if (updatedKeywordCollection instanceof Error) {
      throw new Error('Document missing after update.');
    }

    // Serialize keywords
    updatedKeywordCollection.id = documentId;
    updatedKeywordCollection.keywords = updatedKeywordCollection.items[0].keywords;

    const serializedKeywords = serializeKeywords(updatedKeywordCollection, req);

    return res.status(200).send(serializedKeywords);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get, patch };
