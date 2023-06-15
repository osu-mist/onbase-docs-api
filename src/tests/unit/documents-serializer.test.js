import chai from 'chai';
import proxyquire from 'proxyquire';
import { testMultipleResources, testSingleResource, createConfigStub } from './test-helpers';

chai.should();

describe('Test documents-serializer', () => {
  const configStub = createConfigStub();
  const { serializeDocuments, serializeDocument } = proxyquire(
    'serializers/documents-serializer',
    {},
  );
  configStub.restore();

  it('serializeDocuments should properly serialize multiple documents in JSON API format', () => {
    const doc = {
      typeId: 'fake id',
    };
    const result = serializeDocuments([doc, doc], '');
    return testMultipleResources(result);
  });
  it('serializeDocument should properly serialize a single document (POST) in JSON API format', () => {
    const document = {
      id: 'fake id',
    };
    const result = serializeDocument(document, {method: 'POST', query: 'fake'});
    return testSingleResource(result);
  });
  it('serializeDocument should properly serialize a single document (GET) in JSON API format', () => {
    const document = {
      id: 'fake id',
    };
    const result = serializeDocument(document, {method: 'GET', query: 'fake'});
    return testSingleResource(result);
  });
});
