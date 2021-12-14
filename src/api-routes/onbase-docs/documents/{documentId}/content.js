import { errorHandler } from 'errors/errors';

import {
  getAccessToken,
  getDocumentContent,
} from '../../../../db/http/onbase-dao';

/**
 * Get document content
 *
 * @type {RequestHandler}
 */
const get = async (req, res) => {
  try {
    const { headers, params: { documentId } } = req;
    const onbaseProfile = headers['onbase-profile'];

    // Get access token
    const token = await getAccessToken(onbaseProfile);

    // Get document content
    const {
      headers: documentContentHeaders,
      data: documentContent,
    } = await getDocumentContent(token, documentId);
    res.setHeader('Content-Type', documentContentHeaders['content-type']);

    return res.status(200).send(documentContent);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get };
