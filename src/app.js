import fs from 'fs';
import https from 'https';

import bodyParser from 'body-parser';
import { compose } from 'compose-middleware';
import config from 'config';
import express from 'express';
import { initialize } from 'express-openapi';
import _ from 'lodash';
import moment from 'moment';
import multer from 'multer';
import git from 'simple-git/promise';
import 'source-map-support/register';

import { errorBuilder, errorHandler } from 'errors/errors';
import { authentication } from 'middlewares/authentication';
import { bodyParserError } from 'middlewares/body-parser-error';
import { loggerMiddleware } from 'middlewares/logger';
import { runtimeErrors } from 'middlewares/runtime-errors';
import { validateBooleanParams } from 'middlewares/validate-boolean-params';
import { validateOnbaseProfile } from 'middlewares/validate-onbase-profile';
import { openapi } from 'utils/load-openapi';
import { validateDataSource } from 'utils/validate-data-source';

const serverConfig = config.get('server');
const { version, title } = openapi.info;

validateDataSource();

// Initialize Express applications and routers
const app = express();
const appRouter = express.Router();
const adminApp = express();
const adminAppRouter = express.Router();

/*
 * Use the simple query parser to prevent the parameters which contain square brackets be parsed as
 * a nested object
 */
app.set('query parser', 'simple');

// Create and start HTTPS servers
const httpsOptions = {
  key: fs.readFileSync(serverConfig.keyPath),
  cert: fs.readFileSync(serverConfig.certPath),
  secureProtocol: serverConfig.secureProtocol,
};
const httpsServer = https.createServer(httpsOptions, app);
const adminHttpsServer = https.createServer(httpsOptions, adminApp);

// Middlewares for routers, logger and authentication
const baseEndpoint = serverConfig.basePathPrefix;
app.use(baseEndpoint, appRouter);
adminApp.use(baseEndpoint, adminAppRouter);

appRouter.use(loggerMiddleware);
appRouter.use(authentication);
appRouter.use(validateBooleanParams);
appRouter.use(validateOnbaseProfile);
adminAppRouter.use(authentication);

/**
 * Function that transforms OpenAPI errors. The behavior is to apply all properties from the Ajv
 * error to the OpenAPI error.
 *
 * @param {object} openapiError OpenAPI error
 * @param {object} ajvError Ajv error
 * @returns {object} Transformed error
 */
const errorTransformer = (openapiError, ajvError) => {
  /**
   * express-openapi will add a leading '[' and closing ']' to the 'path' field if the parameter
   * name contains '[' or ']'. This regex is used to remove them to keep the path name consistent.
   *
   * @type {RegExp}
   */
  const pathQueryRegex = /\['(.*)'\]/g;

  const error = { ...openapiError, ...ajvError };

  const regexResult = pathQueryRegex.exec(error.path);
  error.path = regexResult ? regexResult[1] : error.path;
  return error;
};

// Return API meta information at admin endpoint
adminAppRouter.get(`/${version}`, async (req, res) => {
  try {
    const commit = await git().revparse(['--short', 'HEAD']);
    const now = moment();
    const info = {
      meta: {
        name: title,
        time: now.format('YYYY-MM-DD HH:mm:ssZZ'),
        unixTime: now.unix(),
        commit: commit.trim(),
        documentation: 'openapi.yaml',
      },
    };
    res.send(info);
  } catch (err) {
    errorHandler(res, err);
  }
});

// Initialize API with OpenAPI specification
initialize({
  app: appRouter,
  apiDoc: {
    ...openapi,
  },
  paths: 'dist/api-routes',
  consumesMiddleware: {
    'application/json': compose([bodyParser.json(), bodyParserError]),
    'multipart/form-data': (req, res, next) => {
      multer().any()(req, res, (err) => {
        if (err) return next(err);
        if (!req.files || !_.find(req.files, { fieldname: 'uploadedDocument' })) {
          return errorBuilder(res, 400, ['Filed uploadedDocument is required.']);
        }

        req.files.forEach((f) => {
          req.body[f.fieldname] = ''; // Set to empty string to satisfy OpenAPI spec validation
        });
        return next();
      });
    },
  },
  errorMiddleware: runtimeErrors,
  errorTransformer,
  promiseMode: true,
});

// Return a 404 error if resource not found
appRouter.use((req, res) => errorBuilder(res, 404, 'Resource not found.'));

// Start servers and listen on ports
httpsServer.listen(serverConfig.port);
adminHttpsServer.listen(serverConfig.adminPort);
