import config from 'config';
import axios from 'axios';

import { logger } from 'utils/logger';

const { baseUri, apiServer } = config.get('dataSources.http');

/**
 * Validate http connection and throw an error if invalid
 *
 * @returns {Promise} resolves if http connection can be established and rejects otherwise
 */
const validateHttp = async () => {
  try {
    await axios.get(`${baseUri}/app/${apiServer}/onbase/workflow/healthcheck`);
  } catch (err) {
    logger.error(err);
    throw new Error('Unable to connect to HTTP data source');
  }
};

export { validateHttp };
