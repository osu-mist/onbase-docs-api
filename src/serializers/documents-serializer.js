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
 * @param {object} documentMetadata document metadata
 * @param {boolean} req Express request object
 * @returns {object} Serialized documentResource object
 */
const serializeDocument = (documentMetadata, { method, query }) => {
  const baseUrl = method === 'POST'
    ? documentResourceUrl
    : resourcePathLink(documentResourceUrl, documentMetadata.id);
  const topLevelSelfLink = paramsLink(baseUrl, query);

  documentMetadata.documentTypeId = documentMetadata.typeId;

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
  ).serialize(documentMetadata);
};

/**
 * Serialize document resource to JSON API
 *
 * @param {object} documentsMetadata document metadata
 * @param {boolean} query query parameters
 * @returns {object} Serialized documentResource object
 */
const serializeDocuments = (documentsMetadata, query) => {
  const topLevelSelfLink = paramsLink(documentResourceUrl, query);

  _.forEach(documentsMetadata, (documentMetadata) => {
    documentMetadata.documentTypeId = documentMetadata.typeId;
  });

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
  ).serialize(documentsMetadata);
};

export { serializeDocument, serializeDocuments };
