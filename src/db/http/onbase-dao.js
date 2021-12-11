import axios from 'axios';
import config from 'config';
import FormData from 'form-data';

import { logger } from 'utils/logger';

const {
  baseUri,
  apiServer,
  idpServer,
  tenant,
  clientId,
  clientSecret,
  onbaseProfiles,
} = config.get('dataSources.http');

const onbaseIdpUrl = `${baseUri}/app/${idpServer}`;
const onbaseDocumentsUrl = `${baseUri}/app/${apiServer}/onbase/core/documents`;

/**
 * Get API access token from OnBase IDP server
 *
 * @param {string} onbaseProfile OnBase profile name
 * @returns {Promise} resolves if fetched access token and rejects otherwise
 */
const getAccessToken = async (onbaseProfile) => {
  try {
    const { username, password } = onbaseProfiles[onbaseProfile];

    const formData = new FormData();
    formData.append('grant_type', 'password');
    formData.append('scope', 'evolution');
    formData.append('tenant', tenant);
    formData.append('client_id', clientId);
    formData.append('client_secret', clientSecret);
    formData.append('username', username);
    formData.append('password', password);

    const res = await axios.post(`${onbaseIdpUrl}/connect/token`, formData, {
      headers: formData.getHeaders(),
    });

    return res.data.access_token;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status === 400) {
      logger.error(err.response.data.error);
      throw new Error(err.response.data.error_description);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded.
 * @param {string} token access token
 * @param {string} mimeType media type
 * @param {number} fileSize file size
 * @returns {Promise} resolves if staging area initialized and rejects otherwise
 */
const initiateStagingArea = async (token, mimeType, fileSize) => {
  try {
    const reqConfig = { headers: { Authorization: `Bearer ${token}` } };
    const body = { fileExtension: /[^/]*$/.exec(mimeType)[0], fileSize };

    const res = await axios.post(`${onbaseDocumentsUrl}/uploads`, body, reqConfig);

    return res.data;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded.
 * @param {string} token access token
 * @param {string} uploadId the unique reference to the file being uploaded
 * @param {number} filePart part number of the file to upload
 * @param {string} mimeType media type
 * @param {object} fileBuffer binary content for file upload
 * @returns {Promise} resolves if file uploaded and rejects otherwise
 */
const uploadFile = async (token, uploadId, filePart, mimeType, fileBuffer) => {
  try {
    const reqConfig = {
      method: 'put',
      url: `${onbaseDocumentsUrl}/uploads/${uploadId}`,
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': mimeType,
      },
      params: { filePart },
      data: fileBuffer,
    };

    const res = await axios(reqConfig);

    return res;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 204) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

export { getAccessToken, initiateStagingArea, uploadFile };