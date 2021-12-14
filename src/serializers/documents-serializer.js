import { Serializer as JsonApiSerializer } from 'jsonapi-serializer';
import _ from 'lodash';

import { serializerOptions } from 'utils/jsonapi';
import { openapi } from 'utils/load-openapi';
import { apiBaseUrl, resourcePathLink, paramsLink } from 'utils/uri-builder';

const documentResourceProp = openapi.components.schemas.DocumentResource.properties;
const documentResourceType = documentResourceProp.type.enum[0];
const documentResourceKeys = _.keys(documentResourceProp.attributes.properties);
const documentResourcePath = 'documents';
const documentResourceUrl = resourcePathLink(apiBaseUrl, documentResourcePath);

/**
 * Serialize document resource to JSON API
 *
 * @param {object} documentMetaData document meta data
 * @param {boolean} req Express request object
 * @returns {object} Serialized documentResource object
 */
const serializeDocument = (documentMetaData, { method, query }) => {
  const baseUrl = method === 'POST'
    ? documentResourceUrl
    : resourcePathLink(documentResourceUrl, documentMetaData.id);
  const topLevelSelfLink = paramsLink(baseUrl, query);

  documentMetaData.documentTypeId = documentMetaData.typeId;

  const serializerArgs = {
    identifierField: 'id',
    resourceKeys: documentResourceKeys,
    resourcePath: documentResourcePath,
    topLevelSelfLink,
    query,
    enableDataLinks: true,
  };

  return new JsonApiSerializer(
    documentResourceType,
    serializerOptions(serializerArgs),
  ).serialize(documentMetaData);
};
export { serializeDocument };
