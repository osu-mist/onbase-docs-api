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

    let result;

    // Get access token
    result = await getAccessToken(onbaseProfile);
    const [token, fbLb] = result;

    // Get document content
    result = await getDocumentContent(token, fbLb, documentId);
    const { headers: documentContentHeaders, data: documentContent } = result[0];
    res.setHeader('Content-Type', documentContentHeaders['content-type']);

    return res.status(200).send(documentContent);
  } catch (err) {
    return errorHandler(res, err);
  }
};

export { get };
