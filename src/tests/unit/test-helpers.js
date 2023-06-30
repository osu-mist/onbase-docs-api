import { expect } from 'chai';
import config from 'config';
import _ from 'lodash';
import proxyquire from 'proxyquire';
import sinon from 'sinon';

import { logger } from 'utils/logger';
import { fakeBaseUrl, fakeOsuId, fakeDocument } from './mock-data';

/**
 * Creates sinon stub for oracledb connection
 *
 * @param {object} dbReturn value to be returned by connection.execute()
 * @returns {Promise<object>} sinon stub for connection
 */
const getConnectionStub = (dbReturn) => sinon.stub().resolves({
  execute: () => dbReturn,
  close: () => null,
  commit: () => null,
  rollback: () => null,
});

/**
 * Creates a proxy for the dao file being tested
 *
 * @param {string} daoPath relative path to dao file
 * @param {object} dbReturn value to be returned by connection.execute()
 * @returns {Promise<object>} stubbed oracledb connection object
 */
const createDaoProxy = (daoPath, dbReturn) => proxyquire(daoPath, {
  './connection': {
    getConnection: getConnectionStub(dbReturn),
  },
});

/**
 * Creates stub for config that works for both connection and uri-builder
 *
 * @returns {object} config stub
 */
const createConfigStub = () => {
  const dataSources = {
    http: {
      baseUri: 'fake',
      apiServer: 'fake',
      idpServer: 'fake',
      tenant: 'fake',
      clientId: 'fake',
      clientSecret: 'fake',
      onbaseProfiles: {
        testprofile: {
          username: 'fake',
          password: 'fake',
        },
      },
      documentIndexKeyTypeId: 'fake',
    },
  };
  return sinon.stub(config, 'get').returns(dataSources.http);
};

/**
 * Handles stubbing for dao unit tests
 */
const daoBeforeEach = () => {
  createConfigStub();

  sinon.stub(logger, 'error').returns(null);
};

/**
 * Creates resource schema for expected test results
 *
 * @param {string} resourceType type of resource as named in openapi
 * @param {object} resourceAttributes fields expected in attributes subset of resourceType
 * @returns {object} expected schema of serialized resource
 */
const resourceSubsetSchema = (resourceType, resourceAttributes) => {
  const fakeUrl = `${fakeBaseUrl}/${resourceType}s/fakeOsuId`;
  const schema = {
    links: {
      self: fakeUrl,
    },
    data: {
      id: fakeOsuId,
      type: resourceType,
      links: { self: fakeUrl },
    },
  };
  if (resourceAttributes) {
    schema.data.attributes = resourceAttributes;
  }
  return schema;
};

/**
 * Helper function for lite-testing single resource
 *
 * @param {object} serializedResource serialized resource
 * @param {string} resourceType resource type
 * @param {object} nestedProps object containing properties nested under data.attributes
 */
const testSingleResource = (serializedResource, resourceType, nestedProps) => {
  expect(serializedResource).to.have.all.keys(resourceSubsetSchema(resourceType, nestedProps));

  if (nestedProps) {
    _.forEach(Object.keys(nestedProps), (prop) => {
      expect(serializedResource).to.have.nested.property(`data.attributes.${prop}`);
    });
  }
};

/**
 * Helper function for lite-testing multiple resources
 *
 * @param {object} serializedResources serialized resources
 * @returns {object} data object from serialized resources for further use
 */
const testMultipleResources = (serializedResources) => {
  const serializedResourcesData = serializedResources.data;
  expect(_.omit(serializedResources, 'meta')).to.have.keys('data', 'links');
  expect(serializedResourcesData).to.be.an('array');

  return serializedResourcesData;
};

/**
 * Helper function for creating an error response for a web request.
 *
 * @param {number} code the HTTP status code of the error
 * @returns {object} an error response object
 */
const createError = (code) => ({
  response: {
    status: code,
    data: {
      error: `${code} error`,
      error_description: `${code} error message`,
      detail: `${code} error message`,
    },
  },
});

/**
 * Helper function for testing that the result contains data
 * and a cookie.
 *
 * @param {object} result a result to validate
 * @param {object} data the expected result data
 * @param {object} stub the axios stub used for web requests
 */
const testResultWithCookie = (result, data, stub) => {
  result.should
    .eventually.be.fulfilled
    .and.deep.equals([data, 'fake cookie'])
    .and.to.have.length(2);
  sinon.assert.calledOnce(stub);
};

/**
 * Helper function for testing that the result contains only a cookie.
 *
 * @param {object} result a result to validate
 * @param {object} stub the axios stub used for web requests
 */
const testResultCookieOnly = (result, stub) => {
  result.should
    .eventually.be.fulfilled
    .and.deep.equals(['fake cookie'])
    .and.to.have.length(1);
  sinon.assert.calledOnce(stub);
};

/**
 * Helper function for testing that the result contains data
 * but no cookie.
 *
 * @param {object} result a result to validate
 * @param {object} data the expected result data
 * @param {object} stub the axios stub used for web requests
 */
const testResultData = (result, data, stub) => {
  result.should
    .eventually.be.fulfilled
    .and.deep.equals(data);
  sinon.assert.calledOnce(stub);
};

/**
 * Helper function for testing that the result was an error
 * with the specified message.
 *
 * @param {object} result a result to validate
 * @param {string} msg the expected error message
 * @param {object} stub the axios stub used for web requests
 */
const testResultError = (result, msg, stub) => {
  result.should
    .eventually.be.fulfilled
    .and.be.instanceOf(Error)
    .and.have.property('message', msg);
  sinon.assert.calledOnce(stub);
};

/**
 * Helper function for testing that the result was an exception thrown
 * with the specified message.
 *
 * @param {object} result a result to validate
 * @param {string} msg the expected error message
 * @param {object} stub the axios stub used for web requests
 */
const testResultThrowsMessage = (result, msg, stub) => {
  result.should
    .eventually.be.rejected
    .and.be.instanceOf(Error)
    .and.have.property('message', msg);
  sinon.assert.calledOnce(stub);
};

/**
 * Helper function for testing that the result was an exception thrown
 * with an error object.
 *
 * @param {object} result a result to validate
 * @param {object} stub the axios stub used for web requests
 */
const testResultThrows = (result, stub) => {
  result.should
    .eventually.be.rejected
    .and.be.instanceOf(Error);
  sinon.assert.calledOnce(stub);
};

/**
 * Run a suite of up to two serializer tests:
 *  1) Tests that using HTTP POST returns a single resource.
 *  2) Tests that using HTTP GET returns a single resource.
 *
 * @param {String} testLabel the string name of the getter
 * @param {String} typeLabel the string name of the data returned by the serializer
 * @param {object} serializer the stubbed serializer
 */
const runSerializerSuite = (testLabel, typeLabel, serializer) => {
  it(`${testLabel} should properly serialize ${typeLabel} (POST) in JSON API format`, () => {
    const result = serializer(fakeDocument, { method: 'POST', query: 'fake' });
    return testSingleResource(result);
  });
  it(`${testLabel} should properly serialize ${typeLabel} (GET) in JSON API format`, () => {
    const result = serializer(fakeDocument, { method: 'GET', query: 'fake' });
    return testSingleResource(result);
  });
};

export {
  createDaoProxy,
  testSingleResource,
  testMultipleResources,
  getConnectionStub,
  daoBeforeEach,
  createConfigStub,
  createError,
  testResultWithCookie,
  testResultCookieOnly,
  testResultData,
  testResultError,
  testResultThrowsMessage,
  testResultThrows,
  runSerializerSuite,
};
