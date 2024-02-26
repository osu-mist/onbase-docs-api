import 'axios';
import chai from 'chai';
import chaiAsPromised from 'chai-as-promised';
import sinon from 'sinon';
import proxyquire from 'proxyquire';
import 'config';
import setCookie from 'set-cookie-parser';

import {
  docType1,
  fakeData,
  keyword1,
  keyword2,
  keywordType2,
  fakeKeywordsTypesData,
} from './mock-data';

import {
  createConfigStub,
  testResultWithCookie,
  testResultCookieOnly,
  createError,
  testResultData,
  testResultError,
  testResultThrowsMessage,
} from './test-helpers';

chai.should();
chai.use(chaiAsPromised);

/**
 * Run a suite of up to three tests:
 *  1) Tests that the getter returned an Error response object.
 *  2) Tests that the getter threw an Error exception that contained an object as the message.
 *  3) Tests that the getter threw an Error exception with the expected error message.
 *
 * @param {String} testLabel the string name of the getter
 * @param {object} getter the stubbed getter that makes a request for data
 * @param {object} errReturn a stubbed response that returns an error response
 * @param {object} errThrowObj a stubbed response that throws an object
 * @param {object} errThrowMsg a stubbed response that throws a message
 */
const runErrorSuite = (testLabel, getter, errReturn, errThrowObj, errThrowMsg) => {
  if (errReturn) {
    it(`${testLabel} request returns error ${errReturn.response.status}`,
      () => {
        const stubAxios = sinon.stub().rejects(errReturn);
        const onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
        testResultError(
          getter(onbaseDao),
          errReturn.response.data.detail,
          stubAxios,
        );
      });
  }
  it(`${testLabel} request throws ${errThrowObj.response.status}`,
    () => {
      const stubAxios = sinon.stub().rejects(errThrowObj);
      const onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
      testResultThrowsMessage(
        getter(onbaseDao),
        '[object Object]',
        stubAxios,
      );
    });
  it(`${testLabel} request throws ${errThrowMsg.response.status}`,
    () => {
      const stubAxios = sinon.stub().rejects(errThrowMsg);
      const onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
      testResultThrowsMessage(
        getter(onbaseDao),
        errThrowMsg.response.data.detail,
        stubAxios,
      );
    });
};

const error200 = createError(200);
const error201 = createError(201);
const error204 = createError(204);
const error400 = createError(400);
const error404 = createError(404);
const error413 = createError(413);

