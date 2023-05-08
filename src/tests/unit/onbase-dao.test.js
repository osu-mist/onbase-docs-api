import axios from 'axios';
import chai from 'chai';
import chaiAsPromised from 'chai-as-promised';
import sinon from 'sinon';
import proxyquire from 'proxyquire'
import config from 'config';
import setCookie from 'set-cookie-parser';
import { createConfigStub } from './test-helpers';

chai.should();
chai.use(chaiAsPromised);

describe('Test emails-dao', () => {
  const docType1 = {
    id: '24680',
    name: 'TestDocType',
    typeId: '924680',
  };
  const fakeData = { data: {
    access_token: 'fake token',
    keywordGuid: 'fake guid',
    keywordCollection: 'fake keyword collection',
    id: 'fake id',
    items: [ docType1 ]
  } };

  const keyword1 = {
    name: 'Keyword1',
    typeId: '924680',
    keywordTypeId: '924680',
  };
  const keyword2 = {
    name: 'Keyword2',
    typeId: '9123456',
    keywordTypeId: '9123456',
  };
  const keywordType1 = {
    id: '924680',
    name: 'KeywordType1',
  };
  const keywordType2 = {
    id: '9123456',
    name: 'KeywordType2',
  };
  const fakeKeywordsTypesData = { data: {
    access_token: 'fake token',
    keywordGuid: 'fake guid',
    keywordCollection: 'fake keyword collection',
    id: 'fake id',
    items: [ keywordType1, keywordType2 ]
  } };

  let stubAxios;
  let onbaseDao;
  let stubAxios2;
  let onbaseDao2;
  let setCookieStub;

  beforeEach(() => {
    createConfigStub('dataSources.http');
    stubAxios = sinon.stub().callsFake(
      () => { return fakeData });
    onbaseDao = proxyquire('db/http/onbase-dao', {
      'axios': stubAxios,
    });
    stubAxios2 = sinon.stub().callsFake(
      () => { return fakeKeywordsTypesData });
    onbaseDao2 = proxyquire('db/http/onbase-dao', {
      'axios': stubAxios2,
    });
    setCookieStub = sinon.stub(setCookie, 'parse').returns([{
        name: 'FB_LB',
        value: 'fake cookie',
        path: '/',
        httpOnly: true,
        secure: true,
        sameSite: 'none'
    }]);
  });
  afterEach(() => sinon.restore());

  const testResultWithCookie = (result, data, stub) => {
    result.should
      .eventually.be.fulfilled
      .and.deep.equals([ data, 'fake cookie' ])
      .and.to.have.length(2);
    sinon.assert.calledOnce(stub);
  };
  const testResultCookieOnly = (result, stub) => {
    result.should
      .eventually.be.fulfilled
      .and.deep.equals([ 'fake cookie' ])
      .and.to.have.length(1);
    sinon.assert.calledOnce(stub);
  };
  const testResultData = (result, stub) => {
    result.should
      .eventually.be.fulfilled
      .and.deep.equals( fakeData.data );
    sinon.assert.calledOnce(stub);
  };
  const testResultError = (result, msg, stub) => {
    result.should
      .eventually.be.fulfilled
      .and.have.property('message', msg);
    sinon.assert.calledOnce(stub);
  };

  it('getAccessToken for valid profile should return a valid token',
    () => testResultWithCookie(
      onbaseDao.getAccessToken('testprofile'),
      'fake token',
      stubAxios));
  it('initiateStagingArea for valid profile should result',
    () => testResultWithCookie(
      onbaseDao.initiateStagingArea('fake token', 'fake fbLb', 'PDF', 1024),
      fakeData.data,
      stubAxios));
  it('uploadFile should return a valid result',
    () => testResultCookieOnly(
      onbaseDao.uploadFile('fake token', 'fake fbLb',
        'fake', 'fake', 'fake', 'fake'),
      stubAxios));
  it('getDefaultKeywordsGuid should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDefaultKeywordsGuid('fake token', 'fake fbLb', 'fake'),
      'fake guid',
      stubAxios));
  it('postIndexingModifiers should return a valid result',
    () => testResultWithCookie(
      onbaseDao.postIndexingModifiers('fake token', 'fake fbLb',
        'fake', 'fake', 'fake'),
        'fake keyword collection',
        stubAxios));
  it('archiveDocument should return a valid result',
    () => testResultWithCookie(
      onbaseDao.archiveDocument('fake token', 'fake fbLb',
        'fake', 'dummy id', 'fake keyword collection'),
        'fake id',
        stubAxios));
  it('getDocumentById should return a valid result',
    () => testResultData(
      onbaseDao.getDocumentById('fake token', 'fake fbLb', 'dummy id'),
      stubAxios));
  it('getDocumentKeywords should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentKeywords('fake token', 'fake fbLb',
        'fake', 'dummy id', 'fake keyword collection'),
        fakeData.data,
        stubAxios));
  it('getDocumentKeywordTypes should return a valid result',
    () => testResultWithCookie(
      onbaseDao2.getDocumentKeywordTypes('fake token', 'fake fbLb',
        [ keyword1, keyword2 ]),
        [ keyword1, keyword2 ],
        stubAxios2));
  it('patchDocumentKeywords should return a valid result',
    () => testResultWithCookie(
      onbaseDao.patchDocumentKeywords('fake token', 'fake fbLb',
        'fake',
        { items: [ [keyword1, keyword2] ] },
        [keyword1, keyword2]),
        fakeData.data,
        stubAxios));
  it('getDocumentContent should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentContent('fake token', 'fake fbLb', 'dummy id'),
        fakeData,
        stubAxios));
  it('getDocumentTypeByName should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getDocumentTypeByName('fake token', 'fake fbLb', docType1.id),
      docType1.id,
      stubAxios));
  it('getDocumentTypeByName more than one document type should fail',
    () => {
      fakeData.data.items = [ docType1, docType1 ];  

      testResultError(
        onbaseDao.getDocumentTypeByName(
          'fake token', 'fake fbLb', docType1.id),
        'More than one document types matched.',
        stubAxios);      
    });
  it('getDocumentTypeByName no document type should fail',
  () => {
    fakeData.data.items = [  ];  

    testResultError(
      onbaseDao.getDocumentTypeByName(
        'fake token', 'fake fbLb', docType1.id),
      'Please provide a valid document type.',
      stubAxios);      
  });
  it('getKeywordTypesByNames should return a valid result',
    () => testResultWithCookie(
      onbaseDao2.getKeywordTypesByNames('fake token', 'fake fbLb',
        {
          keywordTypeNames: [ keywordType2.name ],
          keywordValues: [ '0' ],
        }),
        { KeywordType2: { value: '0', id: keywordType2.id } },
        stubAxios2));
  it('createQuery should return a valid result',
    () => testResultWithCookie(
      onbaseDao.createQuery('fake token', 'fake fbLb', 'fake 1', 'fake 1', 'fake 1', 'fake 1'),
      'fake id',
      stubAxios));
  it('getQueryResults should return a valid result',
    () => testResultWithCookie(
      onbaseDao.getQueryResults('fake token', 'fake fbLb', 'fake id'),
      fakeData,
      stubAxios));
  it('getDocumentsByIds should return a valid result',
    () => testResultData(
      onbaseDao.getDocumentsByIds('fake token', 'fake fbLb', 'fake id'),
      stubAxios));

});
