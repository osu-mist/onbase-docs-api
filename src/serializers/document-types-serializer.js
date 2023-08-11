import { Serializer as JsonApiSerializer } from 'jsonapi-serializer';
import _ from 'lodash';

import { serializerOptions } from 'utils/jsonapi';
import { openapi } from 'utils/load-openapi';
import { apiBaseUrl, resourcePathLink, paramsLink } from 'utils/uri-builder';

const documentTypeResourceProp = openapi.components.schemas.DocumentTypeResource.properties;
const documentTypeResourceType = documentTypeResourceProp.type.enum[0];
const documentTypeResourceKeys = _.keys(documentTypeResourceProp.attributes.properties);
const documentTypeResourcePath = 'document-types';
const documentTypeResourceUrl = resourcePathLink(
  apiBaseUrl,
  documentTypeResourcePath,
);

/**
 * Serialize document type resource to JSON API
 *
 * @param {object} rawDocumentType document metadata
 * @param {boolean} req Express request object
 * @returns {object} Serialized documentResource object
 */
const serializeDocumentType = (rawDocumentType, { method, query }) => {
  const baseUrl = method === 'POST'
    ? documentTypeResourceUrl
    : resourcePathLink(documentTypeResourceUrl, rawDocumentType.id);
  const topLevelSelfLink = paramsLink(baseUrl, query);

  const serializerArgs = {
    identifierField: 'id',
    resourceKeys: documentTypeResourceKeys,
    resourcePath: documentTypeResourcePath,
    topLevelSelfLink,
    query,
    enableDataLinks: true,
  };

  return new JsonApiSerializer(
    documentTypeResourceType,
    serializerOptions(serializerArgs),
  ).serialize(rawDocumentType);
};

/**
 * Serialize document type resource to JSON API
 *
 * @param {object} rawDocumentTypes raw document types
 * @param {boolean} query query parameters
 * @returns {object} Serialized documentTypeResource object
 */
const serializeDocumentTypes = (rawDocumentTypes, query) => {
  const topLevelSelfLink = paramsLink(documentTypeResourceUrl, query);

  const serializerArgs = {
    identifierField: 'id',
    resourceKeys: documentTypeResourceKeys,
    resourcePath: documentTypeResourcePath,
    topLevelSelfLink,
    query,
    enableDataLinks: true,
  };

  return new JsonApiSerializer(
    documentTypeResourceType,
    serializerOptions(serializerArgs),
  ).serialize(rawDocumentTypes);
};

export { serializeDocumentType, serializeDocumentTypes };
