import { errorBuilder, errorHandler } from 'errors/errors';
import { getAccessToken, getDocumentById } from '../../../db/http/onbase-dao';
import { serializeDocument } from '../../../serializers/documents-serializer';

/**
 * Get document metadata by ID
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  try {
    const { headers, params: { documentId } } = req;
    const onbaseProfile = headers['onbase-profile'];

    // Get access token
    const [token, fbLb] = await getAccessToken(onbaseProfile);

    // Get document metadata
    const documentMetadata = await getDocumentById(token, fbLb, documentId);
    if (documentMetadata instanceof Error) {
      return errorBuilder(res, 404, documentMetadata.message);
    }

    // Serialize document
    const serializedDocument = serializeDocument(documentMetadata, req);

    return res.status(200).send(serializedDocument);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get };
