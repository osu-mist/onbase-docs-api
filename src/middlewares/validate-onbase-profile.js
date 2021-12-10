import config from 'config';
import _ from 'lodash';

import { errorBuilder } from 'errors/errors';

/**
 * Middleware that improves the error message when failing to parse JSON
 *
 * @type {ErrorRequestHandler}
 */
const validateOnbaseProfile = (req, res, next) => {
  const { onbaseProfiles } = config.get('dataSources.http');
  const onbaseProfile = req.headers['onbase-profile'];

  if (!_.has(onbaseProfiles, onbaseProfile)) {
    errorBuilder(res, 401, 'Unrecognized OnBase profile');
  } else {
    next();
  }
};

export { validateOnbaseProfile };