describe('Test onbase-dao (non-keyword types)', () => {
  let stubAxios;
  let onbaseDao;

  beforeEach(() => {
    createConfigStub();
    stubAxios = sinon.stub().callsFake(() => fakeData);
    onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
    sinon.stub(setCookie, 'parse').returns([{
      name: 'FB_LB',
      value: 'fake cookie',
      path: '/',
      httpOnly: true,
      secure: true,
      sameSite: 'none',
    }]);
  });
  afterEach(() => sinon.restore());

  // it('getAccessToken for valid profile should return a valid token',
  //   () => testResultWithCookie(
  //     onbaseDao.getAccessToken('testprofile'),
  //     'fake token',
  //     stubAxios,
  //   ));
  // runErrorSuite('getAccessToken',
  //   (dao) => dao.getAccessToken('testprofile'),
  //   undefined, error200, error413);

  it('initiateStagingArea for valid profile should result',
    () => testResultWithCookie(
      onbaseDao.initiateStagingArea('fake token', 'fake fbLb', 'PDF', 1024),
      fakeData.data,
      stubAxios,
    ));
  runErrorSuite('initiateStagingArea',
    (dao) => dao.initiateStagingArea('fake token', 'fake fbLb', 'PDF', 1024),
    undefined, error201, error413);

  it('uploadFile should return a valid result',
    () => testResultCookieOnly(
      onbaseDao.uploadFile('fake token', 'fake fbLb', 'fake', 'fake', 'fake', 'fake'),
      stubAxios,
    ));
  runErrorSuite('uploadFile',
    (dao) => dao.uploadFile('fake token', 'fake fbLb', 'fake', 'fake', 'fake', 'fake'),
    undefined, error204, error200);
  it('uploadFile request returns error 413',
    () => {
      stubAxios = sinon.stub().rejects(error413);
      onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
      testResultError(
        onbaseDao.uploadFile('fake token', 'fake fbLb', 'fake', 'fake', 'fake', 'fake'),
        '[object Object]',
        stubAxios,
      );
    });

  it('getDefaultKeywordsGuid should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDefaultKeywordsGuid('fake token', 'fake fbLb', 'fake'),
      'fake guid',
      stubAxios,
    ));
  runErrorSuite('getDefaultKeywordsGuid',
    (dao) => dao.getDefaultKeywordsGuid('fake token', 'fake fbLb', 'fake'),
    error404, error200, error201);

  it('postIndexingModifiers should return a valid result',
    () => testResultWithCookie(
      onbaseDao.postIndexingModifiers('fake token', 'fake fbLb', 'fake', 'fake', 'fake'),
      'fake keyword collection',
      stubAxios,
    ));
  runErrorSuite('postIndexingModifiers',
    (dao) => dao.postIndexingModifiers('fake token', 'fake fbLb', 'fake', 'fake', 'fake'),
    error400, error201, error200);

  it('archiveDocument should return a valid result',
    () => testResultWithCookie(
      onbaseDao.archiveDocument('fake token', 'fake fbLb',
        'fake', 'dummy id', 'fake keyword collection'),
      'fake id',
      stubAxios,
    ));
  runErrorSuite('archiveDocument',
    (dao) => dao.archiveDocument('fake token', 'fake fbLb',
      'fake', 'dummy id', 'fake keyword collection'),
    undefined, error201, error200);

  it('getDocumentById should return a valid result',
    () => testResultData(
      onbaseDao.getDocumentById('fake token', 'fake fbLb', 'dummy id'),
      fakeData.data,
      stubAxios,
    ));
  runErrorSuite('getDocumentById',
    (dao) => dao.getDocumentById('fake token', 'fake fbLb', 'dummy id'),
    error404, error200, error201);

  it('getDocumentKeywords should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentKeywords('fake token', 'fake fbLb',
        'fake', 'dummy id', 'fake keyword collection'),
      fakeData.data,
      stubAxios,
    ));
  runErrorSuite('getDocumentKeywords',
    (dao) => dao.getDocumentKeywords('fake token', 'fake fbLb',
      'fake', 'dummy id', 'fake keyword collection'),
    error404, error200, error201);

  it('patchDocumentKeywords should return a valid result',
    () => testResultWithCookie(
      onbaseDao.patchDocumentKeywords('fake token', 'fake fbLb',
        'fake',
        { items: [[keyword1, keyword2]] },
        [keyword1, keyword2]),
      fakeData.data,
      stubAxios,
    ));
  runErrorSuite('patchDocumentKeywords',
    (dao) => dao.patchDocumentKeywords('fake token', 'fake fbLb',
      'fake',
      { items: [[keyword1, keyword2]] },
      [keyword1, keyword2]),
    undefined, error200, error201);

  it('getDocumentContent should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentContent('fake token', 'fake fbLb', 'dummy id'),
      fakeData,
      stubAxios,
    ));
  runErrorSuite('getDocumentContent',
    (dao) => dao.getDocumentContent('fake token', 'fake fbLb', 'dummy id'),
    error404, error200, error201);

  it('getDocumentType should return a valid result', () => testResultWithCookie(
    onbaseDao.getDocumentType('fake token', 'fake fbLb', docType1.id, null),
    docType1.id,
    stubAxios,
  ));
  runErrorSuite(
    'getDocumentType',
    (dao) => dao.getDocumentType('fake token', 'fake fbLb', docType1.id, null),
    undefined,
    error200,
    error201,
  );

  it('getDocumentType more than one document type should fail', () => {
    fakeData.data.items = [docType1, docType1];

    testResultError(
      onbaseDao.getDocumentType('fake token', 'fake fbLb', docType1.id, null),
      'More than one document types matched.',
      stubAxios,
    );
  });

  it('getDocumentType no document type should fail', () => {
    fakeData.data.items = [];

    testResultError(
      onbaseDao.getDocumentType('fake token', 'fake fbLb', docType1.id, null),
      'Please provide a valid document type.',
      stubAxios,
    );
  });

  it('createQuery should return a valid result',
    () => testResultWithCookie(
      onbaseDao.createQuery('fake token', 'fake fbLb', 'fake 1', 'fake 1', 'fake 1', 'fake 1'),
      'fake id',
      stubAxios,
    ));
  runErrorSuite('createQuery',
    (dao) => dao.createQuery('fake token', 'fake fbLb', 'fake 1', 'fake 1', 'fake 1', 'fake 1'),
    undefined, error200, error201);
  it('createQuery request returns error 999',
    () => {
      const error = createError(999);
      error.response.data.detail = 'Some of the provided input data is invalid';
      stubAxios = sinon.stub().rejects(error);
      onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
      testResultError(
        onbaseDao.createQuery('fake token', 'fake fbLb', 'fake 1', 'fake 1', 'fake 1', 'fake 1'),
        '[object Object]',
        stubAxios,
      );
    });

  it('getQueryResults should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getQueryResults('fake token', 'fake fbLb', 'fake id'),
      fakeData,
      stubAxios,
    ));
  runErrorSuite('getQueryResults',
    (dao) => dao.getQueryResults('fake token', 'fake fbLb', 'fake id'),
    undefined, error200, error201);

  it('getDocumentsByIds should return a valid result',
    () => testResultData(
      onbaseDao.getDocumentsByIds('fake token', 'fake fbLb', 'fake id'),
      fakeData.data,
      stubAxios,
    ));
  runErrorSuite('getDocumentsByIds',
    (dao) => dao.getDocumentsByIds('fake token', 'fake fbLb', 'fake id'),
    undefined, error200, error201);
});

