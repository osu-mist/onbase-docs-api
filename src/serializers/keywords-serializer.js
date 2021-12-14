import { Serializer as JsonApiSerializer } from 'jsonapi-serializer';
import _ from 'lodash';

import { serializerOptions } from 'utils/jsonapi';
import { openapi } from 'utils/load-openapi';
import { apiBaseUrl, resourcePathLink, paramsLink } from 'utils/uri-builder';

const keywordsResourceProp = openapi.components.schemas.KeywordsResource.properties;
const keywordsResourceType = keywordsResourceProp.type.enum[0];
const keywordsResourceKeys = _.keys(keywordsResourceProp.attributes.properties);
const keywordsResourcePath = 'keywords';
const documentsResourcePath = 'documents';
const documentsResourceUrl = resourcePathLink(apiBaseUrl, documentsResourcePath);

/**
 * Serialize keywords resource to JSON API
 *
 * @param {object} keywordCollection keyword collection
 * @param {boolean} req Express request object
 * @param updatedKeywordCollection
 * @returns {object} Serialized keywordResource object
 */
const serializeKeywords = (updatedKeywordCollection, { method, query }) => {
  const baseUrl = method === 'POST'
    ? documentsResourceUrl
    : resourcePathLink(
      resourcePathLink(documentsResourceUrl, updatedKeywordCollection.id), keywordsResourcePath,
    );
  const topLevelSelfLink = paramsLink(baseUrl, query);

  const serializerArgs = {
    identifierField: 'id',
    resourceKeys: keywordsResourceKeys,
    resourcePath: keywordsResourcePath,
    topLevelSelfLink,
    query,
    enableDataLinks: false,
  };

  return new JsonApiSerializer(
    keywordsResourceType,
    serializerOptions(serializerArgs),
  ).serialize(updatedKeywordCollection);
};
export { serializeKeywords };
