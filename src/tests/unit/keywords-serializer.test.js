import chai from 'chai';
import proxyquire from 'proxyquire';
import { testSingleResource, createConfigStub } from './test-helpers';

chai.should();

describe('Test keywords-serializer', () => {
  const configStub = createConfigStub();
  const { serializeKeywords } = proxyquire(
    'serializers/keywords-serializer',
    {},
  );
  configStub.restore();

  it('serializeKeywords should properly serialize keywords collection (POST) in JSON API format', () => {
    const updatedKeywordCollection = {
      id: 'fake id',
    };
    const result = serializeKeywords(updatedKeywordCollection, {method: 'POST', query: 'fake'});
    return testSingleResource(result);
  });
  it('serializeKeywords should properly serialize keywords collection (GET) in JSON API format', () => {
    const updatedKeywordCollection = {
      id: 'fake id',
    };
    const result = serializeKeywords(updatedKeywordCollection, {method: 'GET', query: 'fake'});
    return testSingleResource(result);
  });
});
