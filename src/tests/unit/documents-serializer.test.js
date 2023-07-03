import chai from 'chai';
import proxyquire from 'proxyquire';

import { fakeDocument } from './mock-data';
import { testMultipleResources, createConfigStub, runSerializerSuite } from './test-helpers';

chai.should();

describe('Test documents-serializer', () => {
  const configStub = createConfigStub();
  const { serializeDocuments, serializeDocument } = proxyquire('serializers/documents-serializer', {});
  configStub.restore();

  it('serializeDocuments should properly serialize multiple documents in JSON API format', () => {
    const result = serializeDocuments([fakeDocument, fakeDocument], '');
    return testMultipleResources(result);
  });

  runSerializerSuite('serializeDocument', 'a single document', serializeDocument);
});
