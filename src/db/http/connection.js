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

const onbaseWorkflowUrl = `${baseUri}/app/${apiServer}/onbase/workflow`;
const onbaseIdpUrl = `${baseUri}/app/${idpServer}`;

/**
 * Get API access token from OnBase IDP server
 * @param {string} onbaseProfile OnBase profile name
 *
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

    const res = await axios.post(
      `${onbaseIdpUrl}/connect/token`,
      formData,
      { headers: formData.getHeaders() },
    );

    return res.data.access_token;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status === 400) {
      throw new Error('OnBase server login failed');
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Validate http connection and throw an error if invalid
 *
 * @returns {Promise} resolves if http connection can be established and rejects otherwise
 */
const validateHttp = async () => {
  try {
    const res = await axios.get(`${onbaseWorkflowUrl}/healthcheck`);
    if (res.status !== 200) {
      throw new Error('Health check failed');
    }
  } catch (err) {
    logger.error(err);
    throw new Error('Unable to connect to HTTP data source');
  }
};

export { getAccessToken, validateHttp };
