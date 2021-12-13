import axios from 'axios';
import config from 'config';
import FormData from 'form-data';
import _ from 'lodash';

import { logger } from 'utils/logger';

const {
  baseUri,
  apiServer,
  idpServer,
  tenant,
  clientId,
  clientSecret,
  onbaseProfiles,
} = config.get('dataSources.http');

const onbaseIdpUrl = `${baseUri}/app/${idpServer}`;
const onbaseDocumentsUrl = `${baseUri}/app/${apiServer}/onbase/core/documents`;
const onbaseDocumentTypesUrl = `${baseUri}/app/${apiServer}/onbase/core/document-types`;

/**
 * Get API access token from OnBase IDP server
 *
 * @param {string} onbaseProfile OnBase profile name
 * @returns {Promise} resolves if fetched access token or rejects otherwise
 */
const getAccessToken = async (onbaseProfile) => {
  try {
    const { username, password } = onbaseProfiles[onbaseProfile];

    const formData = new FormData();
    formData.append('grant_type', 'password');
    formData.append('scope', 'evolution');
    formData.append('tenant', tenant);
    formData.append('client_id', clientId);
    formData.append('client_secret', clientSecret);
    formData.append('username', username);
    formData.append('password', password);

    const reqConfig = {
      method: 'post',
      url: `${onbaseIdpUrl}/connect/token`,
      headers: formData.getHeaders(),
      data: formData,
    };

    const { data: { access_token: accessToken } } = await axios(reqConfig);
    return accessToken;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.error);
      throw new Error(err.response.data.error_description);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded
 *
 * @param {string} token access token
 * @param {string} fileExtension file extension
 * @param {number} fileSize file size
 * @returns {Promise} resolves if staging area initialized or rejects otherwise
 */
const initiateStagingArea = async (token, fileExtension, fileSize) => {
  try {
    const reqConfig = {
      method: 'post',
      url: `${onbaseDocumentsUrl}/uploads`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
      data: { fileExtension, fileSize },
    };

    const { data } = await axios(reqConfig);
    return data;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded
 *
 * @param {string} token access token
 * @param {string} uploadId the unique reference to the file being uploaded
 * @param {number} filePart part number of the file to upload
 * @param {string} mimeType media type
 * @param {object} fileBuffer binary content for file upload
 * @returns {Promise} resolves if file uploaded or rejects otherwise
 */
const uploadFile = async (token, uploadId, filePart, mimeType, fileBuffer) => {
  try {
    const reqConfig = {
      method: 'put',
      url: `${onbaseDocumentsUrl}/uploads/${uploadId}`,
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': mimeType,
      },
      params: { filePart },
      data: fileBuffer,
      maxContentLength: Infinity,
      maxBodyLength: Infinity,
    };

    await axios(reqConfig);
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 204) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Get default keywords GUID string to ensure integrity of restricted keyword values
 *
 * @param {string} token access token
 * @param {string} documentTypeId the unique identifier of a document type
 * @returns {Promise} resolves if keywords GUID fetched or rejects otherwise
 */
const getDefaultKeywordsGuid = async (token, documentTypeId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentTypesUrl}/${documentTypeId}/default-keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
    };

    const { data: { keywordGuid } } = await axios(reqConfig);
    return keywordGuid;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status === 404) {
      logger.error(err.response.data.errors);
      return new Error(err.response.data.detail);
    } if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    }
    throw new Error(err);
  }
};

/**
 * Finishes the document upload by archiving the document into the given document type
 *
 * @param {string} token access token
 * @param {string} documentTypeId the unique identifier of a document type
 * @param {string} uploadId file uploaded ID
 * @param {string} keywordsGuid keywords GUID string
 * @returns {Promise} resolves if document archived successfully or rejects otherwise
 */
const archiveDocument = async (token, documentTypeId, uploadId, keywordsGuid) => {
  try {
    const reqConfig = {
      method: 'post',
      url: `${onbaseDocumentsUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
      data: {
        documentTypeId,
        uploads: [{ id: uploadId }],
        keywordCollection: {
          keywordGuid: keywordsGuid,
          items: [],
        },
      },
    };

    const { data: { id: documentId } } = await axios(reqConfig);
    return documentId;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Get document metadata
 *
 * @param {string} token access token
 * @param {string} documentId the unique identifier of a document.
 * @returns {Promise} resolves if document meta data fetched successfully or rejects otherwise
 */
const getDocumentById = async (token, documentId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/${documentId}`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
    };

    const { data } = await axios(reqConfig);
    return data;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Get document keywords
 *
 * @param {string} token access token
 * @param {string} documentId the unique identifier of a document type
 * @returns {Promise} resolves if document keywords fetched or rejects otherwise
 */
const getDocumentKeywords = async (token, documentId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/${documentId}/keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
    };

    const { data } = await axios(reqConfig);
    return data;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status === 404) {
      logger.error(err.response.data.errors);
      return new Error(err.response.data.detail);
    } if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    }
    throw new Error(err);
  }
};

/**
 * Get document metadata
 * @param {string} token access token
 * @param {string} documentId the unique identifier of a document.
 * @param {object} currentKeywordCollection current keyword collection
 * @param {object} newKeywords new keywords object
 * @returns {Promise} resolves if document meta data fetched successfully or rejects otherwise
 */
const patchDocumentKeywords = async (token, documentId, currentKeywordCollection, newKeywords) => {
  try {
    const newKeywordsMap = {};
    _.forEach(newKeywords, (newKeyword) => {
      newKeywordsMap[newKeyword.keywordTypeId] = _.map(newKeyword.values, (value) => ({ value }));
    });

    _.forEach(currentKeywordCollection.items[0].keywords, (keyword) => {
      if (_.has(newKeywordsMap, keyword.typeId)) {
        keyword.values = newKeywordsMap[keyword.typeId];
      }
    });

    const reqConfig = {
      method: 'put',
      url: `${onbaseDocumentsUrl}/${documentId}/keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
      },
      data: currentKeywordCollection,
    };

    const data = await axios(reqConfig);
    return data;
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

export {
  getAccessToken,
  initiateStagingArea,
  uploadFile,
  getDefaultKeywordsGuid,
  archiveDocument,
  getDocumentById,
  getDocumentKeywords,
  patchDocumentKeywords,
};
