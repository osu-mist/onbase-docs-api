import chai from 'chai';
import proxyquire from 'proxyquire';

import { createConfigStub, runSerializerSuite } from './test-helpers';

chai.should();

describe('Test keywords-serializer', () => {
  const configStub = createConfigStub();
  const { serializeKeywords } = proxyquire('serializers/keywords-serializer', {});
  configStub.restore();

  runSerializerSuite('serializeKeywords', 'keywords collection', serializeKeywords);
});