describe('Test onbase-dao (keyword types)', () => {
  let stubAxios;
  let onbaseDao;

  beforeEach(() => {
    createConfigStub();
    stubAxios = sinon.stub().callsFake(() => fakeKeywordsTypesData);
    onbaseDao = proxyquire('db/http/onbase-dao', { axios: stubAxios });
    sinon.stub(setCookie, 'parse').returns([{
      name: 'FB_LB',
      value: 'fake cookie',
      path: '/',
      httpOnly: true,
      secure: true,
      sameSite: 'none',
    }]);
  });
  afterEach(() => sinon.restore());

  it('getDocumentKeywordTypes should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentKeywordTypes('fake token', 'fake fbLb', [keyword1, keyword2]),
      [keyword1, keyword2],
      stubAxios,
    ));
  runErrorSuite('getDocumentKeywordTypes',
    (dao) => dao.getDocumentKeywordTypes('fake token', 'fake fbLb', [keyword1, keyword2]),
    error404, error200, error201);

  it('getKeywordTypesByNames should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getKeywordTypesByNames('fake token', 'fake fbLb',
        {
          keywordTypeNames: [keywordType2.name],
          keywordValues: ['0'],
        }),
      { KeywordType2: { value: '0', id: keywordType2.id } },
      stubAxios,
    ));
  runErrorSuite('getKeywordTypesByNames',
    (dao) => dao.getKeywordTypesByNames('fake token', 'fake fbLb',
      {
        keywordTypeNames: [keywordType2.name],
        keywordValues: ['0'],
      }),
    undefined, error200, error201);
});
